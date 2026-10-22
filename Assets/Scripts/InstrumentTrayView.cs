using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Instruments round: the spawned instruments in a purple side tray. Turn banner, category filter,
// and on your turn tap an instrument and then a highlighted free cell to place it (dragging still works).
// A long press opens InstrumentDetailsPanel. The tray slides away to a hexagon stack on the screen edge.
// It stays active while hidden: the pieces in it are networked objects and must keep receiving RPCs.
// Built by the editor menu "BioValue/Build Instrument Tray".
public class InstrumentTrayView : MonoBehaviour
{
    public RectTransform drawer;
    public CanvasGroup drawerGroup;
    public HandFan stack;
    public GameObject stackTurnTag;
    public float slideTime = 0.35f;

    [Header("Turn banner")]
    public Image bannerFill;
    public GameObject turnDot;
    public TMP_Text bannerTitle;
    public TMP_Text bannerSubtitle;
    public TMP_Text leftText;

    [Header("Category filter")]
    public RectTransform chipContainer;
    public GameObject chipTemplate;

    [Header("Pieces")]
    public RectTransform content;   // the InstrumentSpawner's container (pieces are spawned here)

    [Header("Tap to place")]
    public Sprite cellHighlightSprite;
    // Free cells while placing: violet, so it isn't the pink of selection and votes or the objective's gold.
    public Color freeCellColor = new Color32(0x8B, 0x6C, 0xFF, 0x9A);

    public bool IsOpen { get; private set; }

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color BannerOther = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color ChipIdle = new Color(1f, 1f, 1f, 0.65f);
    private static readonly InstrumentType[] CategoryOrder =
    {
        InstrumentType.Planning, InstrumentType.Information, InstrumentType.Incentive,
        InstrumentType.Agreement, InstrumentType.Regulation, InstrumentType.Inspection
    };

    private float openX;
    private bool initialized;
    private InstrumentType? filter;           // null = all
    private InstrumentHook picked;
    private int totalSeen;
    private string lastSignature;
    private float nextRefresh;
    private readonly HashSet<InstrumentHook> hiddenByFilter = new HashSet<InstrumentHook>();
    private readonly List<GameObject> chips = new List<GameObject>();
    private readonly Dictionary<BoardCell, Image> highlights = new Dictionary<BoardCell, Image>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Init()
    {
        if (initialized || !drawer) return;
        initialized = true;
        openX = drawer.anchoredPosition.x;
        if (chipTemplate) chipTemplate.SetActive(false);
    }

    private void OnEnable()
    {
        Init();
        Show(true);
        if (GameState.Instance != null)
        {
            GameState.Instance.onPlayerTurnIndexChanged.AddListener(OnTurnChanged);
            OnTurnChanged(GameState.Instance.GetCurrentPlayerTurnIndex());
        }
        Refresh(true);
    }

    private void OnDisable()
    {
        if (GameState.Instance != null)
            GameState.Instance.onPlayerTurnIndexChanged.RemoveListener(OnTurnChanged);
        Unpick();
    }

    private void LateUpdate() => FitPiecesBelowChips();

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            Refresh(false);
        }

        // Free cells pulse while a piece is armed for tap-to-place.
        if (highlights.Count > 0)
        {
            float a = Mathf.Lerp(0.45f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            foreach (var img in highlights.Values)
            {
                if (!img || !img.gameObject.activeSelf) continue;
                var c = freeCellColor;
                c.a *= a;
                img.color = c;
            }
        }
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    public void Show(bool instant = false)
    {
        Init();
        if (!drawer) return;
        IsOpen = true;
        Slide(drawer.anchoredPosition.x, openX, instant);
        if (drawerGroup) drawerGroup.interactable = drawerGroup.blocksRaycasts = true;
        if (stack) stack.SetShown(false, instant);
    }

    public void Hide(bool instant = false)
    {
        Init();
        if (!drawer) return;
        IsOpen = false;
        Slide(drawer.anchoredPosition.x, ClosedX, instant);
        if (drawerGroup) drawerGroup.interactable = drawerGroup.blocksRaycasts = false;
        if (stack) stack.SetShown(true, instant);
    }

    // Far enough that the close handle on the tray's left border leaves the screen too
    // (the hexagon stack is the way to open it).
    private float ClosedX => openX + drawer.rect.width + 140f;

    private void Slide(float from, float to, bool instant)
    {
        LeanTween.cancel(drawer.gameObject);
        if (instant || slideTime <= 0f || !gameObject.activeInHierarchy)
        {
            SetX(to);
            return;
        }
        LeanTween.value(drawer.gameObject, from, to, slideTime).setEaseOutCubic().setOnUpdate(SetX);
    }

    private void SetX(float x)
    {
        var p = drawer.anchoredPosition;
        p.x = x;
        drawer.anchoredPosition = p;
    }

    // ── Turn banner ──────────────────────────────────────────────────────────

    private bool IsMyTurn => GameState.Instance != null && GameState.Instance.IsMyTurn();

    private bool? bannerMine;
    private int bannerActor = -1;

    private void OnTurnChanged(int actorNumber) => UpdateHighlights(); // also refreshes the banner

    // Title, colours and subtitle all come from the same live check, every refresh, so they can't
    // disagree (the turn event alone can fire before IsMyTurn reflects the new turn).
    private void RefreshBanner()
    {
        bool mine = IsMyTurn;
        int actor = GameState.Instance != null ? GameState.Instance.GetCurrentPlayerTurnIndex() : 0;
        bool armed = picked && CanPlace(picked);
        if (bannerSubtitle)
            bannerSubtitle.text = armed ? $"Now tap a highlighted cell for {picked.instrument.id}"
                : mine ? (VotingManager.Instance ? VotingManager.Instance.PlaceHint() : "Place one instrument on the board")
                : "You can read the instruments meanwhile";
        if (bannerMine == mine && bannerActor == actor) return;

        bool becameMine = mine && bannerMine != true;
        bannerMine = mine;
        bannerActor = actor;
        if (bannerFill) bannerFill.color = mine ? Purple : BannerOther;
        if (turnDot) turnDot.SetActive(mine);
        var textColor = mine ? Color.white : Ink;
        if (bannerTitle)
        {
            bannerTitle.text = mine ? "Your turn"
                : actor > 0 ? $"{GameLog.GetPlayerName(actor)} is placing" : "Waiting for the next turn";
            bannerTitle.color = textColor;
        }
        if (bannerSubtitle) bannerSubtitle.color = textColor;
        if (leftText) leftText.color = textColor;
        if (stackTurnTag) stackTurnTag.SetActive(mine);

        if (becameMine && !IsOpen) Show();
    }

    // ── Pieces, counts and filter ────────────────────────────────────────────

    private List<InstrumentHook> PiecesInTray()
    {
        var list = new List<InstrumentHook>();
        if (!content) return list;
        foreach (Transform child in content)
        {
            var hook = child.GetComponent<InstrumentHook>();
            if (hook && hook.instrument != null) list.Add(hook);
        }
        return list;
    }

    private static bool Matches(InstrumentHook h, InstrumentType? f) =>
        f == null || h.instrument.typeOne == f.Value || h.instrument.typeTwo == f.Value;

    // Cheap: only rebuilds chips when the pieces or the filter changed.
    public void Refresh(bool force)
    {
        var pieces = PiecesInTray();

        // Pieces that left the tray (placed by anyone) must be visible again.
        foreach (var h in hiddenByFilter.ToList())
            if (!h || h.transform.parent != content) SetPieceVisible(h, true);

        foreach (var h in pieces)
        {
            if (h.GetComponent<TrayPieceTap>() == null) h.gameObject.AddComponent<TrayPieceTap>().tray = this;
        }

        if (picked && (picked.transform.parent != content)) Unpick();

        string signature = string.Join(",", pieces.Select(p => p.instrument.id)) + "|" + filter;
        if (!force && signature == lastSignature)
        {
            UpdateHighlights();
            return;
        }
        lastSignature = signature;

        if (filter != null && !pieces.Any(p => Matches(p, filter))) filter = null;
        foreach (var h in pieces) SetPieceVisible(h, Matches(h, filter));

        int placed = InstrumentsInPlay.Ids().Count;
        totalSeen = Mathf.Max(totalSeen, pieces.Count + placed);
        if (leftText) leftText.text = $"{pieces.Count} of {totalSeen} left";
        if (stack) stack.SetCount(pieces.Count, 0);

        RebuildChips(pieces);
        UpdateHighlights();
    }

    // Hidden, not deactivated: the piece's PhotonView must keep receiving RPCs.
    private void SetPieceVisible(InstrumentHook hook, bool visible)
    {
        if (!hook) { hiddenByFilter.Remove(hook); return; }
        var group = hook.GetComponent<CanvasGroup>() ?? hook.gameObject.AddComponent<CanvasGroup>();
        var layout = hook.GetComponent<LayoutElement>() ?? hook.gameObject.AddComponent<LayoutElement>();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        layout.ignoreLayout = !visible;
        if (visible) hiddenByFilter.Remove(hook);
        else hiddenByFilter.Add(hook);
    }

    private void RebuildChips(List<InstrumentHook> pieces)
    {
        if (!chipContainer || !chipTemplate) return;
        foreach (var c in chips) if (c) Destroy(c);
        chips.Clear();

        var style = pieces.Count > 0 ? pieces[0].GetComponent<InstrumentPieceView>() : null;
        AddChip(null, "All", pieces.Count, null);
        foreach (var type in CategoryOrder)
        {
            int n = pieces.Count(p => Matches(p, type));
            if (n == 0) continue;
            var s = style ? style.StyleFor(type) : null;
            string name = s != null && !string.IsNullOrEmpty(s.displayName) ? s.displayName.Split(' ')[0] : type.ToString();
            AddChip(type, name, n, s != null ? s.ring : (Color?)null);
        }
    }

    // The chips wrap onto as many rows as they need, so the pieces list starts right under the last row
    // instead of at a fixed height (more categories = more rows).
    private const float ChipsToPiecesGap = 10f;
    private float fittedChipsHeight = -1f;

    private void FitPiecesBelowChips()
    {
        if (!chipContainer || !content) return;
        var pieces = content.parent ? content.parent.parent as RectTransform : null; // Pieces > Viewport > content
        if (!pieces || pieces.parent != chipContainer.parent) return;

        float h = LayoutUtility.GetPreferredHeight(chipContainer);
        if (Mathf.Approximately(h, fittedChipsHeight)) return;
        fittedChipsHeight = h;

        chipContainer.offsetMin = new Vector2(chipContainer.offsetMin.x, chipContainer.offsetMax.y - h);
        pieces.offsetMax = new Vector2(pieces.offsetMax.x, chipContainer.offsetMax.y - h - ChipsToPiecesGap);
    }

    private void AddChip(InstrumentType? type, string label, int count, Color? dot)
    {
        var chip = Instantiate(chipTemplate, chipContainer);
        chip.name = "Chip " + label;
        chip.SetActive(true);
        bool on = filter == type;

        var fill = chip.GetComponent<Image>();
        if (fill) fill.color = on ? Purple : ChipIdle;
        var dotT = chip.transform.Find("Dot");
        if (dotT)
        {
            dotT.gameObject.SetActive(dot.HasValue);
            if (dot.HasValue) dotT.GetComponent<Image>().color = dot.Value;
        }
        var text = chip.GetComponentInChildren<TMP_Text>(true);
        if (text)
        {
            text.text = $"{label} <alpha=#99>{count}";
            text.color = on ? Color.white : Ink;
        }
        var button = chip.GetComponent<Button>();
        if (button) button.onClick.AddListener(() => SetFilter(type));
        chips.Add(chip);
    }

    public void SetFilter(InstrumentType? type)
    {
        filter = filter == type ? null : type;
        if (type == null) filter = null;
        Refresh(true);
    }

    // ── Pick ─────────────────────────────────────────────────────────────────

    public void Pick(InstrumentHook hook)
    {
        if (!hook || hook.transform.parent != content) return;
        if (picked == hook) { Unpick(); return; }
        Select(hook);
    }

    // From the details window's "Place on board": open the tray and wait for a cell tap.
    public void ArmPlace(InstrumentHook hook)
    {
        if (!hook || hook.transform.parent != content) return;
        if (!IsOpen) Show();
        Select(hook);
    }

    private void Select(InstrumentHook hook)
    {
        picked = hook;
        InstrumentLinkHighlights.Set(new[] { hook.instrument.id }); // reuse the pink piece glow
        InstrumentLinkHighlights.Visible = true;
        UpdateHighlights();
    }

    public void Unpick()
    {
        if (picked) InstrumentLinkHighlights.Clear();
        picked = null;
        UpdateHighlights();
    }

    private bool CanPlace(InstrumentHook hook)
    {
        var drag = hook ? hook.GetComponent<DragPiece>() : null;
        return drag && drag.enabled && drag.CanPlaceNow && drag.gridParent && hook.transform.parent == content;
    }

    // ── Tap to place ─────────────────────────────────────────────────────────

    private void UpdateHighlights()
    {
        bool armed = picked && CanPlace(picked);
        RefreshBanner();
        var board = armed ? picked.GetComponent<DragPiece>().gridParent : null;

        foreach (var pair in highlights)
            if (pair.Value) pair.Value.gameObject.SetActive(false);
        if (!armed || !board) return;

        for (int i = 0; i < board.childCount; i++)
        {
            var cell = board.GetChild(i).GetComponent<BoardCell>();
            if (!cell || !cell.IsFree()) continue;
            if (!highlights.TryGetValue(cell, out var img) || !img) img = highlights[cell] = CreateHighlight(cell);
            img.gameObject.SetActive(true);
        }
    }

    private Image CreateHighlight(BoardCell cell)
    {
        var go = new GameObject("TapToPlace", typeof(RectTransform));
        go.layer = cell.gameObject.layer;
        var rt = (RectTransform)go.transform;
        // On the raised tile when the cell has one (it is smaller than the cell), else the whole cell.
        var tile = cell.transform.Find("Top");
        rt.SetParent(tile ? tile : cell.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        // The same shape as what it covers: the tile's sprite (exactly its size), else the cell's.
        var under = tile ? tile.GetComponent<Image>() : null;
        if (!under) under = cell.GetComponent<Image>();
        img.sprite = under && under.sprite ? under.sprite : cellHighlightSprite;
        img.type = under ? under.type : Image.Type.Simple;
        img.preserveAspect = under ? under.preserveAspect : true;
        img.color = freeCellColor;
        img.raycastTarget = true;
        go.AddComponent<TrayCellTap>().tray = this;
        return img;
    }

    public void OnCellTapped(BoardCell cell)
    {
        if (!picked || !cell || !cell.IsFree() || !CanPlace(picked)) return;
        var drag = picked.GetComponent<DragPiece>();
        var cellRT = (RectTransform)cell.transform;
        Unpick();
        drag.StartCoroutine(drag.PlaceAtCell(cellRT));
    }
}

// Added to each piece while it is in the tray: a tap (not a drag) picks it.
public class TrayPieceTap : MonoBehaviour, IPointerClickHandler
{
    public InstrumentTrayView tray;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!tray || eventData.dragging) return;
        if (InstrumentDetailsPanel.Instance && InstrumentDetailsPanel.Instance.IsOpen) return; // the long press opened it
        tray.Pick(GetComponent<InstrumentHook>());
    }
}

// On a highlighted free board cell: a tap places the picked piece there.
public class TrayCellTap : MonoBehaviour, IPointerClickHandler
{
    public InstrumentTrayView tray;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tray) tray.OnCellTapped(GetComponentInParent<BoardCell>());
    }
}

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: the instrument details window opened by a long press on a piece (in the tray or on the board).
// Left: the piece, big, on its category colour. Right: name, where it is, description, the played action
// cards that link it (board only), browse arrows and "Place on board" on your turn.
// The lobby has no panel in its scene, so SpawnerDetails there still spawns its own details prefab.
// Built by the editor menu "BioValue/Build Instrument Tray".
public class InstrumentDetailsPanel : MonoBehaviour
{
    public static InstrumentDetailsPanel Instance { get; private set; }

    public GameObject root;            // overlay + window, hidden when closed
    public CanvasGroup rootGroup;
    public RectTransform window;
    public InstrumentTrayView tray;

    [Header("Left")]
    public Image leftFill;
    public Image bigRing;
    public Image bigFill;
    public Image bigBadge;
    public TMP_Text bigNumber;
    public TMP_Text bigName;
    public RectTransform categoryContainer;
    public GameObject categoryTemplate;

    [Header("Right")]
    public RectTransform header;       // eyebrow + name + status, stacked
    public TMP_Text eyebrow;
    public TMP_Text nameText;
    public Image statusDot;
    public TMP_Text statusText;
    public TMP_Text description;
    public ScrollRect scroll;
    public GameObject linksBlock;
    public RectTransform linksContainer;
    public GameObject linkTemplate;
    public GameObject noLinks;

    [Header("Footer")]
    public TMP_Text positionText;
    public TMP_Text footHint;
    public GameObject placeButton;
    public GameObject closeButton;

    public bool IsOpen => root && root.activeSelf;

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);

    private InstrumentHook current;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float nextRefresh;

    private void Awake()
    {
        Instance = this;
        if (root) root.SetActive(false);
        if (categoryTemplate) categoryTemplate.SetActive(false);
        if (linkTemplate) linkTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (!current) { Close(); return; }

        // Turn, placement and links can change while it is open.
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            Fill(false);
        }
    }

    // ── Open / close / browse ────────────────────────────────────────────────

    public void Open(InstrumentHook hook)
    {
        if (!hook || hook.instrument == null || !root) return;
        current = hook;
        bool wasOpen = IsOpen;
        root.SetActive(true);
        transform.SetAsLastSibling();
        Fill(true);

        if (!wasOpen && window)
        {
            LeanTween.cancel(window.gameObject);
            window.localScale = Vector3.one * 0.94f;
            LeanTween.scale(window.gameObject, Vector3.one, 0.22f).setEaseOutCubic().setIgnoreTimeScale(true);
            if (rootGroup)
            {
                rootGroup.alpha = 0f;
                LeanTween.alphaCanvas(rootGroup, 1f, 0.18f).setIgnoreTimeScale(true);
            }
        }
    }

    public void Close()
    {
        current = null;
        if (root) root.SetActive(false);
    }

    public void Next() => Step(1);
    public void Previous() => Step(-1);

    private void Step(int delta)
    {
        var list = Siblings();
        if (list.Count == 0) return;
        int i = list.IndexOf(current);
        current = list[((i + delta) % list.Count + list.Count) % list.Count];
        Fill(true);
    }

    // The instruments in the same place as the current one: the tray, or the board.
    private List<InstrumentHook> Siblings()
    {
        if (!current || !current.transform.parent) return new List<InstrumentHook>();
        var list = new List<InstrumentHook>();
        foreach (Transform t in current.transform.parent)
        {
            var h = t.GetComponent<InstrumentHook>();
            if (h && h.instrument != null) list.Add(h);
        }
        return InTray(current) ? list : list.OrderBy(h => h.instrument.id).ToList();
    }

    private bool InTray(InstrumentHook hook) =>
        hook && tray && tray.content && hook.transform.parent == tray.content;

    private static bool CanPlace(InstrumentHook hook)
    {
        var drag = hook ? hook.GetComponent<DragPiece>() : null;
        return drag && drag.enabled && drag.canDrag && drag.gridParent;
    }

    public void PlaceOnBoard()
    {
        var hook = current;
        Close();
        if (tray && hook) tray.ArmPlace(hook);
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Fill(bool resetScroll)
    {
        if (!current) return;
        var inst = current.instrument;
        var view = current.GetComponent<InstrumentPieceView>();
        var a = view ? view.StyleFor(inst.typeOne) : null;
        var b = view && inst.typeTwo != inst.typeOne ? view.StyleFor(inst.typeTwo) : null;
        if (a == null) { a = b; b = null; }

        Color ring = a != null ? a.ring : Purple;
        Color fill = a != null ? a.fill : new Color32(0xED, 0xE6, 0xFA, 0xFF);
        Color badge = a != null ? a.badge : Purple;
        if (leftFill) leftFill.color = fill;
        if (bigRing) bigRing.color = ring;
        if (bigFill) bigFill.color = fill;
        if (bigBadge) bigBadge.color = badge;
        if (bigNumber) bigNumber.text = inst.id.ToString();
        if (bigName) bigName.text = inst.name;
        if (eyebrow) { eyebrow.text = $"Instrument {inst.id}"; eyebrow.color = badge; }
        if (nameText) nameText.text = inst.name;
        if (description) description.text = inst.description;

        bool inTray = InTray(current);
        bool canPlace = inTray && CanPlace(current);
        string status;
        Color dot;
        if (!inTray)
        {
            status = inst.isPlaced ? $"On the board, placed by {GameLog.GetPlayerNameByIndex(inst.ownerId)}" : "On the board";
            dot = PurpleMid;
        }
        else if (canPlace)
        {
            status = "In the tray: you can place it now";
            dot = Pink;
        }
        else
        {
            int actor = GameState.Instance != null ? GameState.Instance.GetCurrentPlayerTurnIndex() : 0;
            status = actor > 0 ? $"In the tray: {GameLog.GetPlayerName(actor)} is placing" : "In the tray";
            dot = Grey;
        }
        if (statusText) statusText.text = status;
        if (statusDot) statusDot.color = dot;

        var list = Siblings();
        if (positionText) positionText.text = $"{list.IndexOf(current) + 1} of {list.Count}";
        if (placeButton) placeButton.SetActive(canPlace);
        if (closeButton) closeButton.SetActive(!canPlace);
        if (footHint)
            footHint.text = canPlace ? "Then tap a highlighted cell" : inTray ? "You can place it on your turn" : "";

        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        AddCategory(a);
        AddCategory(b);

        if (linksBlock) linksBlock.SetActive(!inTray);
        if (!inTray)
        {
            var cards = GameState.Instance != null
                ? GameState.Instance.GetActionCardsLinking(inst.id)
                : new List<(int playerId, int cardId)>();
            foreach (var (player, card) in cards)
            {
                if (!linkTemplate || !linksContainer) break;
                var chip = Instantiate(linkTemplate, linksContainer);
                chip.SetActive(true);
                var label = chip.GetComponentInChildren<TMP_Text>(true);
                if (label) label.text = $"{CardNumber(card)} <alpha=#BB>{GameLog.GetPlayerNameByIndex(player)}";
                spawned.Add(chip);
            }
            if (noLinks) noLinks.SetActive(cards.Count == 0);
        }

        FitBody();
        if (resetScroll && scroll) scroll.verticalNormalizedPosition = 1f;
    }

    // The description starts right under the header, whose height depends on how long the name is.
    private void FitBody()
    {
        if (!header || !scroll || !window) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(header);
        var body = (RectTransform)scroll.transform;
        float top = -header.anchoredPosition.y + header.rect.height + 12f;
        float bottom = window.rect.height - 96f; // above the footer
        body.anchoredPosition = new Vector2(body.anchoredPosition.x, -top);
        body.sizeDelta = new Vector2(body.sizeDelta.x, Mathf.Max(60f, bottom - top));
    }

    private void AddCategory(InstrumentCategoryStyle style)
    {
        if (style == null || !categoryTemplate || !categoryContainer) return;
        var chip = Instantiate(categoryTemplate, categoryContainer);
        chip.SetActive(true);
        var dotT = chip.transform.Find("Dot");
        if (dotT) dotT.GetComponent<Image>().color = style.ring;
        var label = chip.GetComponentInChildren<TMP_Text>(true);
        if (label) label.text = style.displayName;
        spawned.Add(chip);
    }

    // Same numbering as the action cards: "03" for predefined cards, "O2" for open ones.
    public static string CardNumber(int cardId) =>
        cardId >= ActionCardsDatabase.CustomCardIdStart
            ? "O" + (cardId - ActionCardsDatabase.CustomCardIdStart + 1)
            : cardId.ToString("00");
}

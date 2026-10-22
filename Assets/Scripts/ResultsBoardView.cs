using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Results card, "Final board" tab: the board as the game ended, drawn small in the middle. Each instrument
// carries little hexagons for the action cards linked to it (orange action, pink open action, ringed in the
// player's colour). Left: filter by player, and the actions linked to no instrument. Right: what's selected
// (an instrument with who placed it, its vote and every action using it; or one unlinked action).
// Built by "BioValue/Build Turn Ribbon and GM Panel" (inside the results card).
public class ResultsBoardView : MonoBehaviour
{
    [Header("Board")]
    public RectTransform boardArea;
    public Sprite hexSprite;
    public TMP_FontAsset font;

    [Header("Left")]
    public RectTransform filters;
    public GameObject filterTemplate;       // Button + Image; Dot (Image) > Number; Label
    public RectTransform unlinkedList;
    public GameObject unlinkedTemplate;     // Button + Image + Outline; Top > Hex > Number, Who > Number, Kind; Title
    public GameObject noUnlinked;

    [Header("Right")]
    public ScrollRect detailsScroll;
    public GameObject emptyDetails;
    public GameObject instGroup;            // header + owner row
    public Image instHead;
    public TMP_Text instEyebrow, instName, instCat;
    public Image ownerAvatar;
    public TMP_Text ownerNumber, ownerText;
    public GameObject vote;
    public TMP_Text voteText;
    public TMP_Text cardsTitle;
    public RectTransform cardsList;
    public GameObject cardTemplate;         // Image + Outline; Top > Hex > Number, Who > Number, Owner, Kind; Title; Description; With > Avatars, Text
    public GameObject avatarTemplate;
    public GameObject noCards;

    private static readonly Color Amber = new Color32(0xC9, 0x7A, 0x12, 0xFF);
    private static readonly Color OpenPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Glow = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color ChipOn = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color ChipLine = new Color32(0xEE, 0xEA, 0xF5, 0xFF);
    private static readonly Color Cell = new Color(1f, 1f, 1f, 0.16f);

    // A played action card.
    private class Played
    {
        public int ownerIndex, ownerActor, listIndex, cardId;
        public int[] links;
        public string title, what;
        public bool open;
        public List<int> people;
    }

    private List<Played> cards = new List<Played>();
    private int who = -1;                   // filter: player index, -1 = everyone
    private int selInstrument = -1;
    private Played selCard;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<GameObject> boardSpawned = new List<GameObject>();

    private void Awake()
    {
        foreach (var t in new[] { filterTemplate, unlinkedTemplate, cardTemplate, avatarTemplate })
            if (t) t.SetActive(false);
    }

    // Called by ResultsCard when the tab opens.
    public void Show()
    {
        cards = Gather();
        if (selInstrument < 0 && selCard == null)
        {
            var pieces = PlacedPieces();
            var withCards = pieces.FirstOrDefault(h => cards.Any(c => c.links.Contains(h.instrument.id)));
            if (withCards) selInstrument = withCards.instrument.id;
            else if (pieces.Count > 0) selInstrument = pieces[0].instrument.id;
            else selCard = cards.FirstOrDefault(c => c.links.Length == 0);
        }
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        FillFilters();
        FillUnlinked();
        FillDetails();
        Canvas.ForceUpdateCanvases();
        DrawBoard();
    }

    private bool Visible(Played c) => who < 0 || c.ownerIndex == who;

    // ── Data ─────────────────────────────────────────────────────────────────

    private static List<Played> Gather()
    {
        var list = new List<Played>();
        var state = GameState.Instance;
        if (state == null) return list;
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var collab = ColaborationManager.Instance;
        foreach (var kv in state.playerActionCards.OrderBy(k => k.Key))
        {
            if (kv.Value == null) continue;
            int actor = kv.Key >= 0 && kv.Key < sorted.Length ? sorted[kv.Key].ActorNumber : -1;
            for (int i = 0; i < kv.Value.Count; i++)
            {
                int id = kv.Value[i];
                var data = db ? db.GetActionCardById(id) : null;
                var (what, _) = CollaborationRound.CardTexts(actor, id, data);
                list.Add(new Played
                {
                    ownerIndex = kv.Key, ownerActor = actor, listIndex = i, cardId = id,
                    links = state.GetActionCardLinks(kv.Key, id) ?? new int[0],
                    title = data != null && !string.IsNullOrEmpty(data.cardName) ? data.cardName : "Action card",
                    what = what,
                    open = id >= ActionCardsDatabase.CustomCardIdStart,
                    people = collab && actor > 0 ? collab.GetCollaborators(actor, i) : new List<int>()
                });
            }
        }
        return list;
    }

    private static List<InstrumentHook> PlacedPieces()
    {
        var seen = new HashSet<int>();
        return FindObjectsByType<InstrumentHook>(FindObjectsSortMode.None)
            .Where(h => h.instrument != null && h.instrument.isPlaced && OnBoard(h) && seen.Add(h.instrument.id))
            .OrderBy(h => h.instrument.id)
            .ToList();
    }

    private static bool OnBoard(InstrumentHook h)
    {
        var drag = h.GetComponent<DragPiece>();
        return drag ? drag.OnBoard : h.transform.parent && h.transform.parent.GetComponentInChildren<BoardCell>();
    }

    private static string NameOf(int index) => index == 0 ? "GM" : GameLog.GetPlayerNameByIndex(index);
    private static Color ColorOf(int index) => index == 0 ? Purple : PlayerCapsuleHook.ColorFor(index);

    // ── Left ─────────────────────────────────────────────────────────────────

    private void FillFilters()
    {
        if (!filters || !filterTemplate) return;
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
        var indexes = new List<int> { -1 };
        for (int i = 0; i < sorted.Count; i++) if (!GameAuthority.IsGm(sorted[i])) indexes.Add(i);
        if (cards.Any(c => c.ownerIndex == 0)) indexes.Add(0); // the GM played cards too
        foreach (int index in indexes)
        {
            var chip = Spawn(filterTemplate, filters);
            bool on = who == index;
            var img = chip.GetComponent<Image>();
            if (img) img.color = on ? ChipOn : Color.white;
            var outline = chip.GetComponent<Outline>();
            if (outline) outline.effectColor = on ? Purple : ChipLine;
            var dot = chip.transform.Find("Dot");
            if (dot) dot.gameObject.SetActive(index >= 0);
            SetImage(chip.transform, "Dot", index >= 0 ? ColorOf(index) : Color.clear);
            SetText(chip.transform, "Dot/Number", index > 0 ? index.ToString() : "GM");
            SetText(chip.transform, "Label", index < 0 ? "Everyone" : $"<noparse>{NameOf(index)}</noparse>");
            int k = index;
            chip.GetComponent<Button>()?.onClick.AddListener(() => { who = k; Rebuild(); });
        }
    }

    private void FillUnlinked()
    {
        var list = cards.Where(c => c.links.Length == 0 && Visible(c)).ToList();
        if (noUnlinked) noUnlinked.SetActive(list.Count == 0);
        if (!unlinkedList || !unlinkedTemplate) return;
        foreach (var c in list)
        {
            var go = Spawn(unlinkedTemplate, unlinkedList);
            FillCardTop(go.transform, c);
            SetText(go.transform, "Title", c.title);
            var outline = go.GetComponent<Outline>();
            if (outline) outline.effectColor = c == selCard ? Purple : Accent(c);
            var played = c;
            go.GetComponent<Button>()?.onClick.AddListener(() => { selCard = played; selInstrument = -1; Rebuild(); });
        }
    }

    // ── Right ────────────────────────────────────────────────────────────────

    private void FillDetails()
    {
        var hook = selInstrument >= 0 ? InstrumentLookup.Find(selInstrument) : null;
        bool inst = hook != null;
        if (instGroup) instGroup.SetActive(inst);
        if (emptyDetails) emptyDetails.SetActive(!inst && selCard == null);

        List<Played> list;
        if (inst)
        {
            var i = hook.instrument;
            var (a, _) = InstrumentLookup.Styles(hook);
            if (instHead) instHead.color = a != null ? a.ring : Purple;
            if (instEyebrow) instEyebrow.text = $"INSTRUMENT {i.id}";
            if (instName) instName.text = i.name;
            if (instCat) instCat.text = InstrumentLookup.Categories(hook);
            if (ownerAvatar) ownerAvatar.color = ColorOf(i.ownerId);
            if (ownerNumber) ownerNumber.text = i.ownerId > 0 ? i.ownerId.ToString() : "GM";
            if (ownerText) ownerText.text = $"<b>Placed by <noparse>{NameOf(i.ownerId)}</noparse></b>";
            string v = InstrumentLookup.VoteText(hook);
            if (vote) vote.SetActive(!string.IsNullOrEmpty(v));
            if (voteText) voteText.text = v;
            list = cards.Where(c => c.links.Contains(i.id) && Visible(c)).ToList();
            if (cardsTitle) cardsTitle.text = list.Count == 1 ? "1 ACTION USES IT" : $"{list.Count} ACTIONS USE IT";
            if (noCards) noCards.SetActive(list.Count == 0);
        }
        else
        {
            list = selCard != null ? new List<Played> { selCard } : new List<Played>();
            if (cardsTitle) cardsTitle.text = selCard != null ? "ACTION NOT LINKED TO AN INSTRUMENT" : "";
            if (noCards) noCards.SetActive(false);
        }
        if (cardsTitle) cardsTitle.gameObject.SetActive(inst || selCard != null);

        if (cardsList && cardTemplate)
            foreach (var c in list)
            {
                var go = Spawn(cardTemplate, cardsList);
                var t = go.transform;
                FillCardTop(t, c);
                SetText(t, "Top/Owner", $"<noparse>{NameOf(c.ownerIndex)}</noparse>");
                SetText(t, "Title", c.title);
                SetText(t, "Description", c.what);
                var desc = t.Find("Description");
                if (desc) desc.gameObject.SetActive(!string.IsNullOrEmpty(c.what));
                var outline = go.GetComponent<Outline>();
                if (outline) outline.effectColor = Accent(c);
                var avatars = t.Find("With/Avatars");
                if (avatars && avatarTemplate)
                    foreach (int a in c.people)
                    {
                        var av = Instantiate(avatarTemplate, avatars);
                        av.SetActive(true);
                        var img = av.GetComponent<Image>();
                        if (img) img.color = PlayerCapsuleHook.ColorFor(TurnRibbon.PlayerNumberOf(a));
                    }
                SetText(t, "With/Text", c.people.Count == 0 ? "no one"
                    : string.Join(", ", c.people.Select(a => $"<noparse>{GameLog.GetPlayerName(a)}</noparse>")));
            }
        if (detailsScroll)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailsScroll.content);
            detailsScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void FillCardTop(Transform t, Played c)
    {
        Color accent = Accent(c);
        SetImage(t, "Top/Hex", accent);
        SetText(t, "Top/Hex/Number", InstrumentDetailsPanel.CardNumber(c.cardId));
        SetImage(t, "Top/Who", ColorOf(c.ownerIndex));
        SetText(t, "Top/Who/Number", c.ownerIndex > 0 ? c.ownerIndex.ToString() : "GM");
        SetText(t, "Top/Kind", c.open ? "OPEN ACTION" : "ACTION", accent);
    }

    private static Color Accent(Played c) => c.open ? OpenPink : Amber;

    // ── Board ────────────────────────────────────────────────────────────────

    private void DrawBoard()
    {
        foreach (var go in boardSpawned) if (go) Destroy(go);
        boardSpawned.Clear();
        if (!boardArea || !hexSprite) return;

        var anyCell = FindFirstObjectByType<BoardCell>(FindObjectsInactive.Include);
        var grid = anyCell ? anyCell.transform.parent as RectTransform : null;
        if (!grid) return;

        // Cells and pieces in the grid's space, then fitted into the area.
        var cells = new List<(Vector2 centre, Vector2 size)>();
        foreach (Transform child in grid)
        {
            if (!child.GetComponent<BoardCell>()) continue;
            var rt = (RectTransform)child;
            var tile = rt.Find("Top") as RectTransform;
            cells.Add((Centre(grid, rt), (tile ? tile.rect.size : rt.rect.size)));
        }
        if (cells.Count == 0) return;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (var (c, s) in cells) { min = Vector2.Min(min, c - s / 2f); max = Vector2.Max(max, c + s / 2f); }
        var area = boardArea.rect;
        Vector2 span = max - min;
        float scale = Mathf.Min((area.width - 80f) / Mathf.Max(1f, span.x), (area.height - 80f) / Mathf.Max(1f, span.y));
        Vector2 mid = (min + max) / 2f;
        Vector2 Map(Vector2 p) => (p - mid) * scale;

        foreach (var (c, s) in cells)
        {
            var img = Hex(boardArea, "Cell", Map(c), s * scale * 0.94f, Cell);
            boardSpawned.Add(img.gameObject);
        }

        foreach (var hook in PlacedPieces())
        {
            var rt = (RectTransform)hook.transform;
            if (rt.parent != grid) continue;
            DrawPiece(hook, Map(Centre(grid, rt)), cells[0].size * scale);
        }
    }

    private void DrawPiece(InstrumentHook hook, Vector2 pos, Vector2 size)
    {
        int id = hook.instrument.id;
        var (a, _) = InstrumentLookup.Styles(hook);
        var attached = cards.Where(c => c.links.Contains(id) && Visible(c)).ToList();
        bool selected = id == selInstrument;
        bool dim = who >= 0 && hook.instrument.ownerId != who && attached.Count == 0;

        var holder = new GameObject("Piece " + id, typeof(RectTransform), typeof(CanvasGroup));
        var prt = (RectTransform)holder.transform;
        prt.SetParent(boardArea, false);
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = pos;
        prt.sizeDelta = size;
        holder.GetComponent<CanvasGroup>().alpha = dim ? 0.35f : 1f;
        boardSpawned.Add(holder);

        var ring = Hex(prt, "Ring", Vector2.zero, size, selected ? Glow : a != null ? a.ring : Purple);
        ring.raycastTarget = true;
        var button = holder.AddComponent<Button>();
        button.targetGraphic = ring;
        button.onClick.AddListener(() => { selInstrument = id; selCard = null; Rebuild(); });
        Hex(prt, "Face", Vector2.zero, size * 0.84f, a != null ? a.fill : Color.white);
        Label(prt, id.ToString(), size.y * 0.3f, new Color32(0x24, 0x1B, 0x3A, 0xFF), size);
        if (selected)
        {
            var halo = Hex(prt, "Halo", Vector2.zero, size * 1.18f, new Color(Glow.r, Glow.g, Glow.b, 0.35f));
            halo.transform.SetAsFirstSibling();
        }

        // The actions linked to it, stacked on its top-right edge.
        Vector2 badge = size * 0.4f;
        for (int j = 0; j < attached.Count; j++)
        {
            var c = attached[j];
            var at = new Vector2(size.x * (0.36f + (j % 2) * 0.18f), size.y * (0.36f - j * 0.26f));
            Hex(prt, "Badge", at, badge, ColorOf(c.ownerIndex));
            Hex(prt, "BadgeFill", at, badge * 0.8f, Accent(c));
            var label = Label(prt, InstrumentDetailsPanel.CardNumber(c.cardId), badge.y * 0.36f, Color.white, badge);
            ((RectTransform)label.transform).anchoredPosition = at;
        }
    }

    private static Vector2 Centre(RectTransform grid, RectTransform rt) =>
        grid.InverseTransformPoint(rt.TransformPoint(rt.rect.center));

    private Image Hex(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = hexSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TMP_Text Label(RectTransform parent, string text, float fontSize, Color color, Vector2 size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        t.fontStyle = FontStyles.Bold;
        t.fontSize = Mathf.Max(9f, fontSize);
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private GameObject Spawn(GameObject template, Transform parent)
    {
        var go = Instantiate(template, parent);
        go.SetActive(true);
        spawned.Add(go);
        return go;
    }

    private static void SetText(Transform parent, string path, string text, Color? color = null)
    {
        var t = parent.Find(path);
        var label = t ? t.GetComponent<TMP_Text>() : null;
        if (!label) return;
        label.text = text;
        if (color.HasValue) label.color = color.Value;
    }

    private static void SetImage(Transform parent, string path, Color color)
    {
        var t = parent.Find(path);
        var img = t ? t.GetComponent<Image>() : null;
        if (img) img.color = color;
    }
}

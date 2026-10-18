using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: the Game Log drawer. A white "GAME LOG" tab on the right edge slides the log in and out
// sideways (the tab travels with it), mirroring the GM drawer on the left.
// One row per event, newest on top, grouped under round dividers: a coloured icon per kind of event,
// the sentence with names in bold, an optional chip (vote tally, cards played) and the time.
// Filter chips: All / Turns / Board / Votes / Cards. While closed, a new event peeks out next to the tab
// for a few seconds and a pink unread count waits on the tab.
// Built by the editor menu "BioValue/Build Game Log Drawer".
public class GameLogDrawer : MonoBehaviour
{
    public RectTransform drawer;        // panel + tab; slides right until only the tab shows
    public GameObject dim;              // darkens the board while open; a click on it closes
    public RectTransform tabArrow;      // chevron on the tab: points in when closed, out when open
    public float slideTime = 0.28f;

    [Header("List")]
    public ScrollRect scroll;
    public RectTransform list;
    public GameObject entryTemplate;
    public GameObject dividerTemplate;
    public GameObject emptyLabel;
    public TMP_Text countText;
    public float highlightSeconds = 3f;

    [Header("Filters")]
    public Image[] filterChips;         // All, Turns, Board, Votes, Cards
    public TMP_Text[] filterLabels;

    [Header("Unread")]
    public GameObject unreadBadge;
    public TMP_Text unreadText;
    public CanvasGroup peek;
    public TMP_Text peekText;
    public RectTransform peekIcon;      // same icon children as an entry's icon
    public float peekSeconds = 4f;

    public bool IsOpen { get; private set; }

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Teal = new Color32(0x22, 0x8A, 0xA2, 0xFF);
    private static readonly Color Green = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color Red = new Color32(0xD9, 0x3A, 0x3A, 0xFF);
    private static readonly Color Amber = new Color32(0xE9, 0x8A, 0x1F, 0xFF);
    private static readonly Color Blue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);
    private static readonly Color GreenSoft = new Color32(0xE3, 0xF5, 0xE8, 0xFF), GreenInk = new Color32(0x1E, 0x7A, 0x39, 0xFF);
    private static readonly Color RedSoft = new Color32(0xFB, 0xE4, 0xE4, 0xFF), RedInk = new Color32(0xA8, 0x2A, 0x2A, 0xFF);
    private static readonly Color AmberSoft = new Color32(0xFF, 0xF1, 0xDD, 0xFF), AmberInk = new Color32(0x8A, 0x4B, 0x05, 0xFF);

    // Icon child shown for each kind (the template's Icon holds one child per name).
    private static readonly string[] IconNames = { "Turn", "Placed", "Passed", "Failed", "Cards", "Plus", "Minus", "Note" };

    private readonly List<GameObject> entryPool = new List<GameObject>();
    private readonly List<GameObject> dividerPool = new List<GameObject>();
    private int filter;
    private int unread;
    private bool dirty = true;
    private float highlightUntil;
    private GameLog log;
    private Coroutine peekRoutine;

    private void Start()
    {
        if (entryTemplate) entryTemplate.SetActive(false);
        if (dividerTemplate) dividerTemplate.SetActive(false);
        if (peek) { peek.alpha = 0f; peek.gameObject.SetActive(false); }
        SetOpen(false, false);
        SetFilter(0);
        UpdateUnread();
    }

    private void OnDisable() => Unsubscribe();

    private void Update()
    {
        if (!log && GameLog.Instance) Subscribe(GameLog.Instance);
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (dirty) Rebuild();
        else if (highlightUntil > 0f && Time.unscaledTime > highlightUntil) { highlightUntil = 0f; Rebuild(); }
    }

    private void Subscribe(GameLog source)
    {
        log = source;
        log.EntryAdded += OnEntry;
        log.HistoryReplaced += OnHistory;
        dirty = true;
    }

    private void Unsubscribe()
    {
        if (!log) return;
        log.EntryAdded -= OnEntry;
        log.HistoryReplaced -= OnHistory;
        log = null;
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    public void Toggle() => SetOpen(!IsOpen, true);
    public void Close() => SetOpen(false, true);

    private void SetOpen(bool open, bool animate)
    {
        IsOpen = open;
        if (dim) dim.SetActive(open);
        if (tabArrow) tabArrow.localRotation = Quaternion.Euler(0, 0, open ? 0f : 180f);
        if (open)
        {
            unread = 0;
            UpdateUnread();
            HidePeek();
            Rebuild();
            if (scroll) scroll.verticalNormalizedPosition = 1f; // newest on top
        }
        if (!drawer) return;

        float x = open ? 0f : drawer.rect.width; // closed: only the tab sticks out
        LeanTween.cancel(drawer.gameObject);
        var target = new Vector2(x, drawer.anchoredPosition.y);
        if (animate)
            LeanTween.value(drawer.gameObject, drawer.anchoredPosition, target, slideTime)
                .setOnUpdate((Vector2 v) => drawer.anchoredPosition = v)
                .setEaseOutCubic().setIgnoreTimeScale(true);
        else
            drawer.anchoredPosition = target;
    }

    // 0 All, 1 Turns, 2 Board, 3 Votes, 4 Cards
    public void SetFilter(int index)
    {
        filter = index;
        for (int i = 0; filterChips != null && i < filterChips.Length; i++)
        {
            bool on = i == index;
            if (filterChips[i]) filterChips[i].color = on ? Purple : Color.white;
            if (filterLabels != null && i < filterLabels.Length && filterLabels[i])
                filterLabels[i].color = on ? Color.white : Purple;
        }
        Rebuild();
        if (scroll) scroll.verticalNormalizedPosition = 1f;
    }

    // ── New entries ──────────────────────────────────────────────────────────

    private void OnEntry(GameLog.Entry entry)
    {
        dirty = true;
        if (entry.kind == GameLog.Kind.Round) return;
        if (IsOpen)
        {
            highlightUntil = Time.unscaledTime + highlightSeconds;
            return;
        }
        unread++;
        UpdateUnread();
        ShowPeek(entry);
    }

    private void OnHistory() => dirty = true;

    private void UpdateUnread()
    {
        if (unreadBadge) unreadBadge.SetActive(unread > 0 && !IsOpen);
        if (unreadText) unreadText.text = unread > 99 ? "99+" : unread.ToString();
    }

    private void ShowPeek(GameLog.Entry entry)
    {
        if (!peek || !isActiveAndEnabled) return;
        if (peekText) peekText.text = entry.text;
        if (peekIcon) SetIcon(peekIcon, entry.kind);
        if (peekRoutine != null) StopCoroutine(peekRoutine);
        peekRoutine = StartCoroutine(PlayPeek());
    }

    private IEnumerator PlayPeek()
    {
        var rt = (RectTransform)peek.transform;
        var rest = new Vector2(0f, rt.anchoredPosition.y);
        var from = rest + new Vector2(30f, 0f);
        peek.gameObject.SetActive(true);
        LeanTween.cancel(peek.gameObject);

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            peek.alpha = e;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, rest, e);
            yield return null;
        }
        peek.alpha = 1f;
        rt.anchoredPosition = rest;

        yield return new WaitForSecondsRealtime(peekSeconds);

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            peek.alpha = 1f - t;
            rt.anchoredPosition = Vector2.Lerp(rest, from, t);
            yield return null;
        }
        rt.anchoredPosition = rest;
        HidePeek();
    }

    private void HidePeek()
    {
        if (peekRoutine != null) { StopCoroutine(peekRoutine); peekRoutine = null; }
        if (!peek) return;
        peek.alpha = 0f;
        peek.gameObject.SetActive(false);
    }

    // ── List ─────────────────────────────────────────────────────────────────

    private bool Passes(GameLog.Kind kind)
    {
        switch (filter)
        {
            case 1: return kind == GameLog.Kind.Turn;
            case 2: return kind == GameLog.Kind.Placed;
            case 3: return kind == GameLog.Kind.VotePassed || kind == GameLog.Kind.VoteFailed;
            case 4: return kind == GameLog.Kind.Cards || kind == GameLog.Kind.CollabAccepted || kind == GameLog.Kind.CollabDeclined;
            default: return true;
        }
    }

    // Newest round first, with its divider above its events (newest first too).
    private void Rebuild()
    {
        dirty = false;
        if (!list || !entryTemplate) return;
        var entries = log ? log.Entries : null;

        var sections = new List<(GameLog.Entry divider, List<GameLog.Entry> items)>();
        var current = (divider: (GameLog.Entry)null, items: new List<GameLog.Entry>());
        int total = 0;
        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (e.kind == GameLog.Kind.Round)
                {
                    if (current.divider != null || current.items.Count > 0) sections.Add(current);
                    current = (e, new List<GameLog.Entry>());
                    continue;
                }
                total++;
                if (Passes(e.kind)) current.items.Add(e);
            }
        }
        if (current.divider != null || current.items.Count > 0) sections.Add(current);

        int usedEntries = 0, usedDividers = 0, sibling = 0, shown = 0;
        bool first = true;
        for (int s = sections.Count - 1; s >= 0; s--)
        {
            var (divider, items) = sections[s];
            if (divider != null && (filter == 0 || items.Count > 0))
            {
                var d = Take(dividerPool, dividerTemplate, usedDividers++);
                if (d)
                {
                    d.transform.SetSiblingIndex(sibling++);
                    SetText(d.transform, "Label", divider.text);
                }
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var row = Take(entryPool, entryTemplate, usedEntries++);
                row.transform.SetSiblingIndex(sibling++);
                Fill(row.transform, items[i], first && Time.unscaledTime < highlightUntil);
                first = false;
                shown++;
            }
        }
        for (int i = usedEntries; i < entryPool.Count; i++) entryPool[i].SetActive(false);
        for (int i = usedDividers; i < dividerPool.Count; i++) dividerPool[i].SetActive(false);

        if (emptyLabel)
        {
            emptyLabel.SetActive(shown == 0);
            emptyLabel.transform.SetAsLastSibling();
            SetText(emptyLabel.transform, null, total == 0 ? "Nothing has happened yet." : "No events of this kind yet.");
        }
        if (countText) countText.text = total == 1 ? "1 event" : $"{total} events";
    }

    private GameObject Take(List<GameObject> pool, GameObject template, int index)
    {
        if (!template) return null;
        if (index >= pool.Count) pool.Add(Instantiate(template, list));
        var go = pool[index];
        go.SetActive(true);
        return go;
    }

    private void Fill(Transform row, GameLog.Entry e, bool highlight)
    {
        var icon = row.Find("Icon") as RectTransform;
        if (icon) SetIcon(icon, e.kind);
        SetText(row, "Body/Text", e.text);
        SetText(row, "Time", e.time);

        var bg = row.Find("Bg");
        if (bg) bg.gameObject.SetActive(highlight);

        var chip = row.Find("Body/ChipRow");
        bool hasDetail = !string.IsNullOrEmpty(e.detail);
        if (chip)
        {
            chip.gameObject.SetActive(hasDetail);
            if (hasDetail)
            {
                Color fill = AmberSoft, ink = AmberInk;
                if (e.kind == GameLog.Kind.VotePassed) { fill = GreenSoft; ink = GreenInk; }
                else if (e.kind == GameLog.Kind.VoteFailed) { fill = RedSoft; ink = RedInk; }
                var chipImg = chip.Find("Chip")?.GetComponent<Image>();
                if (chipImg) chipImg.color = fill;
                var label = chip.Find("Chip/Label")?.GetComponent<TMP_Text>();
                if (label) { label.text = e.detail; label.color = ink; }
            }
        }
    }

    private static void SetIcon(RectTransform icon, GameLog.Kind kind)
    {
        string show;
        Color color;
        switch (kind)
        {
            case GameLog.Kind.Turn: show = "Turn"; color = PurpleMid; break;
            case GameLog.Kind.Placed: show = "Placed"; color = Teal; break;
            case GameLog.Kind.VotePassed: show = "Passed"; color = Green; break;
            case GameLog.Kind.VoteFailed: show = "Failed"; color = Red; break;
            case GameLog.Kind.Cards: show = "Cards"; color = Amber; break;
            case GameLog.Kind.CollabAccepted: show = "Plus"; color = Blue; break;
            case GameLog.Kind.CollabDeclined: show = "Minus"; color = Grey; break;
            default: show = "Note"; color = PurpleMid; break;
        }
        var circle = icon.GetComponent<Image>();
        if (circle) circle.color = color;
        foreach (var name in IconNames)
        {
            var child = icon.Find(name);
            if (child) child.gameObject.SetActive(name == show);
        }
    }

    private static void SetText(Transform root, string path, string text)
    {
        var t = path == null ? root : root.Find(path);
        var tmp = t ? t.GetComponent<TMP_Text>() : null;
        if (tmp) tmp.text = text;
    }
}

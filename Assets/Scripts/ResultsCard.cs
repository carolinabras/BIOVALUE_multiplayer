using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// End of the game: everyone's results on one card.
//  - When the game ends ("End collaboration") the GM first sees "Players are rating the game" (waiting):
//    who has answered, live. "See results" opens the card (players still answering greyed out);
//    "Show results to everyone" shares it, "Save results" exports.
//  - Players see it once the GM shares it (their end card closes then). Everyone has "Quit game".
//  - Two tabs: "Scores" (below) and "Final board" (ResultsBoardView: the board as it ended, with the
//    action cards on the instruments they use and the unlinked ones to the side).
//  - Header: the two rating averages and the score formula. One row per player, ranked by score: rank,
//    avatar, name, company, both ratings (red → green), a bar of how the score adds up (one colour per
//    item), the score. Click a row for their "did your objective change?" answer and the math.
// Replaces EndGameManager's old results list. Built by "BioValue/Build Turn Ribbon and GM Panel".
public class ResultsCard : MonoBehaviourPunCallbacks
{
    public static ResultsCard Instance { get; private set; }

    public GameObject content;
    public TMP_Text eyebrow, subtitle;
    public TMP_Text gameAverage, ownAverage;
    public RectTransform gameAverageFill, ownAverageFill;  // bars under the averages (anchorMax.x = value / 10)
    public TMP_Text formulaText;
    public ScrollRect scroll;
    public RectTransform rows;
    public GameObject rowTemplate;
    public RectTransform legend;
    public GameObject legendTemplate;  // Dot (Image) + Label
    public GameObject saveButton;
    public Button shareButton;
    public TMP_Text shareLabel;
    public GameObject leaveButton;     // "Quit game", for everyone

    [Header("Tabs")]
    public Button[] tabButtons = new Button[2];
    public Image[] tabFills = new Image[2];
    public TMP_Text[] tabLabels = new TMP_Text[2];
    public GameObject[] scoresParts;       // column titles, rows, legend
    public GameObject boardPage;
    public ResultsBoardView boardView;

    [Header("GM: waiting for the players' ratings")]
    public GameObject waiting;
    public TMP_Text waitingCount;
    public RectTransform waitingRows;
    public GameObject waitingRowTemplate; // Avatar (Image) > Number, Name, Status (Image) > Label
    public Button seeResultsButton;
    public TMP_Text seeResultsLabel;

    private const string EndGameKey = "endGame";
    private const string ShowResultsKey = "showResults";

    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x8A, 0x82, 0xA0, 0xFF);
    private static readonly Color Faint = new Color32(0xC9, 0xC2, 0xDA, 0xFF);
    private static readonly Color Idle = new Color32(0xF1, 0xEE, 0xF6, 0xFF);
    private static readonly Color IdleText = new Color32(0xB5, 0xAE, 0xC6, 0xFF);
    private static readonly Color OpenBg = new Color32(0xF7, 0xF4, 0xFC, 0xFF);
    private static readonly Color Done = new Color32(0xE4, 0xE1, 0xEC, 0xFF);
    private static readonly Color DoneText = new Color32(0x5E, 0x55, 0x75, 0xFF);
    private static readonly Color AnsweredBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF);
    private static readonly Color AnsweredText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);

    private readonly List<GameObject> rowPool = new List<GameObject>();
    private readonly List<GameObject> legendPool = new List<GameObject>();
    private readonly List<GameObject> waitingPool = new List<GameObject>();
    private bool gmOpened;              // the GM chose "See results"
    private int openActor = -1;
    private float nextRefresh;

    private void Awake()
    {
        Instance = this;
        if (content) content.SetActive(false);
        if (waiting) waiting.SetActive(false);
        if (waitingRowTemplate) waitingRowTemplate.SetActive(false);
        if (rowTemplate) rowTemplate.SetActive(false);
        if (legendTemplate) legendTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (shareButton) shareButton.onClick.AddListener(() => EndGameManager.Instance?.OnClickShowResultsToAll());
        if (seeResultsButton) seeResultsButton.onClick.AddListener(() => { gmOpened = true; UpdateVisibility(); });
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int k = i;
            if (tabButtons[i]) tabButtons[i].onClick.AddListener(() => ShowTab(k));
        }
        ShowTab(0);
        UpdateVisibility();
    }

    private static bool RoomFlag(string key) =>
        PhotonNetwork.CurrentRoom != null &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out var v) && v is bool b && b;

    public override void OnRoomPropertiesUpdate(Hashtable changedProps) => UpdateVisibility();
    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps) { Refresh(); RefreshWaiting(); }
    public override void OnPlayerLeftRoom(Player other) { Refresh(); RefreshWaiting(); }
    public override void OnPlayerEnteredRoom(Player other) => RefreshWaiting();

    private void Update()
    {
        if (!content || !content.activeSelf || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1f; // collaborations and cards can still change
        Refresh();
    }

    private void UpdateVisibility()
    {
        bool gm = GameAuthority.IsGameMaster;
        bool ended = RoomFlag(EndGameKey);
        bool shared = RoomFlag(ShowResultsKey);
        // The GM waits for the ratings first (no waiting panel built: straight to the results, as before).
        bool wait = gm && ended && !shared && !gmOpened && waiting;
        bool show = ended && (shared || (gm && !wait));
        if (waiting && waiting.activeSelf != wait) waiting.SetActive(wait);
        if (content && content.activeSelf != show) content.SetActive(show);
        if (wait) RefreshWaiting();
        if (show) Refresh();
    }

    private int tab;
    private static readonly Color TabIdle = new Color32(0x5F, 0x4D, 0x8F, 0xFF);

    private void ShowTab(int index)
    {
        tab = index;
        bool board = index == 1;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool on = i == index;
            Color fill = on ? Color.white : TabIdle;
            if (tabFills[i])
            {
                tabFills[i].color = fill;
                var square = tabFills[i].transform.Find("SquareBottom")?.GetComponent<Image>();
                if (square) square.color = fill;
            }
            if (tabLabels[i]) tabLabels[i].color = on ? Ink : Color.white;
        }
        if (scoresParts != null) foreach (var go in scoresParts) if (go) go.SetActive(!board);
        if (boardPage) boardPage.SetActive(board);
        if (board && boardView) boardView.Show();
    }

    private void RefreshWaiting()
    {
        if (!waiting || !waiting.activeSelf) return;
        var all = ScoreFormula.Players();
        int answered = all.Count(ScoreFormula.HasAnswered);
        bool everyone = all.Length > 0 && answered == all.Length;
        if (waitingCount)
            waitingCount.text = everyone ? "Everyone has answered" : $"{answered} of {all.Length} players have answered";
        if (seeResultsLabel)
        {
            seeResultsLabel.text = everyone ? "See results" : "See results now";
            seeResultsLabel.color = everyone ? Color.white : Purple;
        }
        if (seeResultsButton && seeResultsButton.targetGraphic)
            seeResultsButton.targetGraphic.color = everyone ? Pink : Lilac;

        if (!waitingRows || !waitingRowTemplate) return;
        while (waitingPool.Count < all.Length) waitingPool.Add(Instantiate(waitingRowTemplate, waitingRows));
        for (int i = 0; i < waitingPool.Count; i++)
        {
            bool used = i < all.Length;
            waitingPool[i].SetActive(used);
            if (!used) continue;
            var p = all[i];
            bool done = ScoreFormula.HasAnswered(p);
            int number = TurnRibbon.PlayerNumberOf(p.ActorNumber);
            var t = waitingPool[i].transform;
            var avatar = t.Find("Avatar")?.GetComponent<Image>();
            if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(number);
            var num = t.Find("Avatar/Number")?.GetComponent<TMP_Text>();
            if (num) num.text = number.ToString();
            var name = t.Find("Name")?.GetComponent<TMP_Text>();
            if (name) name.text = $"<noparse>{GameLog.GetPlayerName(p.ActorNumber)}</noparse>";
            var status = t.Find("Status")?.GetComponent<Image>();
            if (status) status.color = done ? AnsweredBg : Idle;
            var label = t.Find("Status/Label")?.GetComponent<TMP_Text>();
            if (label)
            {
                label.text = done ? "Answered" : "Answering…";
                label.color = done ? AnsweredText : Muted;
            }
        }
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (!content || !content.activeSelf) return;
        bool gm = GameAuthority.IsGameMaster;
        bool shared = RoomFlag(ShowResultsKey);
        var terms = ScoreFormula.Current;
        ScoreFormula.Averages(out float avgGame, out float avgOwn, out int answered);
        var all = ScoreFormula.Players();

        if (eyebrow) eyebrow.text = !gm ? "RESULTS" : shared ? "RESULTS · SHARED WITH EVERYONE" : "RESULTS · ONLY YOU SEE THIS";
        if (subtitle) subtitle.text = $"{answered} of {all.Length} players answered";
        Average(gameAverage, gameAverageFill, avgGame, answered);
        Average(ownAverage, ownAverageFill, avgOwn, answered);
        if (formulaText) formulaText.text = ScoreFormula.Describe(terms);

        if (saveButton) saveButton.SetActive(gm);
        if (leaveButton) leaveButton.SetActive(true);
        if (shareButton)
        {
            shareButton.gameObject.SetActive(gm);
            shareButton.interactable = !shared;
            if (shareButton.targetGraphic) shareButton.targetGraphic.color = shared ? Done : Pink;
        }
        if (shareLabel)
        {
            shareLabel.text = shared ? "Shared with everyone" : "Show results to everyone";
            shareLabel.color = shared ? DoneText : Color.white;
        }

        RefreshLegend(terms);

        // Ranked by score; players still answering at the bottom (only the GM sees them).
        var entries = all
            .Select(p => (player: p, score: ScoreFormula.Score(p, avgGame, avgOwn)))
            .Where(e => gm || e.score.HasValue)
            .OrderByDescending(e => e.score ?? float.MinValue)
            .ToList();
        float max = 0.0001f;
        foreach (var e in entries)
        {
            if (!e.score.HasValue) continue;
            float positive = terms.Sum(t => Mathf.Max(0f, t.weight * ScoreFormula.Value(t.item, e.player, avgGame, avgOwn)));
            max = Mathf.Max(max, positive);
        }

        while (rowPool.Count < entries.Count)
        {
            var row = Instantiate(rowTemplate, rows);
            rowPool.Add(row);
        }
        int rank = 0;
        for (int i = 0; i < rowPool.Count; i++)
        {
            bool used = i < entries.Count;
            rowPool[i].SetActive(used);
            if (!used) continue;
            var (player, score) = entries[i];
            if (score.HasValue) rank++;
            FillRow(rowPool[i], player, score, score.HasValue ? rank : 0, terms, avgGame, avgOwn, max);
        }
    }

    private static void Average(TMP_Text label, RectTransform fill, float value, int answered)
    {
        if (label) label.text = answered > 0 ? value.ToString("0.0") : "–";
        if (fill) fill.anchorMax = new Vector2(answered > 0 ? Mathf.Clamp01(value / 10f) : 0f, 1f);
    }

    private void RefreshLegend(ScoreFormula.Term[] terms)
    {
        if (!legend || !legendTemplate) return;
        while (legendPool.Count < terms.Length) legendPool.Add(Instantiate(legendTemplate, legend));
        for (int i = 0; i < legendPool.Count; i++)
        {
            bool used = i < terms.Length;
            legendPool[i].SetActive(used);
            if (!used) continue;
            var dot = legendPool[i].transform.Find("Dot")?.GetComponent<Image>();
            if (dot) dot.color = terms[i].weight < 0 ? new Color32(0xD9, 0x3A, 0x3A, 0xFF) : ScoreFormula.ColorOf(terms[i].item);
            var label = legendPool[i].transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label) label.text = ScoreFormula.Legend(terms[i].item) + (terms[i].weight < 0 ? " (minus)" : "");
        }
    }

    private void FillRow(GameObject row, Player player, float? score, int rank, ScoreFormula.Term[] terms,
                         float avgGame, float avgOwn, float max)
    {
        bool done = score.HasValue;
        bool open = openActor == player.ActorNumber;
        int number = TurnRibbon.PlayerNumberOf(player.ActorNumber);
        var t = row.transform;
        row.name = "Row " + number;

        var bg = row.GetComponent<Image>();
        if (bg) bg.color = open ? OpenBg : Color.white;
        var group = row.GetComponent<CanvasGroup>();
        if (group) group.alpha = done ? 1f : 0.6f;

        var main = t.Find("Main");
        var button = main ? main.GetComponent<Button>() : null;
        if (button)
        {
            int actor = player.ActorNumber;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { openActor = openActor == actor ? -1 : actor; Refresh(); });
        }

        var rankImg = main.Find("Rank")?.GetComponent<Image>();
        if (rankImg) rankImg.color = rank == 1 ? Pink : Lilac;
        SetText(main, "Rank/Label", done ? rank.ToString() : "–", rank == 1 ? Color.white : Purple);
        var avatar = main.Find("Avatar")?.GetComponent<Image>();
        if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(number);
        SetText(main, "Avatar/Number", number.ToString(), null);
        SetText(main, "Name", GameLog.GetPlayerName(player.ActorNumber), null);
        SetText(main, "Company", player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c) ? c as string : "", null);
        Pill(main.Find("Game"), ScoreFormula.TryRating(player, BiovalueStatics.MainObjectiveRatingKey, out int g) ? g : (int?)null);
        Pill(main.Find("Own"), ScoreFormula.TryRating(player, BiovalueStatics.PersonalObjectiveRatingKey, out int o) ? o : (int?)null);

        // Bar: one segment per item that adds points; points taken away in a red chip after it.
        var bar = main.Find("Bar");
        var segs = bar ? bar.Find("Track/Segments") : null;
        var segTemplate = segs ? segs.Find("Segment") : null;
        float negative = 0f, from = 0f;
        int shown = 0;
        if (segs && segTemplate)
        {
            segTemplate.gameObject.SetActive(false);
            foreach (var term in terms)
            {
                float pts = done ? term.weight * ScoreFormula.Value(term.item, player, avgGame, avgOwn) : 0f;
                if (pts < 0f) { negative += pts; continue; }
                if (pts <= 0f) continue;
                Transform seg = shown + 1 < segs.childCount ? segs.GetChild(shown + 1) : Instantiate(segTemplate, segs);
                shown++;
                seg.gameObject.SetActive(true);
                var rt = (RectTransform)seg;
                rt.anchorMin = new Vector2(from / max, 0f);
                rt.anchorMax = new Vector2((from + pts) / max, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                from += pts;
                var img = seg.GetComponent<Image>();
                if (img) img.color = ScoreFormula.ColorOf(term.item);
                var label = seg.GetComponentInChildren<TMP_Text>(true);
                if (label) label.text = pts / max > 0.07f ? pts.ToString("0.#") : "";
            }
            for (int i = shown + 1; i < segs.childCount; i++) segs.GetChild(i).gameObject.SetActive(false);
        }
        if (bar) bar.Find("Track")?.gameObject.SetActive(done);
        var neg = bar ? bar.Find("Negative") : null;
        if (neg)
        {
            neg.gameObject.SetActive(done && negative < 0f);
            var label = neg.GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = "−" + (-negative).ToString("0.#");
        }
        var waiting = bar ? bar.Find("Waiting") : null;
        if (waiting) waiting.gameObject.SetActive(!done);

        SetText(main, "Score", done ? score.Value.ToString("0.0") : "–", done ? Ink : Faint);
        var chevron = main.Find("Chevron");
        if (chevron) chevron.localRotation = Quaternion.Euler(0, 0, open ? -90f : 0f);

        // Details: their answer about the objective, and the math line by line.
        var details = t.Find("Details");
        if (details)
        {
            details.gameObject.SetActive(open);
            if (open)
            {
                string change = player.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveChangeKey, out var ch) ? ch as string : null;
                var changeText = details.Find("Change/Text")?.GetComponent<TMP_Text>();
                if (changeText)
                {
                    bool has = !string.IsNullOrWhiteSpace(change);
                    changeText.text = has ? $"“<noparse>{change.Trim()}</noparse>”" : done ? "No answer." : "Still answering.";
                    changeText.color = has ? Ink : Muted;
                }
                // Their objective, only if they chose to show it on their end card.
                var objectiveText = details.Find("Change/ObjectiveText")?.GetComponent<TMP_Text>();
                if (objectiveText)
                {
                    bool shared = player.CustomProperties.TryGetValue(BiovalueStatics.ShareObjectiveKey, out var sh) && sh is bool b && b;
                    string mine = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerObjectiveKey, out var ob) ? ob as string : null;
                    bool has = shared && !string.IsNullOrWhiteSpace(mine);
                    objectiveText.text = has ? $"“<noparse>{mine.Trim()}</noparse>”" : "Kept private.";
                    objectiveText.color = has ? Ink : Muted;
                }
                var labels = new List<string>();
                var values = new List<string>();
                foreach (var term in terms)
                {
                    float raw = ScoreFormula.Value(term.item, player, avgGame, avgOwn);
                    string what = term.item == ScoreFormula.Item.Objectives ? $"{avgGame:0.0} × {avgOwn:0.0}" : raw.ToString("0.#");
                    float a = Mathf.Abs(term.weight);
                    labels.Add($"{ScoreFormula.Label(term.item)} · {what}{(Mathf.Approximately(a, 1f) ? "" : " × " + a.ToString("0.#"))}");
                    float pts = term.weight * raw;
                    values.Add(done ? (pts < 0 ? "−" : "+") + Mathf.Abs(pts).ToString("0.#") : "–");
                }
                SetText(details, "Math/Labels", string.Join("\n", labels), null);
                SetText(details, "Math/Values", string.Join("\n", values), null);
            }
        }
    }

    // 0–10 rating pill, red → green; grey "–" before answering.
    private static void Pill(Transform pill, int? value)
    {
        if (!pill) return;
        var img = pill.GetComponent<Image>();
        if (img) img.color = value.HasValue ? PlayerEndCard.Tone(value.Value) : Idle;
        var label = pill.GetComponentInChildren<TMP_Text>(true);
        if (label)
        {
            label.text = value.HasValue ? value.Value.ToString() : "–";
            label.color = value.HasValue ? Color.white : IdleText;
        }
    }

    private static void SetText(Transform parent, string path, string text, Color? color)
    {
        var t = parent ? parent.Find(path)?.GetComponent<TMP_Text>() : null;
        if (!t) return;
        t.text = text ?? "";
        if (color.HasValue) t.color = color.Value;
    }
}

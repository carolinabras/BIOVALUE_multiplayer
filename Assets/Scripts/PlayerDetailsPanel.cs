using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: the window opened by clicking a player's capsule (players and the GM see the same thing).
//  - Left, in the player's colour: arrows to step through the players, avatar and number, full name,
//    organisation, what they're doing now, and four numbers (instruments placed, action cards played,
//    collaborations made, collaborators on their cards).
//  - Right, three tabs: the instruments they placed (category, vote), the action cards they played (full
//    text, the instruments they use, collaborators) and the actions they joined, with their tokens left.
// Never shows the player's personal objective. Click an instrument to open its details.
// Built by the editor menu "BioValue/Build Player Capsules".
public class PlayerDetailsPanel : MonoBehaviour
{
    public static PlayerDetailsPanel Instance { get; private set; }

    public GameObject root;            // overlay + window, hidden when closed
    public CanvasGroup rootGroup;
    public RectTransform window;

    [Header("Left: who")]
    public Image leftFill;
    public TMP_Text positionText;      // "PLAYER 2 OF 4"
    public Button prevButton, nextButton;
    public Image avatarIcon;           // the person on the white circle, in the player's colour
    public Image badgeFill;
    public TMP_Text badgeNumber;
    public TMP_Text nameText;
    public TMP_Text companyText;
    public Image statusDot;
    public TMP_Text statusText;
    public TMP_Text[] statValues = new TMP_Text[4];

    [Header("Right: tabs")]
    public Button[] tabButtons = new Button[3];
    public TMP_Text[] tabLabels = new TMP_Text[3];
    public Image[] tabCountFills = new Image[3];
    public TMP_Text[] tabCounts = new TMP_Text[3];
    public Image[] tabUnderlines = new Image[3];
    public GameObject[] tabPages = new GameObject[3];
    public ScrollRect scroll;
    public Button closeButton;

    [Header("Instruments")]
    public RectTransform instrumentsContainer;
    public GameObject instrumentTemplate;   // Button: Hex (Image) > Number, Texts > Category, Name; Vote (Image) > Label
    public GameObject noInstruments;

    [Header("Action cards")]
    public RectTransform cardsContainer;
    public GameObject cardTemplate;         // Top > Hex > Number, Kind; Title; Description; How; Uses > Links > LinkTemplate; Footer > Avatars, People
    public GameObject avatarTemplate;
    public TMP_Text noCards;

    [Header("Collaborations")]
    public RectTransform joinedContainer;
    public GameObject joinedTemplate;       // Avatar (Image) > Number, Texts > Card, Owner
    public GameObject noJoined;
    public RectTransform tokensContainer;
    public GameObject tokenTemplate;
    public TMP_Text tokensText;

    public bool IsOpen => root && root.activeSelf;

    private enum Tab { Instruments, Cards, Collaborations }

    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color TabIdle = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color Amber = new Color32(0xC9, 0x7A, 0x12, 0xFF);
    private static readonly Color OpenPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Gold = new Color32(0xFD, 0xAA, 0x12, 0xFF);
    private static readonly Color Green = new Color32(0x7B, 0xD8, 0x8F, 0xFF);
    private static readonly Color Sky = new Color32(0x9E, 0xC8, 0xFF, 0xFF);
    private static readonly Color VoteBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF), VoteInk = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color TokenOn = new Color32(0x2F, 0x6F, 0xB0, 0xFF), TokenOff = new Color32(0xDC, 0xE6, 0xF2, 0xFF);
    private const string EndGameKey = "endGame";

    private PlayerCapsuleHook current;
    private Tab tab = Tab.Instruments;
    private bool tabChosen;            // the viewer picked a tab: keep it while browsing players
    private readonly List<GameObject> spawned = new List<GameObject>();
    private string listsShown;
    private float nextRefresh;

    private void Awake()
    {
        Instance = this;
        if (root) root.SetActive(false);
        foreach (var t in new[] { instrumentTemplate, cardTemplate, avatarTemplate, joinedTemplate, tokenTemplate })
            if (t) t.SetActive(false);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int k = i;
            if (tabButtons[i]) tabButtons[i].onClick.AddListener(() => { tabChosen = true; ShowTab((Tab)k); });
        }
        if (prevButton) prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton) nextButton.onClick.AddListener(() => Step(1));
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (!current || !current.isActiveAndEnabled) { Close(); return; }

        // Turn, tokens, placements, cards and collaborations can change while it is open.
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            Fill();
        }
    }

    // ── Open / close / browse ────────────────────────────────────────────────

    public void Open(PlayerCapsuleHook capsule)
    {
        if (!capsule || !root) return;
        bool wasOpen = IsOpen;
        if (current != capsule) listsShown = null;
        current = capsule;
        if (!wasOpen) tabChosen = false;
        root.SetActive(true);
        transform.SetAsLastSibling();
        if (!tabChosen) tab = DefaultTab();
        Fill();
        ShowTab(tab);
        if (scroll) scroll.verticalNormalizedPosition = 1f;

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
        listsShown = null;
        if (root) root.SetActive(false);
    }

    // Kept for old scene wiring: opens the played-cards panel of this player.
    public void ShowPlayedCards()
    {
        var spawner = current ? current.CardSpawner : null;
        Close();
        if (spawner) spawner.OnClickSpawnCards();
    }

    private void Step(int delta)
    {
        var list = Capsules();
        if (list.Count == 0) return;
        int i = Mathf.Max(0, list.IndexOf(current));
        Open(list[((i + delta) % list.Count + list.Count) % list.Count]);
    }

    private static List<PlayerCapsuleHook> Capsules() =>
        FindObjectsByType<PlayerCapsuleHook>(FindObjectsSortMode.None)
            .Where(c => c.isActiveAndEnabled && c._id > 0)
            .GroupBy(c => c._id).Select(g => g.First())
            .OrderBy(c => c.PlayerNumber)
            .ToList();

    private static Tab DefaultTab()
    {
        var phase = GameState.Instance != null ? GameState.Instance.GetCurrentGamePhase() : GameState.GamePhase.None;
        return phase == GameState.GamePhase.ActionCardPlay ? Tab.Cards
             : phase == GameState.GamePhase.Collaboration ? Tab.Collaborations
             : Tab.Instruments;
    }

    private void ShowTab(Tab t)
    {
        tab = t;
        Color accent = current ? current.PlayerColor : Ink;
        for (int i = 0; i < 3; i++)
        {
            bool on = i == (int)t;
            if (tabPages[i]) tabPages[i].SetActive(on);
            if (tabUnderlines[i]) tabUnderlines[i].color = on ? accent : Color.clear;
            if (tabLabels[i]) tabLabels[i].color = on ? Ink : Muted;
            if (tabCountFills[i]) tabCountFills[i].color = on ? accent : TabIdle;
            if (tabCounts[i]) tabCounts[i].color = on ? Color.white : Muted;
        }
        if (scroll)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Fill()
    {
        int actor = current._id;
        int number = current.PlayerNumber;
        Color color = current.PlayerColor;
        var player = FindPlayer(actor);
        var collab = ColaborationManager.Instance;

        var capsules = Capsules();
        if (positionText)
            positionText.text = number <= 0 ? "GAME MASTER" : $"PLAYER {number} OF {capsules.Count}";
        bool many = capsules.Count > 1;
        if (prevButton) prevButton.gameObject.SetActive(many);
        if (nextButton) nextButton.gameObject.SetActive(many);
        if (leftFill) leftFill.color = color;
        if (avatarIcon) avatarIcon.color = color;
        if (badgeFill) badgeFill.color = color;
        if (badgeNumber) badgeNumber.text = number.ToString();
        if (nameText) nameText.text = $"<noparse>{current.PlayerName}</noparse>";
        string company = Prop(player, BiovalueStatics.PlayerCompanyKey);
        if (companyText)
        {
            companyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(company));
            companyText.text = $"<noparse>{company}</noparse>";
        }
        var (status, dot) = Status(actor, number, player);
        if (statusText) statusText.text = status;
        if (statusDot) statusDot.color = dot;

        var instruments = PlacedInstruments(number);
        var cards = PlayedCards(number);
        var joined = Joined(actor);
        int made = collab ? collab.GetCollaborationsDone(actor) : 0;
        int received = collab ? collab.GetCollaborationsReceived(actor) : 0;
        SetStat(0, instruments.Count);
        SetStat(1, cards.Count);
        SetStat(2, made);
        SetStat(3, received);
        SetCount(0, instruments.Count);
        SetCount(1, cards.Count);
        SetCount(2, joined.Count);

        int start = ColaborationManager.StartTokens;
        int left = player != null && player.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var t) && t is int ti ? ti : start;

        // Rebuild the lists only when they change, so the window doesn't flicker on refresh.
        var sig = new StringBuilder();
        sig.Append(actor).Append('|').Append(string.Join(",", instruments.Select(h => h.instrument.id)));
        sig.Append('|').Append(left).Append('/').Append(start);
        for (int i = 0; i < cards.Count; i++)
        {
            sig.Append('|').Append(cards[i]).Append(':').Append(string.Join(".", Links(number, cards[i])));
            if (collab) sig.Append(':').Append(string.Join(".", collab.GetCollaborators(actor, i)));
        }
        foreach (var (owner, cardId) in joined) sig.Append('|').Append(owner).Append('_').Append(cardId);
        if (sig.ToString() == listsShown) return;
        listsShown = sig.ToString();

        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        foreach (var hook in instruments) AddInstrument(hook);
        for (int i = 0; i < cards.Count; i++) AddCard(actor, number, i, cards[i], collab);
        foreach (var (owner, cardId) in joined) AddJoined(owner, cardId);
        for (int i = 0; i < Mathf.Max(start, left); i++) AddToken(i < left);

        if (noInstruments) noInstruments.SetActive(instruments.Count == 0);
        if (noCards)
        {
            noCards.gameObject.SetActive(cards.Count == 0);
            var phase = GameState.Instance != null ? GameState.Instance.GetCurrentGamePhase() : GameState.GamePhase.None;
            noCards.text = phase == GameState.GamePhase.None || phase == GameState.GamePhase.InstrumentSelection
                ? "Action cards are played in round 2." : "Hasn’t played any action cards yet.";
        }
        if (noJoined) noJoined.SetActive(joined.Count == 0);
        if (tokensText) tokensText.text = $"{left} of {start} tokens left";

        if (scroll) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
    }

    private (string text, Color dot) Status(int actor, int number, Player player)
    {
        var state = GameState.Instance;
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        if (ended) return ("Game over", Color.white);
        if (state == null) return ("Waiting", Color.white);
        switch (state.GetCurrentGamePhase())
        {
            case GameState.GamePhase.InstrumentSelection:
                if (state.GetCurrentPlayerTurnIndex() == actor) return ("Placing an instrument", Pink);
                if (state.PeekNextTurnActor() == actor) return ("Next to place", Gold);
                return ("Waiting for their turn", Color.white);
            case GameState.GamePhase.ActionCardPlay:
                return TurnRibbon.HasPlayedCards(number) ? ("Played their cards", Green) : ("Choosing cards", Color.white);
            case GameState.GamePhase.Collaboration:
                return player != null && ColaborationManager.IsDone(player) ? ("Done", Green) : ("Collaborating", Sky);
            default:
                return ("Getting ready", Color.white);
        }
    }

    private void SetStat(int i, int value)
    {
        if (i < statValues.Length && statValues[i]) statValues[i].text = value.ToString();
    }

    private void SetCount(int i, int value)
    {
        if (i < tabCounts.Length && tabCounts[i]) tabCounts[i].text = value.ToString();
    }

    private void AddInstrument(InstrumentHook hook)
    {
        if (!instrumentTemplate || !instrumentsContainer) return;
        var row = Instantiate(instrumentTemplate, instrumentsContainer);
        row.SetActive(true);
        spawned.Add(row);
        var (a, _) = InstrumentLookup.Styles(hook);
        var t = row.transform;
        SetImage(t, "Hex", a != null ? a.ring : Ink);
        SetText(t, "Hex/Number", hook.instrument.id.ToString(), null);
        SetText(t, "Texts/Category", InstrumentLookup.Categories(hook), a != null ? a.badge : Muted);
        SetText(t, "Texts/Name", hook.instrument.name, null);
        string vote = InstrumentLookup.VoteText(hook);
        var voteT = t.Find("Vote");
        if (voteT) voteT.gameObject.SetActive(!string.IsNullOrEmpty(vote));
        SetImage(t, "Vote", VoteBg);
        SetText(t, "Vote/Label", vote, VoteInk);
        var button = row.GetComponent<Button>();
        if (button) button.onClick.AddListener(() => OpenInstrument(hook));
    }

    private void AddCard(int actor, int number, int index, int cardId, ColaborationManager collab)
    {
        if (!cardTemplate || !cardsContainer) return;
        var card = Instantiate(cardTemplate, cardsContainer);
        card.SetActive(true);
        spawned.Add(card);
        var t = card.transform;
        bool open = cardId >= ActionCardsDatabase.CustomCardIdStart;
        Color accent = open ? OpenPink : Amber;
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var data = db ? db.GetActionCardById(cardId) : null;

        var outline = card.GetComponent<Outline>();
        if (outline) outline.effectColor = accent;
        SetImage(t, "Top/Hex", accent);
        SetText(t, "Top/Hex/Number", InstrumentDetailsPanel.CardNumber(cardId), null);
        SetText(t, "Top/Kind", open ? "OPEN ACTION" : "ACTION", accent);
        SetText(t, "Title", data != null && !string.IsNullOrEmpty(data.cardName) ? data.cardName : "Action card", null);
        var (what, how) = CollaborationRound.CardTexts(actor, cardId, data);
        SetText(t, "Description", what, null);
        SetText(t, "How", string.IsNullOrEmpty(how) ? "" : "<b>How:</b> " + how, null);
        Show(t, "Description", !string.IsNullOrEmpty(what));
        Show(t, "How", !string.IsNullOrEmpty(how));

        // Instruments it uses
        var links = Links(number, cardId);
        Show(t, "Uses", links.Length > 0);
        var linksT = t.Find("Uses/Links");
        var linkTemplate = linksT ? linksT.Find("LinkTemplate") : null;
        if (linkTemplate)
        {
            linkTemplate.gameObject.SetActive(false);
            foreach (int id in links)
            {
                var hook = InstrumentLookup.Find(id);
                var (a, _) = InstrumentLookup.Styles(hook);
                var chip = Instantiate(linkTemplate.gameObject, linksT);
                chip.SetActive(true);
                var img = chip.GetComponent<Image>();
                if (img) img.color = a != null ? a.fill : TabIdle;
                SetImage(chip.transform, "Dot", a != null ? a.ring : Ink);
                SetText(chip.transform, "Label", $"{id} · {(hook ? hook.instrument.name : "Instrument")}", a != null ? a.badge : Ink);
                var button = chip.GetComponent<Button>();
                if (button && hook) button.onClick.AddListener(() => OpenInstrument(hook));
            }
        }

        // Collaborators
        var people = collab ? collab.GetCollaborators(actor, index) : new List<int>();
        var avatars = t.Find("Footer/Avatars");
        if (avatars && avatarTemplate)
            foreach (int a in people)
            {
                var av = Instantiate(avatarTemplate, avatars);
                av.SetActive(true);
                int n = TurnRibbon.PlayerNumberOf(a);
                var img = av.GetComponent<Image>();
                if (img) img.color = PlayerCapsuleHook.ColorFor(n);
                SetText(av.transform, "Number", n.ToString(), null);
            }
        SetText(t, "Footer/People", people.Count == 0 ? "No one yet"
            : string.Join(", ", people.Select(a => $"<noparse>{GameLog.GetPlayerName(a)}</noparse>")), null);
    }

    private void AddJoined(int owner, int cardId)
    {
        if (!joinedTemplate || !joinedContainer) return;
        var row = Instantiate(joinedTemplate, joinedContainer);
        row.SetActive(true);
        spawned.Add(row);
        int n = TurnRibbon.PlayerNumberOf(owner);
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var data = db ? db.GetActionCardById(cardId) : null;
        SetImage(row.transform, "Avatar", PlayerCapsuleHook.ColorFor(n));
        SetText(row.transform, "Avatar/Number", n.ToString(), null);
        SetText(row.transform, "Texts/Card", $"{InstrumentDetailsPanel.CardNumber(cardId)}  {(data != null ? data.cardName : "Action card")}", null);
        SetText(row.transform, "Texts/Owner", $"<noparse>{GameLog.GetPlayerName(owner)}</noparse>’s action", null);
    }

    private void AddToken(bool on)
    {
        if (!tokenTemplate || !tokensContainer) return;
        var dot = Instantiate(tokenTemplate, tokensContainer);
        dot.SetActive(true);
        dot.transform.SetSiblingIndex(tokenTemplate.transform.GetSiblingIndex()); // before the text
        var img = dot.GetComponent<Image>();
        if (img) img.color = on ? TokenOn : TokenOff;
        spawned.Add(dot);
    }

    private static void OpenInstrument(InstrumentHook hook)
    {
        if (hook && InstrumentDetailsPanel.Instance) InstrumentDetailsPanel.Instance.Open(hook);
    }

    // ── Data ─────────────────────────────────────────────────────────────────

    // Instruments on the board placed by this player (ownerId is the player's number), by id.
    private static List<InstrumentHook> PlacedInstruments(int number)
    {
        var seen = new HashSet<int>();
        return FindObjectsByType<InstrumentHook>(FindObjectsSortMode.None)
            .Where(h => h.instrument != null && h.instrument.isPlaced && h.instrument.ownerId == number && seen.Add(h.instrument.id))
            .OrderBy(h => h.instrument.id)
            .ToList();
    }

    // Keyed by the player's number (sorted position, GM = 0), like ActionDeck stores them.
    private static List<int> PlayedCards(int number)
    {
        if (GameState.Instance != null && GameState.Instance.playerActionCards.TryGetValue(number, out var cards) && cards != null)
            return cards;
        return new List<int>();
    }

    private static int[] Links(int number, int cardId) =>
        GameState.Instance != null ? GameState.Instance.GetActionCardLinks(number, cardId) ?? new int[0] : new int[0];

    // Other players' actions this player collaborates on (accepted), as (owner actor, card id).
    private static List<(int owner, int cardId)> Joined(int actor)
    {
        var list = new List<(int, int)>();
        var collab = ColaborationManager.Instance;
        if (!collab) return list;
        foreach (var kv in collab.collaborations)
        {
            if (kv.Value == null || !kv.Value.Contains(actor)) continue;
            var parts = kv.Key.Split('_');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int owner) || !int.TryParse(parts[1], out int index)) continue;
            var ids = CollaborationRound.CardsOf(owner);
            if (index >= 0 && index < ids.Count) list.Add((owner, ids[index]));
        }
        return list.OrderBy(j => TurnRibbon.PlayerNumberOf(j.Item1)).ToList();
    }

    private static Player FindPlayer(int actor) =>
        PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actor);

    private static string Prop(Player player, string key) =>
        player != null && player.CustomProperties.TryGetValue(key, out var v) && v != null ? v.ToString() : "";

    private static void SetText(Transform parent, string path, string text, Color? color)
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

    private static void Show(Transform parent, string path, bool show)
    {
        var t = parent.Find(path);
        if (t) t.gameObject.SetActive(show);
    }
}

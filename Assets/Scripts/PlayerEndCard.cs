using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Players' end-of-game card, shown when the GM ends the game (room property "endGame"):
//  - header: the player's number, "How did it go, <name>?" and a recap (actions played, collaborations
//    done and received);
//  - the game objective and the player's own objective, each rated 0-10 with 11 number buttons
//    (red → green, the chosen one fills in, with a word: "7 · Mostly");
//  - "Did your personal objective change during the game? How?" (optional text);
//  - "Show your objective to everyone on the results?" Show it / Keep it private (private by default);
//  - "Send my answers" (grey until both ratings are picked) saves the same player properties as the old
//    FinalPlayerCard, so the GM's results and the export keep working;
//  - then "Thanks, <name>!" with who has answered, and "Change my answers" until the GM shows the results.
// The GM never sees it. Replaces FinalPlayerCard's panel. Built by "BioValue/Build Turn Ribbon and GM Panel".
public class PlayerEndCard : MonoBehaviourPunCallbacks
{
    public static PlayerEndCard Instance { get; private set; }

    public GameObject content;          // dim background + both cards
    public GameObject form;
    public GameObject thanks;

    [Header("Header")]
    public Image avatar;
    public TextMeshProUGUI avatarNumber;
    public TextMeshProUGUI title;
    public TextMeshProUGUI actionsChip, collabsChip, receivedChip;

    [Header("Ratings")]
    public GameObject gameQuote;
    public TextMeshProUGUI gameObjective;
    public Button[] gameScale;          // 0..10
    public TextMeshProUGUI gameWord;
    public GameObject myQuote;
    public TextMeshProUGUI myObjective;
    public Button[] myScale;
    public TextMeshProUGUI myWord;

    [Header("Change and send")]
    public TMP_InputField changeInput;
    public Button shareShow;
    public Button shareHide;
    public TextMeshProUGUI hint;
    public Button sendButton;
    public TextMeshProUGUI sendLabel;

    [Header("Thanks")]
    public TextMeshProUGUI thanksTitle;
    public RectTransform dotsRow;
    public GameObject dotTemplate;      // outer circle in the player's colour, white "Inner" when not answered
    public TextMeshProUGUI answeredText;
    public Button changeButton;

    private const string EndGameKey = "endGame";
    private const string ShowResultsKey = "showResults";

    private static readonly string[] Words =
        { "Not at all", "Barely", "Barely", "A little", "A little", "Halfway", "Partly", "Mostly", "Mostly", "Almost fully", "Completely" };
    private static readonly Color Red = new Color32(0xD9, 0x3A, 0x3A, 0xFF);
    private static readonly Color Amber = new Color32(0xE9, 0x8A, 0x1F, 0xFF);
    private static readonly Color Green = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color Idle = new Color32(0xF1, 0xEE, 0xF6, 0xFF);
    private static readonly Color IdleText = new Color32(0x5E, 0x55, 0x75, 0xFF);
    private static readonly Color DimText = new Color32(0x3A, 0x31, 0x50, 0xFF);
    private static readonly Color Muted = new Color32(0x8A, 0x82, 0xA0, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Off = new Color32(0xE4, 0xE1, 0xEC, 0xFF);
    private static readonly Color OffText = new Color32(0x9A, 0x92, 0xAC, 0xFF);
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);

    private int? _game, _mine;
    private bool _share;
    private readonly List<GameObject> _dots = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        if (content) content.SetActive(false);
        if (dotTemplate) dotTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        for (int i = 0; i < gameScale.Length; i++)
        {
            int n = i;
            gameScale[i].onClick.AddListener(() => { _game = n; RefreshForm(); });
        }
        for (int i = 0; i < myScale.Length; i++)
        {
            int n = i;
            myScale[i].onClick.AddListener(() => { _mine = n; RefreshForm(); });
        }
        if (shareShow) shareShow.onClick.AddListener(() => { _share = true; RefreshForm(); });
        if (shareHide) shareHide.onClick.AddListener(() => { _share = false; RefreshForm(); });
        if (sendButton) sendButton.onClick.AddListener(Send);
        if (changeButton) changeButton.onClick.AddListener(() => ShowForm(true));

        // Rejoining: start on "Thanks" with the answers already sent.
        var me = PhotonNetwork.LocalPlayer;
        if (me != null)
        {
            if (me.CustomProperties.TryGetValue(BiovalueStatics.MainObjectiveRatingKey, out var g) && g is int gi) _game = gi;
            if (me.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveRatingKey, out var p) && p is int pi) _mine = pi;
            if (changeInput && me.CustomProperties.TryGetValue(BiovalueStatics.PersonalObjectiveChangeKey, out var c) && c is string cs)
                changeInput.text = cs;
            if (me.CustomProperties.TryGetValue(BiovalueStatics.ShareObjectiveKey, out var sh) && sh is bool share) _share = share;
        }
        UpdateVisibility(sentAlready: _game.HasValue && _mine.HasValue);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(EndGameKey) || changedProps.ContainsKey(ShowResultsKey))
            UpdateVisibility(sentAlready: false);
    }

    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(BiovalueStatics.MainObjectiveRatingKey)) RefreshThanks();
    }

    public override void OnPlayerLeftRoom(Player other) => RefreshThanks();
    public override void OnPlayerEnteredRoom(Player other) => RefreshThanks();

    private static bool RoomFlag(string key) =>
        PhotonNetwork.CurrentRoom != null &&
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out var v) && v is bool b && b;

    // Shown to players after the GM ends the game, until the GM shows the results.
    private void UpdateVisibility(bool sentAlready)
    {
        bool show = !GameAuthority.IsGameMaster && RoomFlag(EndGameKey) && !RoomFlag(ShowResultsKey);
        if (!content) return;
        if (show && !content.activeSelf)
        {
            content.SetActive(true);
            FillHeader();
            ShowForm(!sentAlready);
        }
        else if (!show && content.activeSelf)
            content.SetActive(false);
    }

    private void ShowForm(bool editing)
    {
        if (form) form.SetActive(editing);
        if (thanks) thanks.SetActive(!editing);
        if (editing) RefreshForm();
        else RefreshThanks();
    }

    private void FillHeader()
    {
        var me = PhotonNetwork.LocalPlayer;
        int number = TurnRibbon.PlayerNumberOf(me.ActorNumber);
        string name = GameLog.GetPlayerName(me.ActorNumber);
        if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(number);
        if (avatarNumber) avatarNumber.text = number.ToString();
        if (title) title.text = $"How did it go, <noparse>{name}</noparse>?";
        if (thanksTitle) thanksTitle.text = $"Thanks, <noparse>{name}</noparse>!";

        int actions = GameState.Instance != null && GameState.Instance.playerActionCards.TryGetValue(number, out var cards)
            ? cards.Count : 0;
        var collab = ColaborationManager.Instance;
        int done = collab ? collab.GetCollaborationsDone(me.ActorNumber) : 0;
        int received = collab ? collab.GetCollaborationsReceived(me.ActorNumber) : 0;
        Chip(actionsChip, actions, actions == 1 ? "action played" : "actions played");
        Chip(collabsChip, done, done == 1 ? "collaboration" : "collaborations");
        Chip(receivedChip, received, "received");

        string gameText = PhotonNetwork.CurrentRoom != null &&
                          PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BiovalueStatics.GameObjectiveKey, out var go)
            ? go as string : null;
        string myText = me.CustomProperties.TryGetValue(BiovalueStatics.PlayerObjectiveKey, out var mo) ? mo as string : null;
        Quote(gameQuote, gameObjective, gameText);
        Quote(myQuote, myObjective, myText);
    }

    private static void Chip(TextMeshProUGUI label, int n, string text)
    {
        if (label) label.text = $"<size=125%><b>{n}</b></size> {text}";
    }

    private static void Quote(GameObject box, TextMeshProUGUI label, string text)
    {
        bool has = !string.IsNullOrWhiteSpace(text);
        if (box) box.SetActive(has);
        if (label && has) label.text = $"“<noparse>{text.Trim()}</noparse>”";
    }

    private void RefreshForm()
    {
        Scale(gameScale, gameWord, _game);
        Scale(myScale, myWord, _mine);
        Choice(shareShow, _share);
        Choice(shareHide, !_share);
        bool ready = _game.HasValue && _mine.HasValue;
        if (hint) hint.text = ready
            ? "You can change your answers until the game master shows the results."
            : "Rate both objectives to send.";
        if (sendButton)
        {
            sendButton.interactable = ready;
            if (sendButton.targetGraphic) sendButton.targetGraphic.color = ready ? Pink : Off;
        }
        if (sendLabel) sendLabel.color = ready ? Color.white : OffText;
    }

    // The picked one in purple, the other light.
    private static void Choice(Button b, bool on)
    {
        if (!b) return;
        if (b.targetGraphic) b.targetGraphic.color = on ? Purple : Idle;
        var label = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label) label.color = on ? Color.white : IdleText;
    }

    // Red → amber → green along 0..10.
    public static Color Tone(int n)
    {
        float t = n / 10f;
        return t < 0.5f ? Color.Lerp(Red, Amber, t * 2f) : Color.Lerp(Amber, Green, (t - 0.5f) * 2f);
    }

    private static void Scale(Button[] buttons, TextMeshProUGUI word, int? value)
    {
        for (int n = 0; n < buttons.Length; n++)
        {
            var b = buttons[n];
            if (!b) continue;
            bool chosen = value == n, below = value.HasValue && n < value.Value;
            var bg = chosen ? Tone(n) : below ? Color.Lerp(Color.white, Tone(n), 0.2f) : Idle;
            if (b.targetGraphic) b.targetGraphic.color = bg;
            var label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.color = chosen ? Color.white : below ? DimText : IdleText;
            b.transform.localScale = Vector3.one * (chosen ? 1.08f : 1f);
        }
        if (word)
        {
            word.text = value.HasValue ? $"{value.Value} · {Words[value.Value]}" : "Tap a number";
            word.color = value.HasValue ? Tone(value.Value) : Muted;
        }
    }

    private void Send()
    {
        if (!_game.HasValue || !_mine.HasValue) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            [BiovalueStatics.MainObjectiveRatingKey] = _game.Value,
            [BiovalueStatics.PersonalObjectiveRatingKey] = _mine.Value,
            [BiovalueStatics.PersonalObjectiveChangeKey] = changeInput ? changeInput.text.Trim() : string.Empty,
            [BiovalueStatics.ShareObjectiveKey] = _share,
        });
        ShowForm(false);
    }

    // One dot per player in their colour: filled once they have answered.
    private void RefreshThanks()
    {
        if (!thanks || !thanks.activeInHierarchy || !dotTemplate || !dotsRow) return;
        var players = PhotonNetwork.PlayerList.Where(p => !GameAuthority.IsGm(p)).OrderBy(p => p.ActorNumber).ToList();
        while (_dots.Count < players.Count)
            _dots.Add(Instantiate(dotTemplate, dotsRow));
        int answered = 0;
        for (int i = 0; i < _dots.Count; i++)
        {
            bool used = i < players.Count;
            _dots[i].SetActive(used);
            if (!used) continue;
            var p = players[i];
            bool me = p.IsLocal;
            bool done = p.CustomProperties.ContainsKey(BiovalueStatics.MainObjectiveRatingKey) ||
                        (me && _game.HasValue);
            if (done) answered++;
            var img = _dots[i].GetComponent<Image>();
            if (img) img.color = PlayerCapsuleHook.ColorFor(TurnRibbon.PlayerNumberOf(p.ActorNumber));
            var inner = _dots[i].transform.Find("Inner");
            if (inner) inner.gameObject.SetActive(!done);
        }
        if (answeredText) answeredText.text = $"{answered} of {players.Count} players answered";
    }
}

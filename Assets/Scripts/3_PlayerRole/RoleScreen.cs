using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Role scene (mockup "Role"), built by "BioValue/Build Role".
//  - Player: name, entity and personal objective; a live preview of their card; "I'm ready" locks
//    the fields, "Edit" unlocks them (and un-readies). What they type is shared as they go, so the GM
//    reads it live. The objective is seen only by them and the GM (they can show it on the results).
//  - GM: a card per player (Not started / Writing… / Ready) and "Start the game", or the outlined
//    "Start anyway" while someone is not ready. The master loads MainGame and everyone follows.
public class RoleScreen : MonoBehaviourPunCallbacks
{
    public const int ObjectiveLimit = 220;
    private const string NextScene = "MainGame";
    private const float ShareDelay = 0.8f;

    [Header("Top bar")]
    public TMP_Text gameObjective;
    public Button materialsButton;
    public TMP_Text roomLabel;

    [Header("Player")]
    public GameObject playerScreen;
    public TMP_Text playerTitle;
    public TMP_Text playerSub;
    public TMP_InputField nameInput;
    public TMP_InputField entityInput;
    public TMP_InputField objectiveInput;
    public TMP_Text objectiveCount;
    public TMP_Text readyHint;
    public Button editButton;
    public Button readyButton;
    public Image previewBand;
    public TMP_Text previewNumber;
    public TMP_Text previewName;
    public TMP_Text previewEntity;
    public TMP_Text previewObjective;
    public GameObject readyBanner;
    public TMP_Text othersReady;

    [Header("Game master")]
    public GameObject gmScreen;
    public TMP_Text readyCount;
    public Image readyCountPill;
    public RectTransform cardList;
    public GameObject cardTemplate;
    public TMP_Text gmHint;
    public Button startButton;
    public Button startAnywayButton;

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Faint = new Color32(0xA9, 0xA3, 0xBC, 0xFF);
    private static readonly Color Disabled = new Color32(0xF1, 0xEF, 0xF6, 0xFF);
    private static readonly Color Lilac = new Color32(0xF6, 0xF2, 0xFC, 0xFF);
    private static readonly Color OkBg = new Color32(0xDD, 0xF3, 0xE0, 0xFF);
    private static readonly Color OkText = new Color32(0x1E, 0x7A, 0x2C, 0xFF);
    private static readonly Color WritingBg = new Color32(0xFF, 0xF1, 0xD6, 0xFF);
    private static readonly Color WritingText = new Color32(0x8A, 0x5A, 0x00, 0xFF);
    private static readonly Color Glass = new Color(1f, 1f, 1f, 0.16f);

    private bool _locked, _starting;
    private float _shareAt = -1f;
    private readonly List<GameObject> _cards = new List<GameObject>();

    private static bool IsGm => PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;

    // ── Setup ────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (materialsButton) materialsButton.onClick.AddListener(OpenMaterials);

        nameInput.onValueChanged.AddListener(_ => Typed());
        entityInput.onValueChanged.AddListener(_ => Typed());
        objectiveInput.characterLimit = ObjectiveLimit;
        objectiveInput.onValueChanged.AddListener(_ => Typed());
        foreach (var f in new[] { nameInput, entityInput, objectiveInput }) f.onEndEdit.AddListener(_ => Share());
        readyButton.onClick.AddListener(GetReady);
        editButton.onClick.AddListener(Edit);
        startButton.onClick.AddListener(StartGame);
        startAnywayButton.onClick.AddListener(StartGame);
        if (cardTemplate) cardTemplate.SetActive(false);

        // What the player already gave (the name from the lobby, or everything on a rejoin).
        var me = PhotonNetwork.LocalPlayer;
        if (me != null)
        {
            nameInput.SetTextWithoutNotify(Prop(me, BiovalueStatics.PlayerNameKey));
            entityInput.SetTextWithoutNotify(Prop(me, BiovalueStatics.PlayerCompanyKey));
            objectiveInput.SetTextWithoutNotify(Prop(me, BiovalueStatics.PlayerObjectiveKey));
            _locked = !IsGm && IsReady(me) && Filled;
        }
        Refresh();
    }

    private void Update()
    {
        if (_shareAt > 0f && Time.unscaledTime >= _shareAt) Share();
    }

    // ── Player ───────────────────────────────────────────────────────────────

    private bool Filled =>
        !string.IsNullOrWhiteSpace(nameInput.text) && !string.IsNullOrWhiteSpace(entityInput.text) &&
        !string.IsNullOrWhiteSpace(objectiveInput.text);

    private void Typed()
    {
        _shareAt = Time.unscaledTime + ShareDelay; // the GM sees it shortly after they stop typing
        RefreshPlayer();
    }

    // Sends the three fields (and readiness) to everyone.
    private void Share()
    {
        _shareAt = -1f;
        if (!PhotonNetwork.InRoom || IsGm) return;
        string name = nameInput.text.Trim();
        var props = new Hashtable
        {
            [BiovalueStatics.PlayerNameKey] = name,
            [BiovalueStatics.PlayerCompanyKey] = entityInput.text.Trim(),
            [BiovalueStatics.PlayerObjectiveKey] = objectiveInput.text.Trim(),
            [BiovalueStatics.PlayerReadyKey] = _locked,
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        if (name.Length > 0) PhotonNetwork.NickName = name;
    }

    private void GetReady()
    {
        if (_locked || !Filled) return;
        _locked = true;
        Share();
        Refresh();
    }

    private void Edit()
    {
        if (!_locked) return;
        _locked = false;
        Share();
        Refresh();
    }

    private void RefreshPlayer()
    {
        playerTitle.text = _locked ? "You're ready!" : "Who are you playing?";
        playerSub.text = _locked
            ? "Waiting for the game master to start the game. You can still edit your role."
            : "Everyone represents an entity with its own goal. The others will see your card during the game.";

        foreach (var f in new[] { nameInput, entityInput, objectiveInput })
        {
            f.interactable = !_locked;
            Paint(f.transform, "Fill", _locked ? Lilac : Color.white);
            if (f.textComponent) f.textComponent.color = _locked ? Muted : Ink;
        }
        objectiveCount.text = $"{objectiveInput.text.Length} / {ObjectiveLimit}";

        bool filled = Filled;
        readyHint.text = _locked ? "To change something, press Edit: you stop being ready until you press I'm ready again."
                       : filled ? "All set." : "Fill in all three to get ready.";
        editButton.gameObject.SetActive(_locked);
        Paint(readyButton.transform, "", _locked ? OkBg : filled ? Pink : Disabled);
        Paint(readyButton.transform, "Label", _locked ? OkText : filled ? Color.white : Faint);
        var label = readyButton.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label) label.text = _locked ? "Ready" : "I'm ready";
        readyButton.interactable = !_locked;

        // Preview: their number and colour as in the game.
        var me = PhotonNetwork.LocalPlayer;
        int number = me != null ? TurnRibbon.PlayerNumberOf(me.ActorNumber) : 1;
        if (number < 1) number = 1;
        previewBand.color = PlayerCapsuleHook.ColorFor(number);
        Paint(previewBand.transform, "Square", previewBand.color);
        previewNumber.text = number.ToString();
        previewName.text = Or(nameInput.text, "Your name");
        previewEntity.text = Or(entityInput.text, "The entity you represent");
        bool hasObjective = !string.IsNullOrWhiteSpace(objectiveInput.text);
        previewObjective.text = hasObjective ? objectiveInput.text.Trim() : "Your personal objective will show here.";
        previewObjective.color = hasObjective ? Ink : Faint;
        previewObjective.fontStyle = hasObjective ? FontStyles.Normal : FontStyles.Italic;

        readyBanner.SetActive(_locked);
        if (_locked)
        {
            var players = Players();
            othersReady.text = players.Count <= 1 ? "You're the only player so far."
                : $"{players.Count(IsReady)} of {players.Count} players ready";
        }
    }

    // ── Game master ──────────────────────────────────────────────────────────

    private void RefreshGm()
    {
        var players = Players();
        int ready = players.Count(IsReady);
        bool all = players.Count > 0 && ready == players.Count;
        readyCount.text = $"{ready} of {players.Count} ready";
        readyCount.color = all ? OkText : Color.white;
        readyCountPill.color = all ? OkBg : Glass;

        while (_cards.Count < players.Count)
        {
            var card = Instantiate(cardTemplate, cardList);
            _cards.Add(card);
        }
        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].SetActive(i < players.Count);
            if (i < players.Count) FillCard(_cards[i].transform, players[i]);
        }

        var waiting = players.Where(p => !IsReady(p)).Select(NameOf).ToList();
        gmHint.text = players.Count == 0 ? "No players in the room."
                    : all ? "Everyone is ready." : "Waiting for " + string.Join(", ", waiting) + ".";
        startButton.gameObject.SetActive(all);
        startAnywayButton.gameObject.SetActive(!all && players.Count > 0);
    }

    private static void FillCard(Transform t, Player p)
    {
        int number = TurnRibbon.PlayerNumberOf(p.ActorNumber);
        var color = PlayerCapsuleHook.ColorFor(Mathf.Max(1, number));
        Paint(t, "Side", color);
        Paint(t, "Side/Square", color);
        Text(t, "Side/Number", number.ToString(), Color.white);
        Text(t, "Name", NameOf(p), Ink);

        string entity = Prop(p, BiovalueStatics.PlayerCompanyKey).Trim();
        string objective = Prop(p, BiovalueStatics.PlayerObjectiveKey).Trim();
        Text(t, "Entity", entity.Length > 0 ? entity : "No entity yet", entity.Length > 0 ? Muted : Faint);
        Text(t, "Objective", objective.Length > 0 ? objective : "Still writing their objective…", objective.Length > 0 ? Ink : Faint);
        SetItalic(t, "Entity", entity.Length == 0);
        SetItalic(t, "Objective", objective.Length == 0);

        bool ready = IsReady(p), writing = !ready && (entity.Length > 0 || objective.Length > 0);
        Paint(t, "Status", ready ? OkBg : writing ? WritingBg : Disabled);
        Text(t, "Status/Label", ready ? "Ready" : writing ? "Writing…" : "Not started", ready ? OkText : writing ? WritingText : Muted);
        var tick = t.Find("Status/Tick");
        if (tick) tick.gameObject.SetActive(ready);
    }

    private void StartGame()
    {
        if (!IsGm || _starting || Players().Count == 0) return;
        _starting = true;
        PhotonNetwork.AutomaticallySyncScene = true; // the players follow the master
        PhotonNetwork.LoadLevel(NextScene);
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        bool gm = IsGm;
        playerScreen.SetActive(!gm);
        gmScreen.SetActive(gm);
        if (PhotonNetwork.InRoom)
        {
            roomLabel.text = "Room <b><color=#FFFFFF>" + PhotonNetwork.CurrentRoom.Name + "</color></b>";
            string objective = RoomProp(BiovalueStatics.GameObjectiveKey);
            gameObjective.text = objective.Length > 0 ? objective : "Set by the game master";
            if (materialsButton) materialsButton.gameObject.SetActive(RoomProp(BiovalueStatics.LinkKey).Length > 0);
        }
        if (gm) RefreshGm();
        else RefreshPlayer();
    }

    private void OpenMaterials()
    {
        string link = RoomProp(BiovalueStatics.LinkKey);
        if (link.Length == 0) return;
        if (!link.Contains("://")) link = "https://" + link;
        Application.OpenURL(link);
    }

    // Everyone but the game master, in player order. Dropped players (kept for 60 s so they can
    // rejoin) don't count.
    private static List<Player> Players() =>
        PhotonNetwork.InRoom
            ? PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient && !p.IsInactive).OrderBy(p => p.ActorNumber).ToList()
            : new List<Player>();

    private static bool IsReady(Player p) =>
        p.CustomProperties.TryGetValue(BiovalueStatics.PlayerReadyKey, out var r) && r is bool b && b;

    private static string NameOf(Player p)
    {
        string name = Prop(p, BiovalueStatics.PlayerNameKey).Trim();
        if (name.Length == 0) name = p.NickName;
        return string.IsNullOrWhiteSpace(name) ? "Player " + TurnRibbon.PlayerNumberOf(p.ActorNumber) : name;
    }

    private static string Prop(Player p, string key) =>
        p.CustomProperties.TryGetValue(key, out var v) && v is string s ? s : "";

    private static string RoomProp(string key) =>
        PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out var v) && v is string s ? s.Trim() : "";

    private static string Or(string s, string fallback) => string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();

    private static void Paint(Transform root, string path, Color color)
    {
        var t = string.IsNullOrEmpty(path) ? root : root.Find(path);
        var g = t ? t.GetComponent<Graphic>() : null;
        if (g) g.color = color;
    }

    private static void Text(Transform root, string path, string text, Color color)
    {
        var t = root.Find(path)?.GetComponent<TMP_Text>();
        if (!t) return;
        t.text = text;
        t.color = color;
    }

    private static void SetItalic(Transform root, string path, bool on)
    {
        var t = root.Find(path)?.GetComponent<TMP_Text>();
        if (t) t.fontStyle = on ? FontStyles.Italic : FontStyles.Normal;
    }

    // ── Photon ───────────────────────────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer) => Refresh();
    public override void OnPlayerLeftRoom(Player otherPlayer) => Refresh();
    public override void OnMasterClientSwitched(Player newMasterClient) => Refresh();
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => Refresh();
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) => Refresh();
}

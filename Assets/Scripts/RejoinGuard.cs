using System.Collections;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lets a player who drops out of a game take their seat back.
//  - Every client connects with a user id that stays the same across a reconnect (Photon needs it to give
//    the seat back): on the web, one kept by the browser tab (it survives a reload); elsewhere, one per launch.
//  - The game being played (room, user id, last time seen) is remembered on the device: the main menu offers
//    "Rejoin" while the room still keeps the seat (SeatMinutes, the room's PlayerTtl).
//  - Losing the connection during a game shows "Connection lost · Getting you back…" and tries to get back
//    in; once back, the game scene loads again and rebuilds itself from the room (pieces, cards, votes, …).
// Creates itself before the first scene: nothing to build.
public class RejoinGuard : MonoBehaviourPunCallbacks
{
    public const int SeatMinutes = 10;                         // how long the room keeps a dropped player's seat
    private const string LastGameKey = "biovalue_last_game";   // "room|user id|unix seconds last seen"
    private const float RetryEvery = 3f, GiveUpAfter = 60f;    // automatic tries, then "Try again"
    private const int MainMenuScene = 0;

    public static RejoinGuard Instance { get; private set; }

    // Set by the main menu's "Rejoin": PhotonConnection rejoins this room instead of opening the lobby.
    public static string PendingRejoin { get; private set; }

    private bool _inGame;          // last frame: in a room whose game has started
    private string _room;
    private bool _reconnecting;
    private float _saveIn;
    private Coroutine _retry;
    private Overlay _overlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance) return;
        var go = new GameObject("RejoinGuard");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<RejoinGuard>();
        string id = WebBridge.TabId();
        UseUserId(string.IsNullOrEmpty(id) ? System.Guid.NewGuid().ToString("N") : id);
    }

    // Takes effect on the next connect.
    private static void UseUserId(string id) => PhotonNetwork.AuthValues = new AuthenticationValues(id);

    private static long Now => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // ── The game this device was playing ─────────────────────────────────────

    private static bool TryReadLastGame(out string room, out string userId)
    {
        room = userId = null;
        var parts = PlayerPrefs.GetString(LastGameKey, "").Split('|');
        if (parts.Length != 3 || !long.TryParse(parts[2], out long seen)) return false;
        if (Now - seen > SeatMinutes * 60 - 15) return false; // the room has given the seat away by now
        room = parts[0];
        userId = parts[1];
        return room.Length > 0 && userId.Length > 0;
    }

    // Is there a game this device can still go back to? (the main menu's "Rejoin")
    public static bool TryGetLastGame(out string room) => TryReadLastGame(out room, out _);

    private void Remember()
    {
        if (string.IsNullOrEmpty(_room) || PhotonNetwork.AuthValues == null) return;
        PlayerPrefs.SetString(LastGameKey, $"{_room}|{PhotonNetwork.AuthValues.UserId}|{Now}");
        PlayerPrefs.Save();
    }

    // Left on purpose (quit, the game ended for everyone): nothing to go back to.
    public static void Forget()
    {
        PlayerPrefs.DeleteKey(LastGameKey);
        PlayerPrefs.Save();
    }

    // The main menu's "Rejoin": the next connection uses the id that holds the seat and rejoins the room.
    public static bool PrepareRejoin()
    {
        if (!TryReadLastGame(out var room, out var userId)) return false;
        UseUserId(userId);
        PendingRejoin = room;
        return true;
    }

    // The room or the seat is gone: carry on to the lobby as usual.
    public static void RejoinFailed()
    {
        PendingRejoin = null;
        Forget();
    }

    // ── Once-per-game steps (spawning) ───────────────────────────────────────

    // A client loading the game again (after rejoining) mustn't spawn what's already in the room.
    public static bool AlreadyDone(string key) =>
        PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);

    public static void MarkDone(string key)
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { key, true } });
    }

    // ── Watching the connection ──────────────────────────────────────────────

    private void Update()
    {
        bool inGame = PhotonNetwork.InRoom && GameAuthority.GmActor > 0;
        if (inGame)
        {
            _room = PhotonNetwork.CurrentRoom.Name;
            _saveIn -= Time.unscaledDeltaTime;
            if (!_inGame || _saveIn <= 0f)
            {
                Remember();
                _saveIn = 10f;
            }
        }
        _inGame = inGame;
        _overlay?.Spin();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && _inGame) Remember();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        bool wasInGame = _inGame;
        _inGame = false;
        if (_reconnecting || !wasInGame) return; // the retries carry on by themselves
        if (cause == DisconnectCause.DisconnectByClientLogic)
        {
            Forget(); // quit, or sent back to the main menu
            return;
        }
        Debug.LogWarning($"[RejoinGuard] Lost the game ({cause}): reconnecting.");
        StartReconnecting();
    }

    private void StartReconnecting()
    {
        _reconnecting = true;
        if (_retry != null) StopCoroutine(_retry);
        _retry = StartCoroutine(Reconnect());
    }

    private IEnumerator Reconnect()
    {
        Show(Overlay.State.Trying);
        float started = Time.unscaledTime;
        while (_reconnecting && Time.unscaledTime - started < GiveUpAfter)
        {
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected && !PhotonNetwork.ReconnectAndRejoin())
                PhotonNetwork.ConnectUsingSettings(); // OnConnectedToMaster rejoins
            yield return new WaitForSecondsRealtime(RetryEvery);
        }
        _retry = null;
        if (_reconnecting) Show(Overlay.State.Failed);
    }

    public override void OnConnectedToMaster()
    {
        if (_reconnecting && !string.IsNullOrEmpty(_room)) PhotonNetwork.RejoinRoom(_room);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (!_reconnecting) return;
        Debug.LogWarning($"[RejoinGuard] Couldn't rejoin ({returnCode}): {message}");
        // Still connecting to the game server: the next try deals with it. Otherwise the seat is gone.
        if (returnCode == ErrorCode.GameDoesNotExist || returnCode == ErrorCode.JoinFailedWithRejoinerNotFound ||
            returnCode == ErrorCode.JoinFailedPeerAlreadyJoined || returnCode == ErrorCode.GameClosed)
        {
            _reconnecting = false;
            if (_retry != null) StopCoroutine(_retry);
            _retry = null;
            Forget();
            Show(Overlay.State.Gone);
        }
    }

    public override void OnJoinedRoom()
    {
        PendingRejoin = null;
        _room = PhotonNetwork.CurrentRoom.Name;
        if (!_reconnecting) return;

        _reconnecting = false;
        if (_retry != null) StopCoroutine(_retry);
        _retry = null;
        _overlay?.Hide();
        Debug.Log("[RejoinGuard] Back in the game: loading it again from the room.");
        // The pieces and cards were cleared when the connection dropped: the scene builds itself again.
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }

    private void TryAgain() => StartReconnecting();

    private void BackToMenu()
    {
        _reconnecting = false;
        if (_retry != null) StopCoroutine(_retry);
        _retry = null;
        _overlay?.Hide();
        if (PhotonNetwork.NetworkClientState != ClientState.Disconnected) PhotonNetwork.Disconnect();
        SceneManager.LoadScene(MainMenuScene); // the menu still offers "Rejoin" while the seat is kept
    }

    private void Show(Overlay.State state)
    {
        if (_overlay == null) _overlay = new Overlay(transform, TryAgain, BackToMenu);
        _overlay.Show(state);
    }

    // ── "Connection lost" card, made in code (it outlives the scenes) ────────

    private class Overlay
    {
        public enum State { Trying, Failed, Gone }

        private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
        private static readonly Color Muted = new Color32(0x6B, 0x63, 0x80, 0xFF);
        private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
        private static readonly Color PinkBg = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
        private static readonly Color PinkText = new Color32(0x8A, 0x14, 0x50, 0xFF);

        private readonly GameObject _root;
        private readonly RectTransform _card, _spinner;
        private readonly TMP_Text _title, _body;
        private readonly GameObject _tryAgain, _menu;

        public Overlay(Transform parent, System.Action tryAgain, System.Action menu)
        {
            _root = new GameObject("ConnectionLost", typeof(RectTransform));
            _root.transform.SetParent(parent, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var dim = Rect("Dim", _root.transform);
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            dim.gameObject.AddComponent<Image>().color = new Color32(0x16, 0x10, 0x2C, 0xC0); // blocks the game

            _card = Rect("Card", dim);
            _card.sizeDelta = new Vector2(620, 330);
            var card = _card.gameObject.AddComponent<Image>();
            card.sprite = Rounded(64, 28);
            card.type = Image.Type.Sliced;

            _spinner = Rect("Spinner", _card);
            _spinner.anchorMin = _spinner.anchorMax = new Vector2(0.5f, 1f);
            _spinner.anchoredPosition = new Vector2(0, -70);
            _spinner.sizeDelta = new Vector2(64, 64);
            var ring = _spinner.gameObject.AddComponent<Image>();
            ring.sprite = Ring(96);
            ring.color = Pink;

            _title = Text("Title", _card, "ExtraBold", 36, Ink, -130, 48);
            _body = Text("Body", _card, "Medium", 22, Muted, -180, 64);

            _tryAgain = Button("TryAgain", _card, "Try again", Pink, Color.white, tryAgain);
            _menu = Button("MainMenu", _card, "Main menu", PinkBg, PinkText, menu);
            _root.SetActive(false);
        }

        public void Show(State state)
        {
            _root.SetActive(true);
            _spinner.gameObject.SetActive(state == State.Trying);
            _tryAgain.SetActive(state == State.Failed);
            _menu.SetActive(state != State.Trying);
            switch (state)
            {
                case State.Trying:
                    _title.text = "Connection lost";
                    _body.text = "Getting you back into the game…";
                    break;
                case State.Failed:
                    _title.text = "Still can't reach the game";
                    _body.text = $"Check your internet connection. Your seat is kept for {SeatMinutes} minutes.";
                    break;
                default:
                    _title.text = "You can't rejoin this game";
                    _body.text = "It has ended, or you were away for too long.";
                    break;
            }

            // Spinner up top while trying; the buttons along the bottom otherwise.
            bool buttons = state != State.Trying;
            _card.sizeDelta = new Vector2(620, buttons ? 300 : 290);
            float titleY = buttons ? -48 : -130;
            ((RectTransform)_title.transform).anchoredPosition = new Vector2(0, titleY);
            ((RectTransform)_body.transform).anchoredPosition = new Vector2(0, titleY - 52);
            var menuRt = (RectTransform)_menu.transform;
            var tryRt = (RectTransform)_tryAgain.transform;
            bool both = state == State.Failed;
            menuRt.anchoredPosition = new Vector2(both ? -116 : 0, 40);
            tryRt.anchoredPosition = new Vector2(116, 40);
        }

        public void Hide() => _root.SetActive(false);

        public void Spin()
        {
            if (_root.activeSelf && _spinner.gameObject.activeSelf)
                _spinner.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 300f);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static TMP_FontAsset Font(string weight) =>
            Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.Contains("Baloo") && f.name.Contains(weight))
            ?? TMP_Settings.defaultFontAsset;

        private static TMP_Text Text(string name, RectTransform parent, string weight, float size, Color color, float y, float h)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(540, h);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = Font(weight);
            if (font) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject Button(string name, RectTransform parent, string label, Color fill, Color ink, System.Action onClick)
        {
            var rt = Rect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(212, 60);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Rounded(64, 30);
            image.type = Image.Type.Sliced;
            image.color = fill;
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            var text = Text("Label", rt, "ExtraBold", 22, ink, 0, 60);
            var textRt = (RectTransform)text.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            text.text = label;
            return rt.gameObject;
        }

        // White rounded square, sliced (corner radius r).
        private static Sprite Rounded(int size, int r)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, size - r), cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        // A three-quarter ring, for the spinner.
        private static Sprite Ring(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float outer = size / 2f - 1f, inner = outer - size * 0.12f, c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle > 0f && angle < 90f) a *= 0.15f; // the gap
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}

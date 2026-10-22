using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCapsuleHook : MonoBehaviourPun
{
    [SerializeField] public TMP_Text _playerName;
    [SerializeField] private TMP_Text _playerID;
    [SerializeField] private GameObject _IsMyTurnIndicator;
    [SerializeField] private Image _notMyTurnOverlay;
    [SerializeField] private Image _capsuleImage;

    [Header("Redesigned capsule (BioValue/Build Player Capsules)")]
    [SerializeField] private Image _badge;          // round avatar background, in the player's colour
    [SerializeField] private GameObject _turnGlow;  // pink outline shown on this player's turn
    [SerializeField] private RectTransform _nextTag; // "NEXT" on the player who plays after the current one
    [SerializeField] private Component[] _playerColored; // graphics/outlines also drawn in the player's colour

    [Header("Vote indicators (optional — auto-created if left empty)")]
    [SerializeField] private GameObject _approveIndicator; // green GO — shown when this player voted Yes
    [SerializeField] private GameObject _rejectIndicator;  // red GO   — shown when this player voted No

    public int _id;

    // 1-based player number (the GM is 0 and has no capsule). Instruments use it as their ownerId.
    public int PlayerNumber { get; private set; }
    public string PlayerName { get; private set; } = "";
    public Color PlayerColor => ColorFor(PlayerNumber);
    public PlayedCardsSpawner CardSpawner => cardSpawner ? cardSpawner : GetComponent<PlayedCardsSpawner>();

    public static readonly Color[] PlayerColors =
    {
        new Color32(0x7A, 0x5B, 0xA6, 0xFF), new Color32(0x2F, 0x6F, 0xB0, 0xFF),
        new Color32(0xD9, 0x57, 0x7C, 0xFF), new Color32(0x3E, 0x9C, 0x8F, 0xFF),
        new Color32(0xC2, 0x55, 0x2B, 0xFF), new Color32(0x5B, 0x6B, 0xD6, 0xFF),
        new Color32(0x9C, 0x6B, 0x3E, 0xFF), new Color32(0x6E, 0x8F, 0x2E, 0xFF),
    };

    public static Color ColorFor(int playerNumber) =>
        PlayerColors[((playerNumber - 1) % PlayerColors.Length + PlayerColors.Length) % PlayerColors.Length];

    [SerializeField] private PlayedCardsSpawner cardSpawner;

    // Fallback: a single Image whose colour flips green/red when no GOs are wired above.
    private Image _voteImage;
    private Color _capsuleDefaultColor;

    private static readonly Color ActiveTurnColor = new Color(0.831f, 0.467f, 0.145f); // #D47725

    private void Start()
    {
        if (cardSpawner == null)
            cardSpawner = GetComponent<PlayedCardsSpawner>();

        if (_capsuleImage != null)
            _capsuleDefaultColor = _capsuleImage.color;

        // Second-chance subscribe in case VotingManager wasn't ready during SetPlayerInfo.
        SubscribeToVoting();
    }

    public void SetPlayerInfo(string playerName, int playerID) => SetPlayerInfo(playerName, playerID, playerID - 1);

    public void SetPlayerInfo(string playerName, int playerID, int playerNumber)
    {
        _id = playerID;
        PlayerNumber = playerNumber;
        PlayerName = playerName ?? "";
        if (_playerName) _playerName.text = playerName;
        if (_playerID)   _playerID.text   = PlayerNumber.ToString();
        if (_badge)      _badge.color     = PlayerColor;
        if (_playerColored != null)
            foreach (var c in _playerColored)
            {
                if (c is Graphic g) g.color = PlayerColor;
                else if (c is Shadow s) s.effectColor = PlayerColor; // Outline is a Shadow
            }

        GameState.Instance.onPlayerTurnIndexChanged.RemoveListener(OnPlayerIndexChanged);
        GameState.Instance.onPlayerTurnIndexChanged.AddListener(OnPlayerIndexChanged);
        GameState.Instance.onGamePhaseChanged.RemoveListener(OnGamePhaseChanged);
        GameState.Instance.onGamePhaseChanged.AddListener(OnGamePhaseChanged);
        OnPlayerIndexChanged(GameState.Instance.GetCurrentPlayerTurnIndex());

        if (cardSpawner != null)
            cardSpawner.SetupContent(_id);

        EnsureVoteImage();
        SubscribeToVoting();
        HideVoteIndicator();
    }

    // ── Turn indicator ────────────────────────────────────────────────────────

    public void OnPlayerIndexChanged(int actorNumber)
    {
        bool isMyTurn = actorNumber == _id;

        if (_IsMyTurnIndicator != null)
            _IsMyTurnIndicator.SetActive(isMyTurn);

        if (_notMyTurnOverlay != null)
            _notMyTurnOverlay.gameObject.SetActive(!isMyTurn);

        if (_turnGlow != null)
            _turnGlow.SetActive(isMyTurn);

        RefreshNextTag();

        // Old look: the whole capsule turns orange with white text.
        if (_capsuleImage != null)
        {
            _capsuleImage.color = isMyTurn ? ActiveTurnColor : _capsuleDefaultColor;
            if (_playerName != null)
                _playerName.color = isMyTurn ? Color.white : Color.black;
        }
    }

    private void OnGamePhaseChanged(GameState.GamePhase phase) => RefreshNextTag();

    // Shown only while instruments are being placed, which is when turns matter.
    private void RefreshNextTag()
    {
        if (!_nextTag || GameState.Instance == null) return;
        var phase = GameState.Instance.GetCurrentGamePhase();
        bool placing = phase == GameState.GamePhase.InstrumentSelection || phase == GameState.GamePhase.None;
        bool show = placing && _id > 0 && GameState.Instance.PeekNextTurnActor() == _id
                    && GameState.Instance.GetCurrentPlayerTurnIndex() != _id;
        _nextTag.gameObject.SetActive(show);
        if (!show) return;

        // Above the capsules at the bottom of the screen, below the ones at the top.
        var canvas = GetComponentInParent<Canvas>();
        bool atTop = canvas && transform.position.y > canvas.rootCanvas.transform.position.y;
        float y = atTop ? 0f : 1f;
        _nextTag.anchorMin = _nextTag.anchorMax = new Vector2(0.5f, y);
        _nextTag.pivot = new Vector2(0.5f, 1f - y);
        _nextTag.anchoredPosition = new Vector2(0f, atTop ? -6f : 6f);
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    // Opens the player details window; without one in the scene, opens their played cards as before.
    public void OpenDetails()
    {
        if (PlayerDetailsPanel.Instance != null) PlayerDetailsPanel.Instance.Open(this);
        else if (cardSpawner != null) cardSpawner.OnClickSpawnCards();
    }

    // ── Vote indicator ────────────────────────────────────────────────────────

    // Creates a small coloured dot at the top-right of the capsule when no
    // custom indicator GameObjects are wired in the Inspector.
    private void EnsureVoteImage()
    {
        if (_approveIndicator != null || _rejectIndicator != null) return; // user wired their own
        if (_voteImage != null) return;

        var go = new GameObject("VoteIndicator");
        go.transform.SetParent(transform, worldPositionStays: false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(20f, 20f);
        rt.anchoredPosition = new Vector2(-4f, -4f); // small offset from top-right corner

        _voteImage = go.AddComponent<Image>();
        go.SetActive(false);
    }

    private void SubscribeToVoting()
    {
        if (VotingManager.Instance == null) return;

        // Remove before adding to avoid duplicates on repeated calls.
        VotingManager.Instance.OnPlayerVoted.RemoveListener(OnPlayerVoted);
        VotingManager.Instance.OnVotesChanged.RemoveListener(RefreshVoteIndicator);

        VotingManager.Instance.OnPlayerVoted.AddListener(OnPlayerVoted);
        VotingManager.Instance.OnVotesChanged.AddListener(RefreshVoteIndicator);
    }

    // With several instruments under vote the token shows once the player voted on all of them:
    // a tick if all yes, a cross if all no, nothing for a mix (the ribbon's dot shows that in purple).
    private VotingManager.Summary _shownVote = VotingManager.Summary.NotYet;

    private void OnPlayerVoted(int actorNumber, bool approved)
    {
        if (actorNumber != _id) return;
        _shownVote = VotingManager.Summary.NotYet; // pop again, also when a player changes their vote
        RefreshVoteIndicator();
    }

    private void RefreshVoteIndicator()
    {
        var summary = VotingManager.Instance ? VotingManager.Instance.SummaryOf(_id) : VotingManager.Summary.NotYet;
        if (summary == _shownVote) return;
        _shownVote = summary;
        if (summary == VotingManager.Summary.Yes) ShowVoteIndicator(true);
        else if (summary == VotingManager.Summary.No) ShowVoteIndicator(false);
        else HideVoteIndicator();
    }

    private void ShowVoteIndicator(bool approved)
    {
        if (_approveIndicator != null || _rejectIndicator != null)
        {
            // Use the manually-wired GameObjects.
            if (_approveIndicator) _approveIndicator.SetActive(approved);
            if (_rejectIndicator)  _rejectIndicator.SetActive(!approved);
            Pop(approved ? _approveIndicator : _rejectIndicator); // also when a player changes their vote
            return;
        }

        // Fallback: single dot that turns green or red.
        if (_voteImage == null) EnsureVoteImage();
        if (_voteImage != null)
        {
            _voteImage.gameObject.SetActive(true);
            _voteImage.color = approved
                ? new Color(0.22f, 0.78f, 0.22f)   // green
                : new Color(0.88f, 0.22f, 0.22f);   // red
        }
    }

    private static void Pop(GameObject token)
    {
        if (!token) return;
        LeanTween.cancel(token);
        token.transform.localScale = Vector3.one * 0.4f;
        LeanTween.scale(token, Vector3.one, 0.3f).setEaseOutBack().setIgnoreTimeScale(true);
    }

    private void HideVoteIndicator()
    {
        if (_approveIndicator) _approveIndicator.SetActive(false);
        if (_rejectIndicator)  _rejectIndicator.SetActive(false);
        if (_voteImage != null) _voteImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.onPlayerTurnIndexChanged.RemoveListener(OnPlayerIndexChanged);
            GameState.Instance.onGamePhaseChanged.RemoveListener(OnGamePhaseChanged);
        }
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnPlayerVoted.RemoveListener(OnPlayerVoted);
        VotingManager.Instance.OnVotesChanged.RemoveListener(RefreshVoteIndicator);
    }

    // ── Network ───────────────────────────────────────────────────────────────

    public void SetPlayerInfoInNetwork(string playerName, int playerID)
    {
        StartCoroutine(SetPlayerInfoInNetwork_Async(playerName, playerID));
    }

    private IEnumerator SetPlayerInfoInNetwork_Async(string playerName, int playerID)
    {
        yield return new WaitForSeconds(0.5f);
        if (photonView)
            photonView.RPC(nameof(RPC_SetPlayerInfo), RpcTarget.All, playerName, playerID);
        else
            SetPlayerInfo(playerName, playerID);
    }

    [PunRPC]
    public void RPC_SetPlayerInfo(string playerName, int playerID)
    {
        SetPlayerInfo(playerName, playerID);
    }
}

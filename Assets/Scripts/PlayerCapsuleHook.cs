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

    [Header("Vote indicators (optional — auto-created if left empty)")]
    [SerializeField] private GameObject _approveIndicator; // green GO — shown when this player voted Yes
    [SerializeField] private GameObject _rejectIndicator;  // red GO   — shown when this player voted No

    public int _id;

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

    public void SetPlayerInfo(string playerName, int playerID)
    {
        _id = playerID;
        if (_playerName) _playerName.text = playerName;
        if (_playerID)   _playerID.text   = (_id - 1).ToString();

        GameState.Instance.onPlayerTurnIndexChanged.AddListener(OnPlayerIndexChanged);
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

        if (_playerName != null)
            _playerName.color = isMyTurn ? Color.white : Color.black;

        if (_capsuleImage != null)
            _capsuleImage.color = isMyTurn ? ActiveTurnColor : _capsuleDefaultColor;
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
        VotingManager.Instance.OnVoteStarted.RemoveListener(HideVoteIndicator);
        VotingManager.Instance.OnPlayerVoted.RemoveListener(OnPlayerVoted);
        VotingManager.Instance.OnVoteEnded.RemoveListener(HideVoteIndicator);

        VotingManager.Instance.OnVoteStarted.AddListener(HideVoteIndicator);
        VotingManager.Instance.OnPlayerVoted.AddListener(OnPlayerVoted);
        VotingManager.Instance.OnVoteEnded.AddListener(HideVoteIndicator);
    }

    private void OnPlayerVoted(int actorNumber, bool approved)
    {
        if (actorNumber != _id) return;
        ShowVoteIndicator(approved);
    }

    private void ShowVoteIndicator(bool approved)
    {
        if (_approveIndicator != null || _rejectIndicator != null)
        {
            // Use the manually-wired GameObjects.
            if (_approveIndicator) _approveIndicator.SetActive(approved);
            if (_rejectIndicator)  _rejectIndicator.SetActive(!approved);
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

    private void HideVoteIndicator()
    {
        if (_approveIndicator) _approveIndicator.SetActive(false);
        if (_rejectIndicator)  _rejectIndicator.SetActive(false);
        if (_voteImage != null) _voteImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnVoteStarted.RemoveListener(HideVoteIndicator);
        VotingManager.Instance.OnPlayerVoted.RemoveListener(OnPlayerVoted);
        VotingManager.Instance.OnVoteEnded.RemoveListener(HideVoteIndicator);
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

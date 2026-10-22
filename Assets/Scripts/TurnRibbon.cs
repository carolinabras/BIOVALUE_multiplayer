using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: the white pill at the top of the board that says whose turn it is and who plays next.
//   Placing:  [avatar] NOW · Ana is placing        | NEXT (3) Bruno   [End turn >]
//   Vote:     [avatar] VOTE · Ana's instrument ●●○ | NEXT (3) Bruno   [End vote · next turn >]
//   Actions:  [cards]  ACTION CARDS ROUND · ...                       [End actions round >]
//   Collab:   [person] COLLABORATION ROUND · ...  ●●●○○ 3 tokens left  [I'm done]  (player)
//             [person] COLLABORATION ROUND · 4 of 7 players done  ●●●●○○○  [End collaboration >] (GM)
//   Result:   (green/red) [tick] VOTE PASSED · Density Bonuses stays ●●●○ 4 yes · 1 no, for a few
//             seconds, while an APPROVED / REJECTED stamp lands on the piece (a rejected piece wobbles,
//             then goes back).
// The pink button is only there for the GM. "End turn" advances the turn; during a vote it closes the
// vote first (VotingManager.EndTurn), exactly like the old End Turn button.
// Built by the editor menu "BioValue/Build Turn Ribbon and GM Panel".
public class TurnRibbon : MonoBehaviourPunCallbacks
{
    public RectTransform pill;
    public Image pillImage;

    [Header("Now")]
    public Image nowAvatar;
    public GameObject nowPerson;       // person icon, hidden in the action cards round
    public GameObject nowCards;        // cards icon, shown in the action cards round
    public TMP_Text nowEyebrow;
    public TMP_Text nowText;
    public GameObject resultTick;      // in the avatar, while showing a vote result
    public GameObject resultCross;

    [Header("Vote")]
    public GameObject voteGroup;
    public RectTransform voteDots;
    public GameObject voteDotTemplate;
    public TMP_Text voteText;

    [Header("Next")]
    public GameObject nextGroup;
    public Image nextFill;
    public Image nextAvatar;
    public TMP_Text nextNumber;
    public TMP_Text nextName;

    [Header("GM")]
    public GameObject endTurnButton;   // End turn / End vote · next turn
    public TMP_Text endTurnLabel;
    public GameObject endActionsButton; // starts the collaboration round
    public GameObject endCollabButton;  // wired in the scene like the old "End Actions Round" button

    [Header("Collaboration")]
    public GameObject doneButton;       // players: "I'm done" / "Done · Undo"
    public Image doneButtonImage;
    public TMP_Text doneLabel;
    public GameObject doneGlow;         // pulses around "I'm done" when there is nothing left to collaborate on
    public GameObject endActionsGlow;   // pulses around the GM's "End actions round" once everyone has played

    [Header("Vote result")]
    public float resultSeconds = 3.5f;
    public RectTransform stampHolder;  // moved onto the piece
    public RectTransform stamp;        // tilted APPROVED / REJECTED label
    public CanvasGroup stampGroup;
    public Image stampBorder;
    public TMP_Text stampLabel;
    public RectTransform tally;        // "4 yes · 1 no" chip under the piece
    public TMP_Text tallyLabel;

    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PinkLight = new Color32(0xFF, 0xE3, 0xF1, 0xFF);
    private static readonly Color Lilac = new Color32(0xF7, 0xF4, 0xFC, 0xFF);
    private static readonly Color CardsOrange = new Color32(0xE9, 0x8A, 0x1F, 0xFF);
    private static readonly Color Yes = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color No = new Color32(0xD9, 0x3A, 0x3A, 0xFF);
    private static readonly Color Empty = new Color32(0xDD, 0xD6, 0xEA, 0xFF);
    private static readonly Color CollabBlue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);
    private static readonly Color DoneSoft = new Color32(0xE3, 0xF5, 0xE8, 0xFF);
    private static readonly Color DoneInk = new Color32(0x1E, 0x7A, 0x39, 0xFF);

    private const string EndGameKey = "endGame";

    private bool voting;
    private readonly Dictionary<int, bool> votes = new Dictionary<int, bool>();
    private readonly List<Image> dots = new List<Image>();
    private float nextRefresh;
    private bool subscribed;
    private VotingManager.VoteResult result;
    private float resultUntil;
    private Color eyebrowColor, textColor, voteTextColor;
    private Coroutine stampRoutine;

    private void Start()
    {
        if (voteDotTemplate) voteDotTemplate.SetActive(false);
        if (stampHolder) stampHolder.gameObject.SetActive(false);
        if (nowEyebrow) eyebrowColor = nowEyebrow.color;
        if (nowText) textColor = nowText.color;
        if (voteText) voteTextColor = voteText.color;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (subscribed || VotingManager.Instance == null) return;
        subscribed = true;
        VotingManager.Instance.OnVoteStarted.AddListener(OnVoteStarted);
        VotingManager.Instance.OnPlayerVoted.AddListener(OnPlayerVoted);
        VotingManager.Instance.OnVoteEnded.AddListener(OnVoteEnded);
        VotingManager.Instance.OnVoteResult.AddListener(OnVoteResult);
    }

    private void OnDestroy()
    {
        if (!subscribed || VotingManager.Instance == null) return;
        VotingManager.Instance.OnVoteStarted.RemoveListener(OnVoteStarted);
        VotingManager.Instance.OnPlayerVoted.RemoveListener(OnPlayerVoted);
        VotingManager.Instance.OnVoteEnded.RemoveListener(OnVoteEnded);
        VotingManager.Instance.OnVoteResult.RemoveListener(OnVoteResult);
    }

    private void OnVoteStarted() { voting = true; votes.Clear(); Refresh(); }
    private void OnPlayerVoted(int actor, bool approved) { votes[actor] = approved; Refresh(); }
    private void OnVoteEnded() { voting = false; votes.Clear(); Refresh(); }

    private void OnVoteResult(VotingManager.VoteResult r)
    {
        result = r;
        resultUntil = Time.unscaledTime + resultSeconds;
        Refresh();
        if (stampRoutine != null) StopCoroutine(stampRoutine);
        stampRoutine = StartCoroutine(PlayStamp(r));
    }

    // Turn, phase and the player list change from many places; a cheap poll keeps this in step.
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Subscribe();
        Refresh();
    }

    // ── GM button ────────────────────────────────────────────────────────────

    public void EndTurn()
    {
        if (!GameAuthority.IsGameMaster || GameState.Instance == null) return;
        if (voting && VotingManager.Instance != null) VotingManager.Instance.EndTurn(); // closes the vote, then advances
        else GameState.Instance.GMAdvanceTurn();
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var state = GameState.Instance;
        if (!pill) return;
        if (result != null && Time.unscaledTime < resultUntil) { ShowResult(); return; }
        if (result != null) { result = null; SetResultLook(false, Color.white); }
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        if (state == null || ended) { pill.gameObject.SetActive(false); return; }
        pill.gameObject.SetActive(true);

        var phase = state.GetCurrentGamePhase();
        bool collab = phase == GameState.GamePhase.Collaboration;
        bool actions = phase == GameState.GamePhase.ActionCardPlay || collab;
        bool gm = GameAuthority.IsGameMaster;
        if (nowEyebrow) nowEyebrow.color = collab ? CollabBlue : eyebrowColor;
        if (doneButton) doneButton.SetActive(collab && !gm);
        if (endCollabButton) endCollabButton.SetActive(collab && gm);
        if (collab)
        {
            RefreshCollaboration(gm);
            if (endTurnButton) endTurnButton.SetActive(false);
            if (endActionsButton) endActionsButton.SetActive(false);
            return;
        }
        int me = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        int turn = state.GetCurrentPlayerTurnIndex();
        int next = state.PeekNextTurnActor();

        // Now
        if (nowPerson) nowPerson.SetActive(!actions);
        if (nowCards) nowCards.SetActive(actions);
        if (nowAvatar) nowAvatar.color = actions ? CardsOrange : PlayerCapsuleHook.ColorFor(PlayerNumberOf(turn));
        string eyebrow, text;
        if (actions)
        {
            eyebrow = "ACTION CARDS ROUND";
            var (played, total) = ActionCardsPlayed();
            bool everyone = total > 0 && played >= total;
            text = !gm ? "Choose your action cards"
                 : everyone ? "Everyone has played their cards"
                 : $"{played} of {total} players have played";
            if (endActionsGlow) endActionsGlow.SetActive(gm && everyone);
        }
        else if (voting)
        {
            eyebrow = "VOTE";
            bool gmPlaced = VotingManager.Instance != null && VotingManager.Instance.PlacedByGm;
            text = gmPlaced ? "GM made a special action" : turn == me ? "On your instrument" : turn > 0 ? $"On {GameLog.GetPlayerName(turn)}’s instrument" : "On the new instrument";
        }
        else if (turn == me)
        {
            eyebrow = "YOUR TURN";
            text = "Place one instrument";
        }
        else
        {
            eyebrow = "NOW";
            text = turn > 0 ? $"{GameLog.GetPlayerName(turn)} is placing" : "Waiting to start";
        }
        if (nowEyebrow) nowEyebrow.text = eyebrow;
        if (nowText) nowText.text = text;

        // Vote progress: one dot per voter (everyone but the GM and the player who placed).
        if (voteGroup) voteGroup.SetActive(voting && !actions);
        if (voting && !actions) FillVotes();

        // Next
        bool showNext = !actions && next > 0 && next != turn;
        if (nextGroup) nextGroup.SetActive(showNext);
        if (showNext)
        {
            bool meNext = next == me;
            if (nextFill) nextFill.color = meNext ? PinkLight : Lilac;
            if (nextAvatar) nextAvatar.color = PlayerCapsuleHook.ColorFor(PlayerNumberOf(next));
            if (nextNumber) nextNumber.text = PlayerNumberOf(next).ToString();
            if (nextName) nextName.text = meNext ? "You’re next" : GameLog.GetPlayerName(next);
        }

        // GM
        if (endTurnButton) endTurnButton.SetActive(gm && !actions);
        if (endTurnLabel) endTurnLabel.text = voting ? "End vote · next turn" : "End turn";
        if (endActionsButton) endActionsButton.SetActive(gm && actions);
        if (!actions && endActionsGlow) endActionsGlow.SetActive(false);
    }

    // Action cards round: players (not the GM, not those who left) who have played their cards.
    // Played cards are kept by position in the actor-sorted player list, like GameState.localPlayerIndex.
    public static (int played, int total) ActionCardsPlayed()
    {
        int played = 0, total = 0;
        if (GameState.Instance == null) return (0, 0);
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        for (int i = 0; i < sorted.Length; i++)
        {
            if (sorted[i].IsMasterClient || sorted[i].IsInactive) continue;
            total++;
            if (HasPlayedCards(i)) played++;
        }
        return (played, total);
    }

    public static bool HasPlayedCards(int sortedIndex) =>
        GameState.Instance != null && GameState.Instance.playerActionCards.ContainsKey(sortedIndex);

    // ── Vote result ──────────────────────────────────────────────────────────

    private static Color Accent(bool approved) => approved ? Yes : No;

    private void ShowResult()
    {
        pill.gameObject.SetActive(true);
        SetResultLook(true, Accent(result.approved));
        if (resultTick) resultTick.SetActive(result.approved);
        if (resultCross) resultCross.SetActive(!result.approved);
        if (nowEyebrow) nowEyebrow.text = result.approved ? "VOTE PASSED" : "VOTE FAILED";
        if (nowText) nowText.text = result.instrumentName + (result.approved ? " stays" : " goes back");

        if (voteGroup) voteGroup.SetActive(true);
        while (dots.Count < result.votes.Count && voteDotTemplate && voteDots)
        {
            var go = Instantiate(voteDotTemplate, voteDots);
            go.SetActive(true);
            dots.Add(go.GetComponent<Image>());
        }
        for (int i = 0; i < dots.Count; i++)
        {
            if (!dots[i]) continue;
            dots[i].gameObject.SetActive(i < result.votes.Count);
            if (i < result.votes.Count) dots[i].color = result.votes[i] ? Yes : No;
        }
        if (voteText) voteText.text = $"{result.yes} yes \u00b7 {result.no} no";

        if (nextGroup) nextGroup.SetActive(false);
        if (endTurnButton) endTurnButton.SetActive(false);
        if (endActionsButton) endActionsButton.SetActive(false);
    }

    // Green/red pill with white text and a white avatar holding a tick or cross; or back to normal.
    private void SetResultLook(bool on, Color accent)
    {
        if (pillImage) pillImage.color = on ? accent : Color.white;
        if (on)
        {
            if (nowAvatar) nowAvatar.color = Color.white;
            if (nowPerson) nowPerson.SetActive(false);
            if (nowCards) nowCards.SetActive(false);
        }
        else
        {
            if (resultTick) resultTick.SetActive(false);
            if (resultCross) resultCross.SetActive(false);
        }
        foreach (var icon in new[] { resultTick, resultCross })
            if (icon) foreach (var img in icon.GetComponentsInChildren<Image>(true)) img.color = accent;
        if (nowEyebrow) nowEyebrow.color = on ? new Color(1, 1, 1, 0.85f) : eyebrowColor;
        if (nowText) nowText.color = on ? Color.white : textColor;
        if (voteText) voteText.color = on ? Color.white : voteTextColor;
    }

    // The stamp drops onto the piece (big and faint, then small and tilted), stays, then fades.
    // An approved piece gives a little hop; a rejected one wobbles before VotingManager sends it back.
    private System.Collections.IEnumerator PlayStamp(VotingManager.VoteResult r)
    {
        if (!stampHolder || !r.piece) yield break;
        var accent = Accent(r.approved);
        stampHolder.gameObject.SetActive(true);
        stampHolder.position = r.piece.TransformPoint(r.piece.rect.center);
        if (stampBorder) stampBorder.color = accent;
        if (stampLabel) { stampLabel.text = r.approved ? "APPROVED" : "REJECTED"; stampLabel.color = accent; }
        if (tallyLabel) tallyLabel.text = $"{r.yes} yes \u00b7 {r.no} no";
        if (tally)
        {
            float pieceH = r.piece.rect.height * r.piece.lossyScale.y / Mathf.Max(0.0001f, stampHolder.lossyScale.y);
            tally.anchoredPosition = new Vector2(0f, -pieceH / 2f - 8f);
        }

        var pieceRotation = r.piece.localRotation;
        var pieceScale = r.piece.localScale;
        const float land = 0.35f, hold = 2.4f, fade = 0.4f;
        for (float t = 0f; t < land + hold + fade; t += Time.unscaledDeltaTime)
        {
            if (!stampHolder) yield break;
            float k = Mathf.Clamp01(t / land);
            float scale = k < 0.6f ? Mathf.Lerp(2.2f, 0.9f, k / 0.6f) : Mathf.Lerp(0.9f, 1f, (k - 0.6f) / 0.4f);
            if (stamp)
            {
                stamp.localScale = Vector3.one * scale;
                stamp.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(18f, 8f, k));
            }
            float alpha = t < land ? Mathf.Clamp01(k / 0.6f) : 1f - Mathf.Clamp01((t - land - hold) / fade);
            if (stampGroup) stampGroup.alpha = alpha;

            if (r.piece)
            {
                float u = t - 0.15f;
                if (r.approved && u > 0f && u < 0.3f)
                    r.piece.localScale = pieceScale * (1f + 0.08f * Mathf.Sin(u / 0.3f * Mathf.PI));
                else if (!r.approved && u > 0f && u < 0.5f)
                    r.piece.localRotation = pieceRotation * Quaternion.Euler(0, 0, 7f * Mathf.Sin(u * 40f) * (1f - u / 0.5f));
                else
                {
                    r.piece.localScale = pieceScale;
                    r.piece.localRotation = pieceRotation;
                }
            }
            yield return null;
        }
        if (r.piece) { r.piece.localScale = pieceScale; r.piece.localRotation = pieceRotation; }
        stampHolder.gameObject.SetActive(false);
        stampRoutine = null;
    }

    private void FillVotes()
    {
        int total = Mathf.Max(0, PhotonNetwork.PlayerList.Length - 2);
        while (dots.Count < total && voteDotTemplate && voteDots)
        {
            var go = Instantiate(voteDotTemplate, voteDots);
            go.SetActive(true);
            dots.Add(go.GetComponent<Image>());
        }
        var cast = votes.Values.ToList();
        for (int i = 0; i < dots.Count; i++)
        {
            if (!dots[i]) continue;
            dots[i].gameObject.SetActive(i < total);
            dots[i].color = i < cast.Count ? (cast[i] ? Yes : No) : Empty;
        }
        if (voteText) voteText.text = $"{Mathf.Min(cast.Count, total)} of {total} voted";
    }

    // 1-based position among the players (the GM is 0), the same number the capsules show.
    // Collaboration round: players see their tokens and "I'm done"; the GM sees who is done.
    private void RefreshCollaboration(bool gm)
    {
        if (nowPerson) nowPerson.SetActive(true);
        if (nowCards) nowCards.SetActive(false);
        if (nowAvatar) nowAvatar.color = CollabBlue;
        if (nextGroup) nextGroup.SetActive(false);
        if (nowEyebrow) nowEyebrow.text = "COLLABORATION ROUND";

        var players = PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient).OrderBy(p => p.ActorNumber).ToList();
        if (voteGroup) voteGroup.SetActive(true);
        if (gm)
        {
            int done = players.Count(ColaborationManager.IsDone);
            if (nowText) nowText.text = $"{done} of {players.Count} players done";
            ShowDots(players.Count, i => ColaborationManager.IsDone(players[i])
                ? PlayerCapsuleHook.ColorFor(PlayerNumberOf(players[i].ActorNumber)) : Empty);
            if (voteText) voteText.text = "";
            return;
        }

        bool meDone = ColaborationManager.IsDone(PhotonNetwork.LocalPlayer);
        int tokens = ColaborationManager.MyTokens();
        if (nowText) nowText.text = meDone ? "Waiting for the others" : "Collaborate on other players’ actions";
        ShowDots(ColaborationManager.StartTokens, i => i < tokens ? CollabBlue : Empty);
        if (voteText)
        {
            voteText.color = voteTextColor;
            voteText.text = tokens == 1 ? "1 token left" : $"{tokens} tokens left";
        }
        if (doneLabel)
        {
            doneLabel.text = meDone ? "Done · Undo" : "I’m done";
            doneLabel.color = meDone ? DoneInk : Color.white;
        }
        if (doneButtonImage) doneButtonImage.color = meDone ? DoneSoft : Pink;
        if (doneGlow) doneGlow.SetActive(!meDone && CollaborationRound.NothingLeftToCollaborate());
    }

    private void ShowDots(int count, System.Func<int, Color> color)
    {
        while (dots.Count < count && voteDotTemplate && voteDots)
        {
            var go = Instantiate(voteDotTemplate, voteDots);
            go.SetActive(true);
            dots.Add(go.GetComponent<Image>());
        }
        for (int i = 0; i < dots.Count; i++)
        {
            if (!dots[i]) continue;
            dots[i].gameObject.SetActive(i < count);
            if (i < count) dots[i].color = color(i);
        }
    }

    // Players: "I'm done" toggles whether they have finished collaborating.
    public void ToggleDone()
    {
        if (GameAuthority.IsGameMaster) return;
        ColaborationManager.SetMyDone(!ColaborationManager.IsDone(PhotonNetwork.LocalPlayer));
        Refresh();
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player target, Hashtable changedProps) => Refresh();

    public static int PlayerNumberOf(int actorNumber)
    {
        var sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
        int i = sorted.FindIndex(p => p.ActorNumber == actorNumber);
        return i >= 0 ? i : 0;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps) => Refresh();
}

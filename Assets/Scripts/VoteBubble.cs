using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

// Main game, while instruments are being voted on, on each piece under vote:
//  - a pink hexagon pulses behind the piece, with a pink "VOTING" tag above it (everyone);
//  - a white bubble points at the piece: "Do you agree?" with a red ✗ and a green ✓ (voters; the chosen
//    one fills in and can be changed until the GM ends the vote), or "The others are voting…" for the
//    player who placed it. The GM only sees the ring and the tag (the ribbon has the vote dots).
// Several pieces can be under vote at once: the built set is copied for each extra piece.
// Replaces VotingManager's old voting panel. Built by "BioValue/Build Turn Ribbon and GM Panel".
public class VoteBubble : MonoBehaviour
{
    public static VoteBubble Instance { get; private set; }

    public GameObject content;
    public RectTransform ring;          // pink hexagon behind the piece
    public RectTransform votingTag;
    public RectTransform bubble;
    public RectTransform arrow;         // little white diamond pointing at the piece
    public GameObject voterGroup;       // "Do you agree?" + buttons
    public GameObject waitingGroup;     // "The others are voting…"
    public Button noButton, yesButton;

    public float ringScale = 1.28f;     // ring size against the piece
    public float gap = 22f;             // between the piece and the bubble

    private static readonly Color Yes = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color No = new Color32(0xD9, 0x3A, 0x3A, 0xFF);

    // One set of ring, tag and bubble.
    private class View
    {
        public GameObject content;
        public RectTransform ring, votingTag, bubble, arrow;
        public GameObject voterGroup, waitingGroup;
        public Button noButton, yesButton;
        public Image ringImage;
        public int viewId = -1;
    }

    private readonly List<View> views = new List<View>();
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        Instance = this;
        if (content) content.SetActive(false);
    }

    private void Start()
    {
        if (!content) return;
        var first = new View
        {
            content = content, ring = ring, votingTag = votingTag, bubble = bubble, arrow = arrow,
            voterGroup = voterGroup, waitingGroup = waitingGroup, noButton = noButton, yesButton = yesButton,
            ringImage = ring ? ring.GetComponent<Image>() : null
        };
        Wire(first);
        views.Add(first);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Wire(View v)
    {
        if (v.noButton) v.noButton.onClick.AddListener(() => VotingManager.Instance?.Vote(v.viewId, false));
        if (v.yesButton) v.yesButton.onClick.AddListener(() => VotingManager.Instance?.Vote(v.viewId, true));
    }

    // A copy of the built set for another piece.
    private View Copy()
    {
        var go = Instantiate(content, content.transform.parent);
        var t = go.transform;
        var v = new View
        {
            content = go,
            ring = Same(t, ring) as RectTransform,
            votingTag = Same(t, votingTag) as RectTransform,
            bubble = Same(t, bubble) as RectTransform,
            arrow = Same(t, arrow) as RectTransform,
            voterGroup = Same(t, voterGroup)?.gameObject,
            waitingGroup = Same(t, waitingGroup)?.gameObject,
            noButton = Same(t, noButton)?.GetComponent<Button>(),
            yesButton = Same(t, yesButton)?.GetComponent<Button>()
        };
        v.ringImage = v.ring ? v.ring.GetComponent<Image>() : null;
        Wire(v);
        return v;
    }

    // The object in the copy at the same place as `original` is under content.
    private Transform Same(Transform copy, Object original)
    {
        var t = original is GameObject g ? g.transform : (original as Component)?.transform;
        if (!t) return null;
        var path = new List<string>();
        for (; t && t != content.transform; t = t.parent) path.Insert(0, t.name);
        return path.Count == 0 ? copy : copy.Find(string.Join("/", path));
    }

    private void LateUpdate()
    {
        if (views.Count == 0) return;
        var voting = VotingManager.Instance;
        var ballots = voting && voting.IsVoting
            ? voting.Ballots.Where(b => b.Piece && b.Piece.gameObject.activeInHierarchy).ToList()
            : new List<VotingManager.Ballot>();
        while (views.Count < ballots.Count) views.Add(Copy());

        for (int i = 0; i < views.Count; i++)
        {
            var v = views[i];
            bool show = i < ballots.Count;
            if (v.content && v.content.activeSelf != show) v.content.SetActive(show);
            if (!show) { v.viewId = -1; continue; }
            Fill(v, ballots[i], voting);
        }
    }

    private void Fill(View v, VotingManager.Ballot ballot, VotingManager voting)
    {
        v.viewId = ballot.viewId;
        bool gm = GameAuthority.IsGameMaster;
        bool placer = PhotonNetwork.LocalPlayer != null && ballot.placer == PhotonNetwork.LocalPlayer.ActorNumber;
        bool canVote = voting.CanVoteOn(ballot);
        if (v.voterGroup) v.voterGroup.SetActive(canVote);
        if (v.waitingGroup) v.waitingGroup.SetActive(!canVote && placer);
        if (v.bubble) v.bubble.gameObject.SetActive(canVote || placer);

        Place(v, ballot.Piece, gm && !canVote);
        Pulse(v);
        if (canVote) ShowChoice(v, voting.MyVoteOn(ballot));
    }

    // Ring and tag on the piece, bubble beside it (on the left when the piece is on the right of the screen).
    private void Place(View v, RectTransform piece, bool ringOnly)
    {
        var parent = (RectTransform)v.content.transform.parent;
        piece.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);
        Vector2 centre = (min + max) / 2f;
        Vector2 size = max - min;

        if (v.ring)
        {
            v.ring.anchoredPosition = centre;
            v.ring.sizeDelta = size * ringScale;
        }
        if (v.votingTag) v.votingTag.anchoredPosition = centre + new Vector2(0f, size.y * ringScale / 2f + 4f);

        if (!v.bubble || ringOnly) return;
        bool left = centre.x > parent.rect.center.x + parent.rect.width * 0.15f;
        v.bubble.pivot = new Vector2(left ? 1f : 0f, 0.5f);
        v.bubble.anchoredPosition = centre + new Vector2((size.x / 2f + gap) * (left ? -1f : 1f), 0f);
        if (v.arrow)
        {
            v.arrow.anchorMin = v.arrow.anchorMax = new Vector2(left ? 1f : 0f, 0.5f);
            v.arrow.anchoredPosition = Vector2.zero;
        }
    }

    private static void Pulse(View v)
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.4f);
        if (v.ring) v.ring.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, t);
        if (v.ringImage)
        {
            var c = v.ringImage.color;
            c.a = Mathf.Lerp(0.9f, 0.35f, t);
            v.ringImage.color = c;
        }
    }

    // The chosen button fills in; the other one stays white with a coloured ring.
    private static void ShowChoice(View v, bool? vote)
    {
        Style(v.noButton, No, vote == false);
        Style(v.yesButton, Yes, vote == true);
    }

    private static void Style(Button button, Color color, bool chosen)
    {
        if (!button) return;
        var t = button.transform;
        var inner = t.Find("Inner");
        if (inner) inner.gameObject.SetActive(!chosen);
        var glyph = t.Find("Glyph");
        if (glyph)
            foreach (var bar in glyph.GetComponentsInChildren<Image>(true))
                bar.color = chosen ? Color.white : color;
        t.localScale = Vector3.one * (chosen ? 1.08f : 1f);
    }
}

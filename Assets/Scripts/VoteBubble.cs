using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game, while an instrument is being voted on:
//  - a pink hexagon pulses behind the piece, with a pink "VOTING" tag above it (everyone);
//  - a white bubble points at the piece: "Do you agree?" with a red ✗ and a green ✓ (voters; the chosen
//    one fills in and can be changed until the GM ends the vote), or "The others are voting…" for the
//    player who placed it. The GM only sees the ring and the tag (the ribbon has the vote dots).
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

    private Image ringImage;
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        Instance = this;
        if (ring) ringImage = ring.GetComponent<Image>();
        if (content) content.SetActive(false);
    }

    private void Start()
    {
        if (noButton) noButton.onClick.AddListener(() => VotingManager.Instance?.OnClickReject());
        if (yesButton) yesButton.onClick.AddListener(() => VotingManager.Instance?.OnClickApprove());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        var voting = VotingManager.Instance;
        var piece = voting && voting.IsVoting ? voting.PendingPiece : null;
        bool show = piece && piece.gameObject.activeInHierarchy;
        if (content && content.activeSelf != show) content.SetActive(show);
        if (!show) return;

        bool gm = GameAuthority.IsGameMaster;
        bool placer = PhotonNetwork.LocalPlayer != null && voting.PendingPlacer == PhotonNetwork.LocalPlayer.ActorNumber;
        bool canVote = voting.LocalCanVote;
        if (voterGroup) voterGroup.SetActive(canVote);
        if (waitingGroup) waitingGroup.SetActive(!canVote && placer);
        if (bubble) bubble.gameObject.SetActive(canVote || placer);

        Place(piece, gm && !canVote);
        Pulse();
        if (canVote) ShowChoice(voting.MyVote);
    }

    // Ring and tag on the piece, bubble beside it (on the left when the piece is on the right of the screen).
    private void Place(RectTransform piece, bool ringOnly)
    {
        var parent = (RectTransform)transform;
        piece.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);
        Vector2 centre = (min + max) / 2f;
        Vector2 size = max - min;

        if (ring)
        {
            ring.anchoredPosition = centre;
            ring.sizeDelta = size * ringScale;
        }
        if (votingTag) votingTag.anchoredPosition = centre + new Vector2(0f, size.y * ringScale / 2f + 4f);

        if (!bubble || ringOnly) return;
        bool left = centre.x > parent.rect.center.x + parent.rect.width * 0.15f;
        bubble.pivot = new Vector2(left ? 1f : 0f, 0.5f);
        bubble.anchoredPosition = centre + new Vector2((size.x / 2f + gap) * (left ? -1f : 1f), 0f);
        if (arrow)
        {
            arrow.anchorMin = arrow.anchorMax = new Vector2(left ? 1f : 0f, 0.5f);
            arrow.anchoredPosition = Vector2.zero;
        }
    }

    private void Pulse()
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.4f);
        if (ring) ring.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, t);
        if (ringImage)
        {
            var c = ringImage.color;
            c.a = Mathf.Lerp(0.9f, 0.35f, t);
            ringImage.color = c;
        }
    }

    // The chosen button fills in; the other one stays white with a coloured ring.
    private void ShowChoice(bool? vote)
    {
        Style(noButton, No, vote == false);
        Style(yesButton, Yes, vote == true);
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

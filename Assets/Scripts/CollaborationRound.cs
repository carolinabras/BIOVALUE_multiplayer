using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game, collaboration round (GamePhase.Collaboration, after the action cards round):
// the board is covered by "Played actions", one column per player, 4 players per page (arrows and dots).
// Each played card shows its collaborators and, on other players' cards, "Collaborate · 1 token"
// (asks the owner, who accepts or declines in the usual pop-up; a decline gives the token back).
// The owner's cards show "Your action" or the pink "N requests". The GM sees each player's tokens used.
// "I'm done" and "End collaboration" are on the turn ribbon.
// Built by the editor menu "BioValue/Build Turn Ribbon and GM Panel".
public class CollaborationRound : MonoBehaviour
{
    [Serializable]
    public class Lane
    {
        public GameObject root;
        public Image avatar;
        public TMP_Text number;
        public GameObject doneRing;
        public TMP_Text name;
        public TMP_Text company;
        public GameObject usedChip;
        public TMP_Text usedText;
        public RectTransform cards;
    }

    public GameObject content;
    public TMP_Text hintText;
    public TMP_Text pageText;
    public Button prevButton, nextButton;
    public GameObject prevAlert, nextAlert;     // pink dot: a request waits on that side
    public RectTransform pageDots;
    public GameObject pageDotTemplate;
    public Lane[] lanes;
    public GameObject cardTemplate;
    public GameObject avatarTemplate;

    private const int PerPage = 4;
    private const string EndGameKey = "endGame";

    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color Amber = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color OpenPink = new Color32(0xB8, 0x15, 0x6A, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Blue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);
    private static readonly Color BlueSoft = new Color32(0xDC, 0xE8, 0xF7, 0xFF), BlueInk = new Color32(0x1D, 0x4F, 0x86, 0xFF);
    private static readonly Color GreySoft = new Color32(0xF1, 0xEE, 0xF4, 0xFF), GreyInk = new Color32(0x7A, 0x70, 0x90, 0xFF);
    private static readonly Color Disabled = new Color32(0xE4, 0xE1, 0xEC, 0xFF), DisabledInk = new Color32(0x9A, 0x92, 0xAC, 0xFF);

    private int page;
    private bool shown;
    private string signature = "";
    private float nextRefresh;
    private bool dirty = true;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<Image> dots = new List<Image>();

    private void Start()
    {
        if (cardTemplate) cardTemplate.SetActive(false);
        if (avatarTemplate) avatarTemplate.SetActive(false);
        if (pageDotTemplate) pageDotTemplate.SetActive(false);
        if (prevButton) prevButton.onClick.AddListener(() => GoTo(page - 1));
        if (nextButton) nextButton.onClick.AddListener(() => GoTo(page + 1));
        if (content) content.SetActive(false);
    }

    private void OnEnable() => TrySubscribe();

    private void OnDisable()
    {
        if (ColaborationManager.Instance) ColaborationManager.Instance.Changed -= MarkDirty;
        subscribed = false;
    }

    private bool subscribed;

    private void TrySubscribe()
    {
        if (subscribed || !ColaborationManager.Instance) return;
        ColaborationManager.Instance.Changed += MarkDirty;
        subscribed = true;
    }

    private void MarkDirty() => dirty = true;

    private void Update()
    {
        TrySubscribe();
        if (!dirty && Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        dirty = false;
        Refresh();
    }

    private void GoTo(int target)
    {
        int pages = Mathf.Max(1, Mathf.CeilToInt(Players().Count / (float)PerPage));
        page = Mathf.Clamp(target, 0, pages - 1);
        signature = "";
        Refresh();
    }

    private static List<Player> Players() =>
        PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient).OrderBy(p => p.ActorNumber).ToList();

    // This player has nothing left to ask for: no tokens, or every other player's card is already
    // asked for (pending, collaborating or declined). The ribbon's "I'm done" glows then.
    public static bool NothingLeftToCollaborate()
    {
        var collab = ColaborationManager.Instance;
        var me = PhotonNetwork.LocalPlayer;
        if (!collab || me == null || GameAuthority.IsGameMaster) return false;
        if (ColaborationManager.MyTokens() <= 0) return true;
        foreach (var p in Players())
        {
            if (p.IsLocal) continue;
            int count = CardsOf(p.ActorNumber).Count;
            for (int i = 0; i < count; i++)
                if (collab.MyState(p.ActorNumber, i) == ColaborationManager.RequestState.None) return false;
        }
        return true;
    }

    private static List<int> CardsOf(int actor)
    {
        var state = GameState.Instance;
        if (state == null) return new List<int>();
        // Keyed by the player's number (sorted position, GM = 0), like ActionDeck stores them.
        return state.playerActionCards.TryGetValue(TurnRibbon.PlayerNumberOf(actor), out var ids) ? ids : new List<int>();
    }

    // ── Refresh ──────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var state = GameState.Instance;
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        bool show = state != null && !ended && state.GetCurrentGamePhase() == GameState.GamePhase.Collaboration;
        if (content && content.activeSelf != show) content.SetActive(show);
        if (show && !shown) { page = 0; signature = ""; } // a new round starts on the first page
        if (!show && shown && ColaborationManager.Instance) ColaborationManager.Instance.EndRound(); // refund unanswered requests
        shown = show;
        if (!show) return;

        var players = Players();
        int pages = Mathf.Max(1, Mathf.CeilToInt(players.Count / (float)PerPage));
        page = Mathf.Clamp(page, 0, pages - 1);
        bool gm = GameAuthority.IsGameMaster;
        int me = PhotonNetwork.LocalPlayer.ActorNumber;
        var collab = ColaborationManager.Instance;
        int tokens = ColaborationManager.MyTokens();
        bool meDone = ColaborationManager.IsDone(PhotonNetwork.LocalPlayer);

        // Rebuild only when something visible changed.
        var sig = new StringBuilder();
        sig.Append(page).Append('|').Append(players.Count).Append('|').Append(tokens).Append(meDone).Append(gm);
        foreach (var p in players)
        {
            sig.Append('|').Append(p.ActorNumber).Append(ColaborationManager.IsDone(p)).Append(GameLog.GetPlayerName(p.ActorNumber));
            var ids = CardsOf(p.ActorNumber);
            for (int i = 0; i < ids.Count; i++)
            {
                sig.Append(',').Append(ids[i]);
                if (!collab) continue;
                sig.Append(':').Append(string.Join(".", collab.GetCollaborators(p.ActorNumber, i)))
                   .Append(collab.MyState(p.ActorNumber, i)).Append(collab.IncomingRequests(p.ActorNumber, i));
            }
        }
        if (sig.ToString() == signature) return;
        signature = sig.ToString();

        if (hintText) hintText.text = gm ? "Players collaborate on each other’s actions; you see the tokens each one used"
                                         : "Collaborating costs 1 token";
        int first = page * PerPage;
        if (pageText) pageText.text = players.Count == 0 ? "" : $"Players {first + 1}–{Mathf.Min(players.Count, first + PerPage)} of {players.Count}";
        SetArrow(prevButton, page > 0);
        SetArrow(nextButton, page < pages - 1);
        FillPageDots(pages);

        // Requests waiting on my cards, on another page.
        int myIndex = players.FindIndex(p => p.ActorNumber == me);
        bool waiting = false;
        if (collab && myIndex >= 0)
            for (int i = 0; i < CardsOf(me).Count; i++) waiting |= collab.IncomingRequests(me, i) > 0;
        int myPage = myIndex >= 0 ? myIndex / PerPage : page;
        if (prevAlert) prevAlert.SetActive(waiting && myPage < page);
        if (nextAlert) nextAlert.SetActive(waiting && myPage > page);

        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();

        for (int l = 0; l < lanes.Length; l++)
        {
            var lane = lanes[l];
            int pi = first + l;
            bool used = pi < players.Count;
            if (lane.root) lane.root.SetActive(used);
            if (!used) continue;
            var player = players[pi];
            FillLane(lane, player, gm, me, tokens, meDone, collab);
        }
    }

    private void FillLane(Lane lane, Player player, bool gm, int me, int tokens, bool meDone, ColaborationManager collab)
    {
        int actor = player.ActorNumber;
        int number = TurnRibbon.PlayerNumberOf(actor);
        bool own = actor == me && !gm;
        if (lane.avatar) lane.avatar.color = PlayerCapsuleHook.ColorFor(number);
        if (lane.number) lane.number.text = number.ToString();
        if (lane.doneRing) lane.doneRing.SetActive(ColaborationManager.IsDone(player));
        if (lane.name) lane.name.text = GameLog.GetPlayerName(actor) + (own ? " (you)" : "");
        if (lane.company)
            lane.company.text = player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c) ? c as string : "";
        if (lane.usedChip) lane.usedChip.SetActive(gm);
        if (gm && lane.usedText && collab)
            lane.usedText.text = $"{collab.GetCollaborationsDone(actor)} of {ColaborationManager.StartTokens} tokens";

        if (!lane.cards || !cardTemplate) return;
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var ids = CardsOf(actor);
        for (int i = 0; i < ids.Count; i++)
        {
            var card = Instantiate(cardTemplate, lane.cards);
            card.SetActive(true);
            spawned.Add(card);
            FillCard(card.transform, actor, i, ids[i], db, gm, own, tokens, meDone, collab);
        }
    }

    private void FillCard(Transform card, int owner, int index, int cardId, ActionCardsDatabase db,
                          bool gm, bool own, int tokens, bool meDone, ColaborationManager collab)
    {
        bool open = cardId >= ActionCardsDatabase.CustomCardIdStart;
        Color accent = open ? OpenPink : Amber;
        var data = db ? db.GetActionCardById(cardId) : null;

        var state = collab ? collab.MyState(owner, index) : ColaborationManager.RequestState.None;
        int incoming = collab && own ? collab.IncomingRequests(owner, index) : 0;

        var border = card.GetComponent<Image>();
        if (border) border.color = incoming > 0 ? Pink : !gm && state == ColaborationManager.RequestState.Collaborating ? Blue : accent;
        SetImage(card, "Face/Top/Hex", accent);
        SetText(card, "Face/Top/Hex/Number", InstrumentDetailsPanel.CardNumber(cardId));
        SetText(card, "Face/Top/Kind", open ? "OPEN ACTION" : "ACTION", accent);
        SetText(card, "Face/Title", data != null && !string.IsNullOrEmpty(data.cardName) ? data.cardName : "Action card");

        // Collaborators
        var avatars = card.Find("Face/Footer/Avatars");
        var list = collab ? collab.GetCollaborators(owner, index) : new List<int>();
        if (avatars && avatarTemplate)
        {
            foreach (int a in list)
            {
                var av = Instantiate(avatarTemplate, avatars);
                av.SetActive(true);
                int n = TurnRibbon.PlayerNumberOf(a);
                var img = av.GetComponent<Image>();
                if (img) img.color = PlayerCapsuleHook.ColorFor(n);
                SetText(av.transform, "Number", n.ToString());
            }
        }
        var noOne = card.Find("Face/Footer/NoOne");
        if (noOne) noOne.gameObject.SetActive(list.Count == 0);

        // Button or tag
        var buttonT = card.Find("Face/Footer/Collaborate");
        var tagT = card.Find("Face/Footer/Tag");
        bool canAct = !gm && !own && !meDone &&
                      (state == ColaborationManager.RequestState.None || state == ColaborationManager.RequestState.Pending);
        if (buttonT)
        {
            buttonT.gameObject.SetActive(canAct);
            if (canAct)
            {
                bool pending = state == ColaborationManager.RequestState.Pending;
                bool afford = tokens > 0;
                var button = buttonT.GetComponent<Button>();
                var img = buttonT.GetComponent<Image>();
                if (img) img.color = pending ? Lilac : afford ? Blue : Disabled;
                SetText(buttonT, "Label", pending ? $"Waiting for {GameLog.GetPlayerName(owner)}…" : "Collaborate · 1 token",
                        pending ? PurpleMid : afford ? Color.white : DisabledInk);
                if (button)
                {
                    button.interactable = !pending && afford;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => { if (collab && collab.RequestFromRound(owner, index)) MarkDirty(); });
                }
            }
        }
        if (tagT)
        {
            string tag = "";
            Color fill = Lilac, ink = Purple;
            if (own)
            {
                tag = incoming > 0 ? (incoming == 1 ? "1 request" : $"{incoming} requests") : "Your action";
                if (incoming > 0) { fill = Pink; ink = Color.white; }
            }
            else if (!gm && state == ColaborationManager.RequestState.Collaborating) { tag = "Collaborating"; fill = BlueSoft; ink = BlueInk; }
            else if (!gm && state == ColaborationManager.RequestState.Declined) { tag = "Declined"; fill = GreySoft; ink = GreyInk; }
            tagT.gameObject.SetActive(tag != "" && !canAct);
            var tagImg = tagT.GetComponent<Image>();
            if (tagImg) tagImg.color = fill;
            SetText(tagT, "Label", tag, ink);
        }
    }

    // ── Paging ───────────────────────────────────────────────────────────────

    private static void SetArrow(Button button, bool enabled)
    {
        if (!button) return;
        button.interactable = enabled;
        var img = button.GetComponent<Image>();
        if (img) img.color = enabled ? Color.white : new Color(1, 1, 1, 0.14f);
        var arrow = button.transform.Find("Arrow")?.GetComponent<Image>();
        if (arrow) arrow.color = enabled ? Purple : new Color(1, 1, 1, 0.35f);
    }

    private void FillPageDots(int pages)
    {
        if (!pageDots || !pageDotTemplate) return;
        while (dots.Count < pages)
        {
            var go = Instantiate(pageDotTemplate, pageDots);
            int target = dots.Count;
            var button = go.GetComponent<Button>();
            if (button) button.onClick.AddListener(() => GoTo(target));
            dots.Add(go.GetComponent<Image>());
        }
        for (int i = 0; i < dots.Count; i++)
        {
            var dot = dots[i];
            if (!dot) continue;
            dot.gameObject.SetActive(i < pages && pages > 1);
            dot.color = i == page ? Color.white : new Color(1, 1, 1, 0.4f);
            var size = dot.GetComponent<LayoutElement>();
            if (size) size.preferredWidth = size.minWidth = i == page ? 30f : 12f;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void SetText(Transform root, string path, string text, Color? color = null)
    {
        var t = root.Find(path);
        var tmp = t ? t.GetComponent<TMP_Text>() : null;
        if (!tmp) return;
        tmp.text = text;
        if (color.HasValue) tmp.color = color.Value;
    }

    private static void SetImage(Transform root, string path, Color color)
    {
        var t = root.Find(path);
        var img = t ? t.GetComponent<Image>() : null;
        if (img) img.color = color;
    }
}

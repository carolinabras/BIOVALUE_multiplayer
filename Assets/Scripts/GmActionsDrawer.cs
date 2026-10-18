using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// GM only, during the action cards round: "Everyone's actions", a side drawer with the same look as the
// players' action drawer. One section per player (avatar, name, company, "Played N cards" / "Still
// choosing") with the cards they played, read-only. Opened from "See everyone's actions" in the GM panel;
// players never see it. Updates while open when a player plays or edits cards.
// Built by "BioValue/Build Turn Ribbon and GM Panel".
public class GmActionsDrawer : MonoBehaviour
{
    public static GmActionsDrawer Instance { get; private set; }

    public RectTransform drawer;
    public float slideTime = 0.35f;
    public TMP_Text subtitle;
    public ScrollRect scroll;
    public RectTransform sections;
    public GameObject sectionTemplate;   // Header (Avatar/Number, Name, Company, Status) + Cards grid + Empty
    public GameObject actionCardPrefab;

    public bool IsOpen { get; private set; }

    private static readonly Color Played = new Color32(0x2F, 0x8A, 0x4A, 0xFF);
    private static readonly Color PlayedBg = new Color32(0xDF, 0xF3, 0xE4, 0xFF);
    private static readonly Color Waiting = new Color32(0x6B, 0x5A, 0x48, 0xFF);
    private static readonly Color WaitingBg = new Color32(0xF3, 0xE6, 0xD4, 0xFF);

    private float openX;
    private bool initialized, dirty;
    private readonly List<GameObject> built = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        Init();
        if (drawer) drawer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var state = GameState.Instance;
        if (state)
        {
            state.onPlayerActionCardsSet.RemoveListener(OnCardsChanged);
            state.onActionCardDescriptionChanged.RemoveListener(OnCardsChanged);
        }
    }

    private void Start()
    {
        var state = GameState.Instance;
        if (state)
        {
            state.onPlayerActionCardsSet.AddListener(OnCardsChanged);
            state.onActionCardDescriptionChanged.AddListener(OnCardsChanged);
        }
    }

    private void Init()
    {
        if (initialized || !drawer) return;
        initialized = true;
        openX = drawer.anchoredPosition.x;
        if (sectionTemplate) sectionTemplate.SetActive(false);
    }

    private float ClosedX => openX + drawer.rect.width + 80f;

    // Only for the GM, only in the action cards round.
    public static bool Allowed =>
        GameAuthority.IsGameMaster && GameState.Instance != null &&
        GameState.Instance.GetCurrentGamePhase() == GameState.GamePhase.ActionCardPlay;

    private void Update()
    {
        if (!IsOpen) return;
        if (!Allowed) { Hide(true); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { Hide(); return; }
        if (dirty) { dirty = false; Rebuild(); }
    }

    private void OnCardsChanged(int _) => dirty = true;

    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    public void Close() => Hide();

    public void Show()
    {
        Init();
        if (!drawer || !Allowed) return;
        IsOpen = true;
        drawer.gameObject.SetActive(true);
        Rebuild();
        if (scroll) scroll.verticalNormalizedPosition = 1f;
        Slide(ClosedX, openX, null);
    }

    public void Hide(bool instant = false)
    {
        Init();
        if (!drawer) return;
        IsOpen = false;
        if (instant || !drawer.gameObject.activeSelf)
        {
            LeanTween.cancel(drawer.gameObject);
            SetX(ClosedX);
            drawer.gameObject.SetActive(false);
            return;
        }
        Slide(drawer.anchoredPosition.x, ClosedX, () => drawer.gameObject.SetActive(false));
    }

    private void Slide(float from, float to, System.Action done)
    {
        LeanTween.cancel(drawer.gameObject);
        SetX(from);
        LeanTween.value(drawer.gameObject, from, to, slideTime).setEaseOutCubic().setIgnoreTimeScale(true)
            .setOnUpdate(SetX)
            .setOnComplete(() => done?.Invoke());
    }

    private void SetX(float x)
    {
        var p = drawer.anchoredPosition;
        p.x = x;
        drawer.anchoredPosition = p;
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Rebuild()
    {
        foreach (var go in built)
            if (go) { go.SetActive(false); Destroy(go); }
        built.Clear();
        if (!sections || !sectionTemplate) return;

        var state = GameState.Instance;
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var players = PhotonNetwork.PlayerList.Where(p => !p.IsMasterClient).OrderBy(p => p.ActorNumber).ToList();
        int playedCount = 0;

        foreach (var player in players)
        {
            int number = TurnRibbon.PlayerNumberOf(player.ActorNumber); // also the playerActionCards key
            var ids = state != null && state.playerActionCards.TryGetValue(number, out var list) ? list : null;
            bool played = ids != null;
            if (played) playedCount++;

            var section = Instantiate(sectionTemplate, sections);
            section.name = "Player " + number;
            section.SetActive(true);
            built.Add(section);

            var header = section.transform.Find("Header");
            var avatar = header ? header.Find("Avatar")?.GetComponent<Image>() : null;
            if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(number);
            SetText(header, "Avatar/Number", number.ToString());
            SetText(header, "Name", GameLog.GetPlayerName(player.ActorNumber));
            SetText(header, "Company", player.CustomProperties.TryGetValue(BiovalueStatics.PlayerCompanyKey, out var c) ? c as string : "");
            var status = header ? header.Find("Status") : null;
            if (status)
            {
                int n = played ? ids.Count : 0;
                var bg = status.GetComponent<Image>();
                if (bg) bg.color = played ? PlayedBg : WaitingBg;
                var label = status.GetComponentInChildren<TMP_Text>(true);
                if (label)
                {
                    label.text = !played ? "Still choosing" : n == 1 ? "Played 1 card" : $"Played {n} cards";
                    label.color = played ? Played : Waiting;
                }
            }

            var cards = section.transform.Find("Cards");
            var empty = section.transform.Find("Empty");
            bool any = played && ids.Count > 0 && db != null && actionCardPrefab;
            if (cards) cards.gameObject.SetActive(any);
            if (empty)
            {
                empty.gameObject.SetActive(!any);
                var t = empty.GetComponent<TMP_Text>();
                if (t) t.text = played ? "Played no cards." : "No cards played yet.";
            }
            if (!any || !cards) continue;

            for (int i = 0; i < ids.Count; i++)
            {
                var data = db.GetActionCardById(ids[i]);
                if (data == null) continue;
                var copy = new ActionCard
                {
                    id = data.id,
                    cardName = data.cardName,
                    descriptionGeneral = data.descriptionGeneral,
                    descriptionHow = data.descriptionHow,
                    type = data.type,
                    icon = data.icon
                };
                var card = Instantiate(actionCardPrefab, cards);
                var hook = card.GetComponent<ActionCardsHook>();
                if (hook)
                {
                    hook.isPlayedreal = true;
                    hook.ownerPlayerId = number;
                    hook.SetActionCard(copy);
                }
                card.GetComponent<CollaborateButton>()?.Setup(player.ActorNumber, i); // hides its old button
            }
        }

        if (subtitle)
            subtitle.text = players.Count == 0 ? "No players yet"
                : $"{playedCount} of {players.Count} players have played their cards";
        LayoutRebuilder.ForceRebuildLayoutImmediate(sections);
    }

    private static void SetText(Transform parent, string path, string text)
    {
        var t = parent ? parent.Find(path)?.GetComponent<TMP_Text>() : null;
        if (t) t.text = text ?? "";
    }
}

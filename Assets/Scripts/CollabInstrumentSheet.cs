using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Collaboration round: the sheet beside the lanes that opens when you click one of a card's instruments.
// Header in the instrument's category colour (number, name, categories); who placed it and the vote;
// its full description; the other played cards that use it. Arrows step through the card's instruments;
// "Show on the board" hides the lanes and makes those pieces glow (CollaborationRound.PeekBoard).
// Built by "BioValue/Build Turn Ribbon and GM Panel".
public class CollabInstrumentSheet : MonoBehaviour
{
    public GameObject root;
    public RectTransform header;
    public Image headerFill;
    public TMP_Text eyebrow;
    public TMP_Text bigNumber;
    public TMP_Text nameText;
    public RectTransform categories;
    public GameObject categoryTemplate;    // white pill > Label
    public GameObject ownerRow;
    public Image ownerAvatar;
    public TMP_Text ownerNumber;
    public TMP_Text ownerText;
    public GameObject vote;
    public TMP_Text voteText;
    public TMP_Text description;
    public ScrollRect scroll;
    public RectTransform alsoContainer;
    public GameObject alsoTemplate;        // Avatar (Image) > Number, Label
    public GameObject noAlso;
    public Button closeButton, prevButton, nextButton, showOnBoardButton;

    public bool IsOpen => root && root.activeSelf;
    public int[] Links => links;

    // Set by CollaborationRound: show these pieces on the board.
    public Action<int[]> onShowOnBoard;

    private const float FooterH = 78f;
    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);

    private int owner = -1, cardId = -1, at;
    private int[] links = new int[0];
    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        if (root) root.SetActive(false);
        if (categoryTemplate) categoryTemplate.SetActive(false);
        if (alsoTemplate) alsoTemplate.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (prevButton) prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton) nextButton.onClick.AddListener(() => Step(1));
        if (showOnBoardButton) showOnBoardButton.onClick.AddListener(() => onShowOnBoard?.Invoke(links));
    }

    public bool IsShowing(int ownerActor, int card, int index) =>
        IsOpen && owner == ownerActor && cardId == card && at == index;

    public void Open(int ownerActor, int card, int[] cardLinks, int index)
    {
        if (!root || cardLinks == null || cardLinks.Length == 0) return;
        bool wasOpen = IsOpen;
        owner = ownerActor;
        cardId = card;
        links = cardLinks;
        at = Mathf.Clamp(index, 0, links.Length - 1);
        root.SetActive(true);
        Fill();

        if (!wasOpen)
        {
            var rt = (RectTransform)root.transform;
            LeanTween.cancel(root);
            var group = root.GetComponent<CanvasGroup>();
            if (group)
            {
                group.alpha = 0f;
                LeanTween.alphaCanvas(group, 1f, 0.18f).setIgnoreTimeScale(true);
            }
            var start = rt.anchoredPosition;
            rt.anchoredPosition = start + new Vector2(40, 0);
            LeanTween.move(rt, start, 0.22f).setEaseOutCubic().setIgnoreTimeScale(true);
        }
    }

    public void Close()
    {
        if (root) root.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Step(int delta)
    {
        if (links.Length == 0) return;
        at = ((at + delta) % links.Length + links.Length) % links.Length;
        Fill();
    }

    private void Fill()
    {
        int id = links[at];
        var hook = InstrumentLookup.Find(id);
        var inst = hook ? hook.instrument : null;
        var (a, b) = InstrumentLookup.Styles(hook);
        Color ring = a != null ? a.ring : Purple;

        if (headerFill) headerFill.color = ring;
        if (eyebrow) eyebrow.text = links.Length > 1 ? $"INSTRUMENT {at + 1} OF {links.Length} ON THIS CARD" : "INSTRUMENT ON THIS CARD";
        if (bigNumber) { bigNumber.text = id.ToString(); bigNumber.color = ring; }
        if (nameText) nameText.text = inst != null ? inst.name : $"Instrument {id}";
        if (description)
            description.text = inst == null ? "" : !string.IsNullOrWhiteSpace(inst.description) ? inst.description : inst.generalDescription;

        bool placed = inst != null && inst.isPlaced;
        if (ownerRow) ownerRow.SetActive(placed);
        if (placed)
        {
            if (ownerAvatar) ownerAvatar.color = PlayerCapsuleHook.ColorFor(inst.ownerId);
            if (ownerNumber) ownerNumber.text = inst.ownerId.ToString();
            if (ownerText)
                ownerText.text = $"<b>Placed by <noparse>{GameLog.GetPlayerNameByIndex(inst.ownerId)}</noparse></b>\n<color=#6B6584>Instruments round</color>";
            string v = InstrumentLookup.VoteText(hook);
            if (vote) vote.SetActive(!string.IsNullOrEmpty(v));
            if (voteText) voteText.text = v;
        }

        bool many = links.Length > 1;
        if (prevButton) prevButton.interactable = many;
        if (nextButton) nextButton.interactable = many;

        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        AddCategory(a);
        AddCategory(b);

        // The other played cards that use it.
        int ownerIndex = TurnRibbon.PlayerNumberOf(owner);
        var others = GameState.Instance != null
            ? GameState.Instance.GetActionCardsLinking(id).Where(c => !(c.playerId == ownerIndex && c.cardId == cardId)).ToList()
            : new List<(int playerId, int cardId)>();
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        foreach (var (player, card) in others)
        {
            if (!alsoTemplate || !alsoContainer) break;
            var row = Instantiate(alsoTemplate, alsoContainer);
            row.SetActive(true);
            spawned.Add(row);
            var avatar = row.transform.Find("Avatar")?.GetComponent<Image>();
            if (avatar) avatar.color = PlayerCapsuleHook.ColorFor(player);
            var num = row.transform.Find("Avatar/Number")?.GetComponent<TMP_Text>();
            if (num) num.text = player.ToString();
            var label = row.transform.Find("Label")?.GetComponent<TMP_Text>();
            var data = db ? db.GetActionCardById(card) : null;
            if (label) label.text = $"{InstrumentDetailsPanel.CardNumber(card)}  {(data != null ? data.cardName : "Action card")}";
        }
        if (noAlso) noAlso.SetActive(others.Count == 0);

        FitBody();
        if (scroll) scroll.verticalNormalizedPosition = 1f;
    }

    private void AddCategory(InstrumentCategoryStyle style)
    {
        if (style == null || !categoryTemplate || !categories) return;
        var chip = Instantiate(categoryTemplate, categories);
        chip.SetActive(true);
        var label = chip.GetComponentInChildren<TMP_Text>(true);
        if (label) label.text = style.displayName;
        spawned.Add(chip);
    }

    // The body starts under the header, whose height depends on the name.
    private void FitBody()
    {
        if (!header || !scroll) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(header);
        var body = (RectTransform)scroll.transform;
        body.offsetMax = new Vector2(body.offsetMax.x, -header.rect.height);
        body.offsetMin = new Vector2(body.offsetMin.x, FooterH);
        if (scroll.content) LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
    }
}

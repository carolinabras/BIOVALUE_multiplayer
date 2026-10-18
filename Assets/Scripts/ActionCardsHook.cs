using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One action card. Two modes:
//  - In the player's hand (ActionCardsSpawner): select it, flip it to write the "how",
//    link it to instruments in play, and (open cards) write the action itself.
//  - Played (PlayedCardsSpawner / CardSummarySpawner, isPlayedreal = true): read-only.
// The visuals are built by the editor menu "BioValue/Build Action Card".
public class ActionCardsHook : MonoBehaviourPun
{
    [HideInInspector] public ActionCard actionCard;

    [SerializeField] private TMPro.TMP_Text description;          // front, predefined cards

    [SerializeField] private TMP_InputField descriptionCustom;    // front, open cards

    [SerializeField] private TMP_InputField descriptionHow;       // back

    [SerializeField] private TMP_Text descriptionCustomReadOnly;  // played: the action
    [SerializeField] private TMP_Text descriptionHowReadOnly;     // played: the "how"

    [Header("Card design")]
    [SerializeField] private Image frame;
    [SerializeField] private Image paper;
    [SerializeField] private Image glow;
    [SerializeField] private Image badge;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text kindText;
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;
    [SerializeField] private GameObject played;
    [SerializeField] private TMP_Text howTitle;
    [SerializeField] private TMP_Text linksLabel;
    [SerializeField] private RectTransform chipContainer;
    [SerializeField] private GameObject chipTemplate;
    [SerializeField] private TMP_Text noLinksText;
    [SerializeField] private Button flipButton;
    [SerializeField] private TMP_Text flipLabel;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image selectFill;
    [SerializeField] private TMP_Text selectLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    public bool isPlayedreal = false;
    public int ownerPlayerId = -1;

    // Raised whenever selection, text or links change (the spawner updates the deck status).
    public event Action<ActionCardsHook> Changed;
    // Set by the spawner: may this card be selected now (max cards per round)?
    public Func<ActionCardsHook, bool> canSelect;

    private bool flipped;
    private bool limitReached;
    private readonly List<GameObject> chips = new List<GameObject>();

    private static readonly Color Orange = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color Pink = new Color32(0xB8, 0x15, 0x6A, 0xFF);
    private static readonly Color PaperPredefined = new Color32(0xFB, 0xF5, 0xEC, 0xFF); // cream, stands out on the light orange drawer
    private static readonly Color PaperOpen = new Color32(0xFF, 0xF6, 0xFA, 0xFF);
    private static readonly Color PaperBack = new Color32(0xF7, 0xF0, 0xE6, 0xFF);
    private static readonly Color Ink = new Color32(0x1E, 0x2B, 0x27, 0xFF);
    private static readonly Color Muted = new Color32(0x4A, 0x5A, 0x55, 0xFF);
    private static readonly Color SelectedPink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color DisabledFill = new Color32(0xE3, 0xE8, 0xE4, 0xFF);
    private static readonly Color ChipOn = new Color32(0x46, 0x32, 0x7F, 0xFF); // logo purple
    private static readonly Color ChipOff = new Color32(0xD5, 0xDD, 0xD8, 0xFF);

    public bool IsOpenCard => actionCard != null && actionCard.type == ActionCardType.Custom;

    // "03" for predefined cards, "O2" for open ones.
    public string Number => actionCard == null ? ""
        : IsOpenCard ? "O" + (actionCard.id - ActionCardsDatabase.CustomCardIdStart + 1)
        : actionCard.id.ToString("00");

    public List<int> LinkedInstruments => actionCard != null ? actionCard.linkedInstruments : new List<int>();

    private void Awake()
    {
        if (descriptionCustom)
        {
            descriptionCustom.onValueChanged.AddListener(OnCustomValueChanged);
            descriptionCustom.onEndEdit.AddListener(OnCustomEndEdit);
        }
        if (descriptionHow)
        {
            descriptionHow.onValueChanged.AddListener(OnHowValueChanged);
            descriptionHow.onEndEdit.AddListener(OnHowEndEdit);
        }
        if (flipButton) flipButton.onClick.AddListener(Flip);
        if (selectButton) selectButton.onClick.AddListener(ToggleSelected);
        if (chipTemplate) chipTemplate.SetActive(false);
    }

    public void Start()
    {
        if (GameState.Instance != null)
            GameState.Instance.onActionCardDescriptionChanged.AddListener(OnDescriptionChanged);
        else
            Debug.LogWarning("GameState.Instance is null in Start!");
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
            GameState.Instance.onActionCardDescriptionChanged.RemoveListener(OnDescriptionChanged);
    }

    // Played cards: text or links arrived over the network after the card was shown.
    private void OnDescriptionChanged(int cardId)
    {
        if (actionCard == null || actionCard.id != cardId) return;
        if (!isPlayedreal) return;
        ApplyPlayedTexts();
        RebuildChips();
    }

    // ── Input callbacks ──────────────────────────────────────────────────────

    public void OnCustomValueChanged(string text)
    {
        DescriptionCustom = text;
        if (howTitle) howTitle.text = DisplayText;
        Changed?.Invoke(this);
    }

    public void OnCustomEndEdit(string text)
    {
        DescriptionCustom = text;
        if (actionCard != null && GameState.Instance != null)
            GameState.Instance.SetActionCardDescription(GameState.Instance.localPlayerIndex, actionCard.id, text);
    }

    public void OnHowValueChanged(string text)
    {
        DescriptionHow = text;
        RefreshFlipLabel();
        Changed?.Invoke(this);
    }

    public void OnHowEndEdit(string text)
    {
        DescriptionHow = text;
        if (actionCard != null && GameState.Instance != null)
            GameState.Instance.SetActionCardDescriptionHow(GameState.Instance.localPlayerIndex, actionCard.id, text);
    }

    // ── Properties kept for the rest of the game ─────────────────────────────

    public string Description
    {
        get => description != null ? description.text : string.Empty;
        set
        {
            if (actionCard != null) actionCard.descriptionGeneral = value;
            if (description != null) description.text = value;
        }
    }

    public string DescriptionHow
    {
        get => descriptionHow != null ? descriptionHow.text : string.Empty;
        set
        {
            if (actionCard != null) actionCard.descriptionHow = value;
            if (descriptionHow != null && descriptionHow.text != value)
                descriptionHow.SetTextWithoutNotify(value);
        }
    }

    public string DescriptionCustom
    {
        get => descriptionCustom != null ? descriptionCustom.text : string.Empty;
        set
        {
            if (actionCard != null) actionCard.descriptionGeneral = value;
            if (descriptionCustom != null && descriptionCustom.text != value)
                descriptionCustom.SetTextWithoutNotify(value);
        }
    }

    public int ID
    {
        get => actionCard != null ? actionCard.id : -1;
        set { if (actionCard != null) actionCard.id = value; }
    }

    public bool IsSelected
    {
        get => actionCard != null && actionCard.isSelected;
        set { if (actionCard != null) actionCard.isSelected = value; }
    }

    public bool isPlayed
    {
        get => actionCard != null && actionCard.isPlayed;
        set { if (actionCard != null) actionCard.isPlayed = value; }
    }

    // The action's text as shown to players.
    public string DisplayText
    {
        get
        {
            if (actionCard == null) return "";
            if (!string.IsNullOrWhiteSpace(actionCard.descriptionGeneral)) return actionCard.descriptionGeneral.Trim();
            return IsOpenCard ? "Open action" : actionCard.cardName;
        }
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    public void OnSelectedClicked() => ToggleSelected();

    public void ToggleSelected()
    {
        if (actionCard == null || isPlayedreal) return;
        if (!IsSelected && canSelect != null && !canSelect(this)) return;

        IsSelected = !IsSelected;
        Refresh();
        Changed?.Invoke(this);
    }

    [Header("Flip animation")]
    [SerializeField] private float flipDuration = 0.4f;
    private bool flipping;

    // The card turns edge-on, swaps sides at 90°, then turns back to face the player.
    public void Flip()
    {
        if (isPlayedreal || flipping) return;

        var card = frame ? frame.rectTransform.parent as RectTransform : null;
        if (!card || flipDuration <= 0f)
        {
            SwapSide();
            return;
        }

        flipping = true;
        float half = flipDuration * 0.5f;
        LeanTween.cancel(card.gameObject);
        LeanTween.value(card.gameObject, 0f, 90f, half).setEaseInQuad()
            .setOnUpdate(a => SetTurn(card, a))
            .setOnComplete(() =>
            {
                SwapSide();
                LeanTween.value(card.gameObject, -90f, 0f, half).setEaseOutQuad()
                    .setOnUpdate(a => SetTurn(card, a))
                    .setOnComplete(() =>
                    {
                        SetTurn(card, 0f);
                        flipping = false;
                        // Back side: put the cursor in the "how" field right away.
                        if (flipped && descriptionHow) descriptionHow.ActivateInputField();
                    });
            });
    }

    private void SwapSide()
    {
        flipped = !flipped;
        Refresh();
    }

    private static void SetTurn(RectTransform card, float angle) =>
        card.localRotation = Quaternion.Euler(0f, angle, 0f);

    // Closing the deck mid-flip: finish instantly so the card isn't left edge-on.
    private void OnDisable()
    {
        if (!flipping) return;
        var card = frame ? frame.rectTransform.parent as RectTransform : null;
        if (card)
        {
            LeanTween.cancel(card.gameObject);
            SetTurn(card, 0f);
        }
        flipping = false;
    }

    // Greys the card out when the player already picked the maximum number of cards.
    public void SetLimitReached(bool value)
    {
        if (limitReached == value) return;
        limitReached = value;
        Refresh();
    }

    // Can this card be played? Linking instruments is optional (callers pass requireLinks = false).
    public bool IsReady(bool requireLinks, out string reason)
    {
        reason = "";
        if (actionCard == null) { reason = "Card missing."; return false; }
        if (IsOpenCard && string.IsNullOrWhiteSpace(actionCard.descriptionGeneral))
            reason = "Needs a description.";
        else if (requireLinks && LinkedInstruments.Count == 0)
            reason = "Needs a linked instrument.";
        else if (string.IsNullOrWhiteSpace(actionCard.descriptionHow))
            reason = "Needs a \"how\" (flip the card).";
        return reason.Length == 0;
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public void SetActionCard(ActionCard card)
    {
        this.actionCard = card;
        if (card == null) return;
        if (card.linkedInstruments == null) card.linkedInstruments = new List<int>();

        string savedDesc = GameState.Instance?.GetActionCardDescription(ownerPlayerId, card.id) ?? "";
        string savedHow = GameState.Instance?.GetActionCardDescriptionHow(ownerPlayerId, card.id) ?? "";

        if (isPlayedreal)
        {
            if (!string.IsNullOrEmpty(savedDesc)) card.descriptionGeneral = savedDesc;
            if (!string.IsNullOrEmpty(savedHow)) card.descriptionHow = savedHow;
            ApplyPlayedTexts();
        }
        else
        {
            if (IsOpenCard)
            {
                if (!string.IsNullOrEmpty(savedDesc)) card.descriptionGeneral = savedDesc;
                if (descriptionCustom) descriptionCustom.SetTextWithoutNotify(card.descriptionGeneral ?? "");
            }
            else if (description)
            {
                description.text = card.descriptionGeneral;
            }

            if (!string.IsNullOrEmpty(savedHow)) card.descriptionHow = savedHow;
            if (descriptionHow) descriptionHow.SetTextWithoutNotify(card.descriptionHow ?? "");
        }

        Color accent = IsOpenCard ? Pink : Orange;
        if (frame) frame.color = accent;
        if (badge) badge.color = accent;
        if (numberText) numberText.text = Number;
        if (kindText)
        {
            kindText.text = IsOpenCard ? "Open action" : "Action";
            kindText.color = accent;
        }
        if (howTitle) howTitle.text = DisplayText;

        flipped = false;
        Refresh();
    }

    private void ApplyPlayedTexts()
    {
        if (actionCard == null) return;
        string savedDesc = GameState.Instance?.GetActionCardDescription(ownerPlayerId, actionCard.id) ?? "";
        string savedHow = GameState.Instance?.GetActionCardDescriptionHow(ownerPlayerId, actionCard.id) ?? "";
        if (!string.IsNullOrEmpty(savedDesc)) actionCard.descriptionGeneral = savedDesc;
        if (!string.IsNullOrEmpty(savedHow)) actionCard.descriptionHow = savedHow;

        int[] links = GameState.Instance?.GetActionCardLinks(ownerPlayerId, actionCard.id);
        if (links != null && links.Length > 0) actionCard.linkedInstruments = links.ToList();

        if (descriptionCustomReadOnly) descriptionCustomReadOnly.text = DisplayText;
        if (descriptionHowReadOnly)
            descriptionHowReadOnly.text = string.IsNullOrWhiteSpace(actionCard.descriptionHow)
                ? "<i>No \"how\" written.</i>" : actionCard.descriptionHow.Trim();
    }

    // Updates everything that depends on the card's state.
    public void Refresh()
    {
        if (actionCard == null) return;

        bool sel = IsSelected && !isPlayedreal;

        if (played) played.SetActive(isPlayedreal);
        if (front) front.SetActive(!isPlayedreal && !flipped);
        if (back) back.SetActive(!isPlayedreal && flipped);
        if (description) description.gameObject.SetActive(!IsOpenCard);
        if (descriptionCustom) descriptionCustom.gameObject.SetActive(IsOpenCard);

        if (paper) paper.color = isPlayedreal ? PaperPredefined : flipped ? PaperBack : IsOpenCard ? PaperOpen : PaperPredefined;
        if (glow) glow.gameObject.SetActive(sel);
        if (canvasGroup) canvasGroup.alpha = limitReached && !sel && !isPlayedreal ? 0.55f : 1f;

        if (flipButton) flipButton.gameObject.SetActive(!isPlayedreal);
        RefreshFlipLabel();

        if (selectButton)
        {
            selectButton.gameObject.SetActive(!isPlayedreal);
            bool blocked = limitReached && !sel;
            selectButton.interactable = !blocked;
            if (selectFill) selectFill.color = sel ? SelectedPink : blocked ? DisabledFill : Orange;
            if (selectLabel)
            {
                selectLabel.text = sel ? "Selected" : blocked ? "Limit reached" : "Select";
                selectLabel.color = sel ? new Color32(0x1E, 0x0A, 0x14, 0xFF) : blocked ? Muted : Color.white;
            }
        }

        RebuildChips();
    }

    private void RefreshFlipLabel()
    {
        if (!flipLabel || actionCard == null) return;
        flipLabel.text = flipped ? "Front"
            : string.IsNullOrWhiteSpace(actionCard.descriptionHow) ? "Write how" : "Edit how";
    }

    // In the hand: every instrument in play, tap to link (selected or not).
    // Played cards: only the linked instruments.
    private void RebuildChips()
    {
        if (!chipContainer || !chipTemplate || actionCard == null) return;

        foreach (var c in chips)
        {
            if (!c) continue;
            c.SetActive(false); // leave the layout now, Destroy is end-of-frame
            Destroy(c);
        }
        chips.Clear();

        bool picker = !isPlayedreal;
        var links = LinkedInstruments;
        List<int> shown = picker ? InstrumentsInPlay.Ids() : links.OrderBy(n => n).ToList();
        // Keep links to instruments that left the board visible so they can be removed.
        if (picker) shown = shown.Union(links).OrderBy(n => n).ToList();

        foreach (int n in shown)
        {
            int number = n;
            bool on = links.Contains(number);
            var chip = Instantiate(chipTemplate, chipContainer);
            chip.name = "Chip " + number;
            chip.SetActive(true);

            var img = chip.GetComponent<Image>();
            if (img) img.color = on ? ChipOn : ChipOff;
            var label = chip.GetComponentInChildren<TMP_Text>(true);
            if (label)
            {
                label.text = number.ToString();
                label.color = on ? Color.white : Ink;
            }
            var button = chip.GetComponent<Button>();
            if (button)
            {
                button.interactable = picker;
                button.onClick.AddListener(() => ToggleLink(number));
            }
            chips.Add(chip);
        }

        if (linksLabel) linksLabel.text = picker ? "Link to instruments in play" : "Linked instruments";
        if (noLinksText)
        {
            noLinksText.gameObject.SetActive(shown.Count == 0);
            noLinksText.text = isPlayedreal ? "No instrument linked"
                : "No instruments on the board yet";
        }
    }

    private void ToggleLink(int number)
    {
        if (actionCard == null || isPlayedreal) return;
        if (!actionCard.linkedInstruments.Remove(number))
            actionCard.linkedInstruments.Add(number);
        RebuildChips();
        Changed?.Invoke(this);
    }

    // ── Network ──────────────────────────────────────────────────────────────

    public void SetActionCardInNetwork(ActionCard card)
    {
        if (photonView)
        {
            photonView.RPC(nameof(RPC_SetActionCard), RpcTarget.All, card.id, card.cardName, card.descriptionGeneral,
                card.descriptionHow, card.type);
        }
        else
        {
            Debug.LogWarning("PhotonView is not assigned, setting action card locally.");
            SetActionCard(card);
        }
    }

    [PunRPC]
    public void RPC_SetActionCard(int id, string cardName, string descriptionGeneral, string descriptionHowCard,
        ActionCardType type)
    {
        ActionCard card = new ActionCard
        {
            id = id,
            cardName = cardName,
            descriptionGeneral = descriptionGeneral,
            descriptionHow = descriptionHowCard,
            type = type
        };
        SetActionCard(card);
    }
}

// Numbers of the instruments currently placed on the board (as seen by this client).
public static class InstrumentsInPlay
{
    public static List<int> Ids()
    {
        return UnityEngine.Object.FindObjectsByType<InstrumentHook>(FindObjectsSortMode.None)
            .Where(h => h.instrument != null && h.instrument.isPlaced && h.instrument.id > 0)
            .Select(h => h.instrument.id)
            .Distinct()
            .OrderBy(n => n)
            .ToList();
    }
}

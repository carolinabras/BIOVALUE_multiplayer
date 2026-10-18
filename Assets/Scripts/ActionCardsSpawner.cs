using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionCardsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject actionCardPrefab;
    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    [SerializeField] private GameObject parentOfInstruments;

    [SerializeField] private Transform panelParent;
    [SerializeField] private Transform newpanelParent;

    [HideInInspector] public List<ActionCardsHook> injectionStepHooks = new List<ActionCardsHook>();

    private void Awake()
    {
        actionCardsDatabase = ActionCardsDatabaseSession.Instance.SessionDb;
    }

    public List<ActionCardsHook> spawnedHooks = new List<ActionCardsHook>();

    [Header("Rules")]
    [Tooltip("Max action cards a player can play per round. 0 = no limit (the GM will set this in the Lobby later).")]
    [SerializeField] private int maxCardsPerRound = 0;
    public int MaxCardsPerRound => maxCardsPerRound;
    public bool HasLimit => maxCardsPerRound > 0;

    [Header("Empty (player-written) cards")]
    [Tooltip("How many empty cards the player starts with.")]
    [SerializeField] private int startingEmptyCards = 1;
    [Tooltip("Max empty cards a player can have. 0 = no limit.")]
    [SerializeField] private int maxEmptyCards = 5;
    [Tooltip("Button that adds one more empty card. If empty, the 'Add open card' tile from Resources is used.")]
    [SerializeField] private Button addEmptyCardButton;
    private TMP_Text addEmptyCardCountText;

    // Kept across re-spawns so cards the player already added (and wrote on) come back.
    private int emptyCardCount = 0;

    // Raised when a card is selected/unselected or its text/links change.
    public event Action SelectionChanged;

    private void OnEnable()
    {
        if (addEmptyCardButton) addEmptyCardButton.onClick.AddListener(AddEmptyCard);
    }

    private void OnDisable()
    {
        if (addEmptyCardButton) addEmptyCardButton.onClick.RemoveListener(AddEmptyCard);
        // Out of the actions round: stop the glow on the board.
        InstrumentLinkHighlights.Clear();
    }

    // Builds the hand the first time only, so closing and reopening the deck keeps
    // the selection, linked instruments and text.
    public void EnsureSpawned()
    {
        spawnedHooks.RemoveAll(h => !h);
        if (spawnedHooks.Count == 0) SpawnActionCards();
    }

    // ── Filter (All / Action / Open tabs) ────────────────────────────────────

    public enum DeckFilter { All = 0, Predefined = 1, Open = 2 }
    private DeckFilter filter = DeckFilter.All;

    public void SetFilter(DeckFilter value)
    {
        filter = value;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        foreach (var h in spawnedHooks)
        {
            if (!h) continue;
            bool show = filter == DeckFilter.All || (filter == DeckFilter.Open) == h.IsOpenCard;
            h.gameObject.SetActive(show);
        }
        RefreshAddButton();
    }

    public void SpawnActionCards()
    {
        spawnedHooks.Clear();
        ClearCards();
        EnsureAddButton();

        // Pre-written cards come from the database; the old "Custom" entries there are ignored.
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            if (actionCard == null || actionCard.type == ActionCardType.Custom) continue;

            ActionCard cardCopy = new ActionCard
            {
                id = actionCard.id,
                cardName = actionCard.cardName,
                descriptionGeneral = actionCard.descriptionGeneral,
                descriptionHow = actionCard.descriptionHow,
                type = actionCard.type,
                icon = actionCard.icon
            };
            SpawnCard(cardCopy);
        }

        emptyCardCount = Mathf.Max(emptyCardCount, startingEmptyCards);
        for (int i = 0; i < emptyCardCount; i++)
            SpawnCard(actionCardsDatabase.CreateCustomCard(ActionCardsDatabase.CustomCardIdStart + i));

        ApplyFilter();
        UpdateLimits();
    }

    // Hook this to the "+" button (done automatically if addEmptyCardButton is assigned).
    public void AddEmptyCard()
    {
        if (maxEmptyCards > 0 && emptyCardCount >= maxEmptyCards) return;

        SpawnCard(actionCardsDatabase.CreateCustomCard(ActionCardsDatabase.CustomCardIdStart + emptyCardCount));
        emptyCardCount++;
        RefreshAddButton();
        UpdateLimits();
    }

    private void SpawnCard(ActionCard card)
    {
        GameObject cardObject = Instantiate(actionCardPrefab, panelParent);
        ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
        if (hook != null)
        {
            hook.ownerPlayerId = GameState.Instance.localPlayerIndex;
            hook.canSelect = _ => !HasLimit || SelectedCount < maxCardsPerRound;
            hook.Changed += OnCardChanged;
            hook.SetActionCard(card);
            spawnedHooks.Add(hook);
        }
    }

    // ── Selection rules ──────────────────────────────────────────────────────

    public IEnumerable<ActionCardsHook> SelectedHooks => spawnedHooks.Where(h => h && h.IsSelected);
    public int SelectedCount => SelectedHooks.Count();

    private void OnCardChanged(ActionCardsHook hook)
    {
        UpdateLimits();
    }

    private void UpdateLimits()
    {
        bool full = HasLimit && SelectedCount >= maxCardsPerRound;
        foreach (var h in spawnedHooks)
            if (h) h.SetLimitReached(full);
        // Links can be made before selecting, so every card in the hand counts.
        InstrumentLinkHighlights.Set(spawnedHooks.Where(h => h).SelectMany(h => h.LinkedInstruments));
        SelectionChanged?.Invoke();
    }

    // The deck can be played: 1 to max cards, each one ready (text, how, linked instrument).
    public bool CanPlay(out string reason)
    {
        int count = SelectedCount;
        if (count == 0) { reason = "Select at least one action."; return false; }

        // Linking an instrument is optional.
        foreach (var h in SelectedHooks)
            if (!h.IsReady(false, out reason)) return false;

        reason = "";
        return true;
    }

    // Instruments may have been placed since the cards were built.
    public void RefreshCards()
    {
        foreach (var h in spawnedHooks)
            if (h) h.Refresh();
        SelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        foreach (var h in spawnedHooks)
        {
            if (!h || !h.IsSelected) continue;
            h.IsSelected = false;
            h.Refresh();
        }
        UpdateLimits();
    }

    // ── Layout helpers ───────────────────────────────────────────────────────

    // Removes the spawned cards but keeps the add button if it lives inside the grid.
    private void ClearCards()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in panelParent)
        {
            if (addEmptyCardButton && child == addEmptyCardButton.transform) continue;
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
        {
            go.transform.SetParent(null); // leave the layout right away, Destroy is end-of-frame
            Destroy(go);
        }
    }

    // No button assigned in the scene: use the "Add open card" tile (built by BioValue/Build Action Card).
    private void EnsureAddButton()
    {
        if (addEmptyCardButton) return;
        var tile = Resources.Load<GameObject>("ActionCardAddTile");
        if (!tile) return;
        addEmptyCardButton = Instantiate(tile, panelParent).GetComponent<Button>();
        if (!addEmptyCardButton) return;
        addEmptyCardButton.onClick.AddListener(AddEmptyCard);
        var count = addEmptyCardButton.transform.Find("Count");
        if (count) addEmptyCardCountText = count.GetComponent<TMP_Text>();
    }

    private void RefreshAddButton()
    {
        if (!addEmptyCardButton) return;

        if (addEmptyCardButton.transform.parent == panelParent)
            addEmptyCardButton.transform.SetAsLastSibling();

        bool canAdd = maxEmptyCards <= 0 || emptyCardCount < maxEmptyCards;
        addEmptyCardButton.gameObject.SetActive(canAdd && filter != DeckFilter.Predefined);
        if (addEmptyCardCountText)
            addEmptyCardCountText.text = maxEmptyCards > 0 ? $"{emptyCardCount} of {maxEmptyCards} used" : "";
    }

    public void SpawnPlayedActionCards()
    {
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            if (actionCard.isPlayed)
            {
                GameObject cardObject = Instantiate(actionCardPrefab, newpanelParent);
                ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
                if (hook != null)
                {
                    hook.SetActionCard(actionCard);
                }
            }
        }
    }

    public void SpawnPlayedActionCardsInNetwork()
    {
        foreach (var actionCard in actionCardsDatabase.actionCards)
        {
            if (actionCard.isPlayed)
            {
                PhotonView containerPhotonView = newpanelParent.GetComponent<PhotonView>();
                if (!containerPhotonView)
                {
                    Debug.LogError("Container does not have a PhotonView component.");
                    break;
                }

                int parentViewID = containerPhotonView.ViewID;
                object[] initData = new object[1];
                initData[0] = parentViewID;

                GameObject cardObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", actionCardPrefab.name),
                    Vector3.zero, Quaternion.identity, 0, initData);

                if (!cardObject.GetComponent<SetPhotonParentOnInstantiation>())
                {
                    Debug.LogWarning(
                        "Prefab does not have SetPhotonParentOnInstantiation component. Setting the parent is not possible.");
                }

                ActionCardsHook hook = cardObject.GetComponent<ActionCardsHook>();
                if (hook != null)
                {
                    hook.SetActionCardInNetwork(actionCard);
                }
            }
        }
    }
}

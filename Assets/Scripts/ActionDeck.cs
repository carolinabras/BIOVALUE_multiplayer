using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class ActionDeck : MonoBehaviour
{
    [SerializeField] private GameObject panelMenu;


    [SerializeField] private GameObject buttonPlayCards;

    [SerializeField] private TMP_Text playedCardsText;

    public List<ActionCard> playedCards;
    public List<int> playedCardIds;
    [SerializeField] private GameObject panelMenuMaster;


    [SerializeField] private float animationTime = 0.5f;

    private bool isOpen = false;

    [SerializeField] private ActionCardsDatabase actionCardsDatabase;
    [SerializeField] private ActionCardsSpawner actionCardsSpawner;

    [Tooltip("Shows '2 / 3 selected' or what is missing. Created above the play button if empty.")]
    [SerializeField] private TMP_Text selectionStatusText;

    [Tooltip("Side drawer (BioValue/Build Action Drawer). When set it replaces the old full-screen panel.")]
    [SerializeField] private ActionDrawerView drawerView;

    private bool hasPlayed;

    private void OnEnable()
    {
        if (actionCardsSpawner) actionCardsSpawner.SelectionChanged += UpdatePlayState;

        // Actions round starts: drawer closed, card fan on the screen edge.
        if (drawerView)
        {
            isOpen = false;
            drawerView.Hide(true);
            InstrumentLinkHighlights.Visible = false;
            UpdatePlayState(); // the hand itself is built on first open
        }
    }

    // Card fan / "See board" button.
    public void ToggleMenu()
    {
        if (isOpen) CloseMenu();
        else OpenMenu();
    }

    // Tabs: 0 = All, 1 = Action (predefined), 2 = Open.
    public void SetFilter(int filter)
    {
        if (actionCardsSpawner) actionCardsSpawner.SetFilter((ActionCardsSpawner.DeckFilter)filter);
    }

    // ObjectsManager switches this object off outside the actions round. The drawer and the fan
    // live next to it (not inside), so hide them too: without this they stay clickable but the
    // "Your play" tray no longer hears about selection changes.
    private void OnDisable()
    {
        if (actionCardsSpawner) actionCardsSpawner.SelectionChanged -= UpdatePlayState;
        if (drawerView)
        {
            isOpen = false;
            drawerView.HideAll();
            InstrumentLinkHighlights.Visible = false;
        }
    }

    public void PlaySelectedCardsButton()
    {
        if (!actionCardsSpawner.CanPlay(out string reason))
        {
            Debug.LogWarning($"Cannot play cards: {reason}");
            UpdatePlayState();
            return;
        }

        playedCards.Clear();
        playedCardIds.Clear();

        int localPlayerId = GameState.Instance.localPlayerIndex;

        foreach (var hook in actionCardsSpawner.SelectedHooks)
        {
            var card = hook.actionCard;
            playedCards.Add(card);
            playedCardIds.Add(card.id);

            if (!string.IsNullOrEmpty(card.descriptionGeneral))
                GameState.Instance.SetActionCardDescription(localPlayerId, card.id, card.descriptionGeneral);
            if (!string.IsNullOrEmpty(card.descriptionHow))
                GameState.Instance.SetActionCardDescriptionHow(localPlayerId, card.id, card.descriptionHow);
            GameState.Instance.SetActionCardLinks(localPlayerId, card.id, card.linkedInstruments.ToArray());
        }

        GameState.Instance.SetPlayerActionCards(localPlayerId, playedCardIds);
        hasPlayed = true;

        CloseMenu();

        playedCardsText.text = playedCardIds.Count == 1
            ? "You played 1 action card"
            : $"You played {playedCardIds.Count} action cards";
        Invoke(nameof(ClearPlayedCardsText), 5f);
    }

    public void ClearPlayedCardsText()
    {
        Vector2 target = new Vector2(playedCardsText.rectTransform.anchoredPosition.x + 1000000, playedCardsText.rectTransform.anchoredPosition.y);
        LeanTween.move(playedCardsText.GameObject(), target, 0.25f).setEaseOutCubic();
    }

    public void OpenMenu()
    {
        if (drawerView)
        {
            isOpen = true;
            if (actionCardsSpawner)
            {
                actionCardsSpawner.EnsureSpawned();
                actionCardsSpawner.RefreshCards();
            }
            drawerView.Show();
            InstrumentLinkHighlights.Visible = true;
            UpdatePlayState();
            return;
        }

        if (!isOpen)
        {

            isOpen = true;
            panelMenuMaster.SetActive(true);
            panelMenu.SetActive(true);
            buttonPlayCards.SetActive(true);

            // Instruments may have been placed since the cards were built.
            if (actionCardsSpawner) actionCardsSpawner.RefreshCards();
            UpdatePlayState();

            float targetScale = 1f;
            if (panelMenu.TryGetComponent(out FitGridToViewport fit))
            {
                fit.Recalculate();
                targetScale = fit.FitScale;
            }
            LeanTween.scale(panelMenu.GetComponent<RectTransform>(), new Vector3(targetScale, targetScale, 1f), animationTime).setEaseOutBack()
                .setOnComplete(RebuildCardText);
        }
    }

    // TextMesh Pro sets each letter's edge sharpness from the scale at the moment the text is built.
    // The cards are built while the panel is at scale 0, so after the open animation the text looks
    // jagged/pixelated. Rebuilding it at the final scale fixes that.
    private void RebuildCardText()
    {
        foreach (var text in panelMenu.GetComponentsInChildren<TMP_Text>(true))
            text.ForceMeshUpdate(true);
    }

    public void CloseMenu()
    {
        if (drawerView)
        {
            isOpen = false;
            drawerView.Hide();
            InstrumentLinkHighlights.Visible = false; // glow only while the deck is open
            return;
        }

        if (isOpen)
        {

            isOpen = false;
            panelMenuMaster.SetActive(false);
            buttonPlayCards.SetActive(false);
            if (selectionStatusText) selectionStatusText.gameObject.SetActive(false);
            LeanTween.scale(panelMenu.GetComponent<RectTransform>(), Vector3.zero, animationTime).setEaseInBack()
                .setOnComplete(() => panelMenu.SetActive(false));
        }
    }

    // Play button only works with 1–3 ready cards; the label says what's missing.
    private void UpdatePlayState()
    {
        if (!actionCardsSpawner) return;

        bool canPlay = actionCardsSpawner.CanPlay(out string reason);

        if (drawerView)
        {
            drawerView.Render(actionCardsSpawner.SelectedHooks.ToList(), actionCardsSpawner.MaxCardsPerRound,
                canPlay, reason, hasPlayed);
            return;
        }

        if (!buttonPlayCards) return;
        if (buttonPlayCards.TryGetComponent(out Button button))
            button.interactable = canPlay;

        EnsureStatusText();
        if (!selectionStatusText) return;

        selectionStatusText.gameObject.SetActive(isOpen);
        int count = actionCardsSpawner.SelectedCount;
        int max = actionCardsSpawner.MaxCardsPerRound;
        string counter = max > 0 ? $"<b>{count} / {max} selected</b>" : $"<b>{count} selected</b>";
        selectionStatusText.text = canPlay ? $"{counter}\nReady to play" : $"{counter}\n<color=#FFB27A>{reason}</color>";
    }

    private void EnsureStatusText()
    {
        if (selectionStatusText || !buttonPlayCards) return;

        var buttonRT = (RectTransform)buttonPlayCards.transform;
        var go = new GameObject("SelectionStatus", typeof(RectTransform));
        go.layer = buttonPlayCards.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(buttonRT.parent, false);
        // Same column as the play button, just above it, a bit wider to the left.
        rt.anchorMin = new Vector2(buttonRT.anchorMin.x - 0.1f, buttonRT.anchorMax.y);
        rt.anchorMax = new Vector2(buttonRT.anchorMax.x, buttonRT.anchorMax.y + 0.08f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var text = go.AddComponent<TextMeshProUGUI>();
        if (playedCardsText) text.font = playedCardsText.font;
        text.fontSize = 24;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14;
        text.fontSizeMax = 26;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = new Color32(0xF3, 0xF1, 0xEA, 0xFF);
        text.raycastTarget = false;
        selectionStatusText = text;
    }
}

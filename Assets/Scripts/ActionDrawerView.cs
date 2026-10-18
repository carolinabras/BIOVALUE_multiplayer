using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The action deck as a light side drawer: slides in from the right so the board stays visible,
// and slides away (arrow on its left edge) leaving the card fan on the screen edge to bring it back.
// Built by the editor menu "BioValue/Build Action Drawer"; driven by ActionDeck.
public class ActionDrawerView : MonoBehaviour
{
    public RectTransform drawer;
    public HandFan fan;
    public float slideTime = 0.35f;

    [Header("Header")]
    public TMP_Text title;

    [Header("Cards")]
    public ScrollRect cardsScroll;

    [Header("Your play (tray)")]
    public TMP_Text summaryText;
    public RectTransform trayContent;
    public GameObject trayItemTemplate;
    public GameObject trayEmpty;
    public GameObject reasonRow;
    public TMP_Text reasonText;
    public Button playButton;
    public Image playFill;
    public TMP_Text playLabel;

    public bool IsOpen { get; private set; }

    private static readonly Color Orange = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color Pink = new Color32(0xB8, 0x15, 0x6A, 0xFF);
    private static readonly Color PlayReady = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PlayOff = new Color32(0xE6, 0xDC, 0xCD, 0xFF);
    private static readonly Color Warning = new Color32(0x8A, 0x3A, 0x0C, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x5A, 0x48, 0xFF);

    private float openX;
    private bool initialized;
    private readonly List<GameObject> trayItems = new List<GameObject>();

    private void Init()
    {
        if (initialized || !drawer) return;
        initialized = true;
        openX = drawer.anchoredPosition.x;
        if (trayItemTemplate) trayItemTemplate.SetActive(false);
    }

    private float ClosedX => openX + drawer.rect.width + 80f;

    public void Show(bool instant = false)
    {
        Init();
        if (!drawer) return;
        IsOpen = true;
        drawer.gameObject.SetActive(true);
        Slide(ClosedX, openX, instant, null);
        if (fan) fan.SetShown(false, instant);
    }

    public void Hide(bool instant = false)
    {
        Init();
        if (!drawer) return;
        IsOpen = false;
        if (!drawer.gameObject.activeSelf) instant = true;
        Slide(drawer.anchoredPosition.x, ClosedX, instant, () => drawer.gameObject.SetActive(false));
        if (fan) fan.SetShown(true, instant);
    }

    // Outside the actions round: no drawer and no card fan.
    public void HideAll()
    {
        Init();
        IsOpen = false;
        if (drawer)
        {
            LeanTween.cancel(drawer.gameObject);
            SetX(ClosedX);
            drawer.gameObject.SetActive(false);
        }
        if (fan) fan.SetShown(false, true);
    }

    private void Slide(float from, float to, bool instant, System.Action done)
    {
        LeanTween.cancel(drawer.gameObject);
        if (instant || slideTime <= 0f)
        {
            SetX(to);
            done?.Invoke();
            return;
        }
        SetX(from);
        LeanTween.value(drawer.gameObject, from, to, slideTime).setEaseOutCubic()
            .setOnUpdate(SetX)
            .setOnComplete(() => done?.Invoke());
    }

    private void SetX(float x)
    {
        var p = drawer.anchoredPosition;
        p.x = x;
        drawer.anchoredPosition = p;
    }

    // Title, tray (one small card per selected action), the first problem and the play button.
    public void Render(IList<ActionCardsHook> selected, int max, bool canPlay, string reason, bool played)
    {
        Init();
        int count = selected.Count;
        int ready = 0;
        foreach (var h in selected)
            if (h && h.IsReady(false, out _)) ready++;

        if (title) title.text = max <= 0 ? "Choose your action cards" : max == 1 ? "Choose 1 action card" : $"Choose up to {max} action cards";
        if (summaryText)
        {
            string selectedText = max > 0 ? $"{count}/{max} selected" : $"{count} selected";
            summaryText.text = count == 0 ? "Nothing selected yet" : $"{selectedText} · {ready} ready";
        }

        RenderTray(selected);

        bool showProblem = count > 0 && !canPlay && !string.IsNullOrEmpty(reason);
        bool showPlayed = played && canPlay;
        if (reasonRow) reasonRow.SetActive(showProblem || showPlayed);
        if (reasonText)
        {
            reasonText.text = showProblem ? reason : showPlayed ? "Played. You can still change your cards and play again." : "";
            reasonText.color = showProblem ? Warning : Muted;
        }

        if (playButton) playButton.interactable = canPlay;
        if (playFill) playFill.color = canPlay ? PlayReady : PlayOff;
        if (playLabel)
        {
            playLabel.text = count == 0 ? "Play cards" : count == 1 ? "Play 1 card" : $"Play {count} cards";
            playLabel.color = canPlay ? new Color32(0x1E, 0x0A, 0x14, 0xFF) : (Color)new Color32(0x7A, 0x6A, 0x58, 0xFF);
        }

        if (fan) fan.SetCount(count, max);
    }

    // Filled = ready; white with a coloured border and an "!" = something missing.
    private void RenderTray(IList<ActionCardsHook> selected)
    {
        if (!trayContent || !trayItemTemplate) return;
        if (trayEmpty) trayEmpty.SetActive(selected.Count == 0);

        while (trayItems.Count < selected.Count)
            trayItems.Add(Instantiate(trayItemTemplate, trayContent));

        for (int i = 0; i < trayItems.Count; i++)
        {
            var item = trayItems[i];
            bool used = i < selected.Count && selected[i];
            item.SetActive(used);
            if (!used) continue;

            var hook = selected[i];
            bool ready = hook.IsReady(false, out _);
            Color accent = hook.IsOpenCard ? Pink : Orange;
            item.name = "Tray " + hook.Number;

            var outer = item.GetComponent<Image>();
            if (outer) outer.color = accent;
            var inner = item.transform.Find("Inner");
            if (inner) inner.gameObject.SetActive(!ready);
            var number = item.transform.Find("Number")?.GetComponent<TMP_Text>();
            if (number)
            {
                number.text = hook.Number;
                number.color = ready ? Color.white : accent;
            }
            var alert = item.transform.Find("Alert");
            if (alert) alert.gameObject.SetActive(!ready);

            var button = item.GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ScrollToCard(hook));
            }
        }
        LayoutRebuilder.MarkLayoutForRebuild(trayContent);
    }

    // Tray item clicked: scroll the card into view and give it a little bump.
    public void ScrollToCard(ActionCardsHook hook)
    {
        if (!hook || !hook.gameObject.activeInHierarchy) return;
        var card = (RectTransform)hook.transform;

        if (cardsScroll && cardsScroll.content && cardsScroll.viewport)
        {
            Canvas.ForceUpdateCanvases();
            float contentH = cardsScroll.content.rect.height;
            float viewH = cardsScroll.viewport.rect.height;
            if (contentH > viewH)
            {
                float center = -card.anchoredPosition.y; // grid cells: distance from the top to the card's centre
                float target = 1f - Mathf.Clamp01((center - viewH * 0.5f) / (contentH - viewH));
                LeanTween.cancel(cardsScroll.gameObject);
                LeanTween.value(cardsScroll.gameObject, cardsScroll.verticalNormalizedPosition, target, 0.3f)
                    .setEaseOutCubic()
                    .setOnUpdate(v => cardsScroll.verticalNormalizedPosition = v);
            }
        }

        LeanTween.cancel(card.gameObject);
        card.localScale = Vector3.one;
        LeanTween.scale(card, new Vector3(1.05f, 1.05f, 1f), 0.12f).setDelay(0.25f).setEaseOutQuad().setLoopPingPong(1);
    }
}

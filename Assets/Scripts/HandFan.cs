using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// A fan of action card backs peeking in from the screen edge during the actions round.
// Clicking it opens the deck; hovering spreads the cards a little.
public class HandFan : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform[] cards;
    public float[] angles = { 22f, 11f, 0f, -11f };
    public float hoverSpread = 1.35f;
    public float hoverLift = 18f;
    public float slideTime = 0.3f;
    public TMP_Text countText;

    private float hover, hoverTarget;
    private float shownX;
    private bool initialized;
    private Vector2[] basePositions;

    private void Init()
    {
        if (initialized) return;
        initialized = true;
        shownX = ((RectTransform)transform).anchoredPosition.x;
        basePositions = new Vector2[cards.Length];
        for (int i = 0; i < cards.Length; i++)
            if (cards[i]) basePositions[i] = cards[i].anchoredPosition;
    }

    private void Awake() => Init();

    public void OnPointerEnter(PointerEventData e) => hoverTarget = 1f;
    public void OnPointerExit(PointerEventData e) => hoverTarget = 0f;

    private void OnDisable()
    {
        hover = hoverTarget = 0f;
        Apply();
    }

    private void Update()
    {
        if (Mathf.Approximately(hover, hoverTarget)) return;
        hover = Mathf.MoveTowards(hover, hoverTarget, Time.unscaledDeltaTime * 6f);
        Apply();
    }

    private void Apply()
    {
        if (!initialized) return;
        float t = Mathf.SmoothStep(0f, 1f, hover);
        for (int i = 0; i < cards.Length; i++)
        {
            if (!cards[i]) continue;
            float a = i < angles.Length ? angles[i] : 0f;
            cards[i].localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Lerp(1f, hoverSpread, t));
            cards[i].anchoredPosition = basePositions[i] + new Vector2(-hoverLift * t, 0f);
        }
    }

    public void SetCount(int count, int max)
    {
        if (countText) countText.text = max > 0 ? $"{count}/{max}" : count.ToString();
    }

    // Shows or hides the count bubble next to the label; the pill shrinks to fit the label alone.
    private float pillWidth = -1f;
    public void ShowCount(bool show)
    {
        if (!countText || !(countText.transform.parent is RectTransform bubble)) return;
        bubble.gameObject.SetActive(show);
        if (!(bubble.parent is RectTransform pill)) return;
        if (pillWidth < 0f) pillWidth = pill.sizeDelta.x;
        pill.sizeDelta = new Vector2(show ? pillWidth : pillWidth - bubble.sizeDelta.x - 6f, pill.sizeDelta.y);
    }

    // Slides off the edge while the drawer is open.
    public void SetShown(bool shown, bool instant = false)
    {
        Init();
        var rt = (RectTransform)transform;
        float target = shown ? shownX : shownX + rt.rect.width + 40f;
        LeanTween.cancel(gameObject);
        if (!shown && !gameObject.activeInHierarchy) instant = true;
        if (shown) gameObject.SetActive(true);
        if (instant || slideTime <= 0f)
        {
            SetX(target);
            if (!shown) gameObject.SetActive(false);
            return;
        }
        LeanTween.value(gameObject, rt.anchoredPosition.x, target, slideTime).setEaseOutCubic()
            .setOnUpdate(SetX)
            .setOnComplete(() => { if (!shown) gameObject.SetActive(false); });
    }

    private void SetX(float x)
    {
        var rt = (RectTransform)transform;
        var p = rt.anchoredPosition;
        p.x = x;
        rt.anchoredPosition = p;
    }
}

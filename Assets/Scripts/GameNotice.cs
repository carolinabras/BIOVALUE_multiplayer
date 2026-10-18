using TMPro;
using UnityEngine;

// A full-screen notice over the game (dim + white card): e.g. "The game master ended the game ·
// Going back to the main menu…" before the players are sent back. Built by
// "BioValue/Build Turn Ribbon and GM Panel".
public class GameNotice : MonoBehaviour
{
    public static GameNotice Instance { get; private set; }

    public GameObject content;
    public CanvasGroup group;
    public RectTransform card;
    public TMP_Text title;
    public TMP_Text body;

    private void Awake()
    {
        Instance = this;
        if (content) content.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string titleText, string bodyText)
    {
        if (!content) return;
        if (title) title.text = titleText;
        if (body) body.text = bodyText;
        transform.SetAsLastSibling(); // over everything, the results card included
        content.SetActive(true);
        if (group)
        {
            group.alpha = 0f;
            LeanTween.cancel(group.gameObject);
            LeanTween.alphaCanvas(group, 1f, 0.25f).setIgnoreTimeScale(true);
        }
        if (card)
        {
            card.localScale = Vector3.one * 0.9f;
            LeanTween.scale(card, Vector3.one, 0.3f).setEaseOutBack().setIgnoreTimeScale(true);
        }
    }
}

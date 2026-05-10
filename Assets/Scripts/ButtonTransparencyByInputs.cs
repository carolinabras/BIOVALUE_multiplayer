using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTransparencyByInputs : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_InputField[] requiredInputs;

    [Range(0f, 1f)]
    [SerializeField] private float alphaEmpty = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float alphaFull  = 1.0f;

    private CanvasGroup _group;

    private void Start()
    {
        if (button == null) return;

        _group = button.gameObject.GetComponent<CanvasGroup>();
        if (_group == null) _group = button.gameObject.AddComponent<CanvasGroup>();

        foreach (var field in requiredInputs)
            if (field != null)
                field.onValueChanged.AddListener(_ => Refresh());

        Refresh();
    }

    private void Refresh()
    {
        if (_group == null) return;

        bool allFilled = true;
        foreach (var field in requiredInputs)
        {
            if (field == null || field.text.Length == 0)
            {
                allFilled = false;
                break;
            }
        }

        _group.alpha = allFilled ? alphaFull : alphaEmpty;
    }
}

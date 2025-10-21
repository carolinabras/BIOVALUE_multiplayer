using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisableParentButtonOnFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Button parentButton;

    private void Awake()
    {
        parentButton = GetComponentInParent<Button>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (parentButton != null)
            parentButton.interactable = false;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (parentButton != null)
            parentButton.interactable = true;
    }
}
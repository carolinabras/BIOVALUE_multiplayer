using UnityEngine;

public class SelectObjective : MonoBehaviour
{
    private ObjectivesHook objectiveHook;

    private void Awake()
    {
         objectiveHook = GetComponent<ObjectivesHook>();
    }

    public void ToggleSelection()
    {
        if (objectiveHook == null || objectiveHook.objective == null) return;

        // toggle no hook
        objectiveHook.objective.isSelected = !objectiveHook.objective.isSelected;
        UpdateVisual();
    }
    
    public void UpdateVisual()
    {
        if (objectiveHook == null || objectiveHook.objective == null) return;

        // cores mudar
        UnityEngine.UI.Image backgroundImage = GetComponent<UnityEngine.UI.Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = objectiveHook.objective.isSelected ? Color.green : Color.white;
        }
    }
}

using UnityEngine;

public class GameMasterPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject gameMasterPanel;

    public void ToggleGameMasterPanel()
    {
        if (gameMasterPanel != null)
        {
            bool isActive = gameMasterPanel.activeSelf;
            gameMasterPanel.SetActive(!isActive);
        }
    }
}

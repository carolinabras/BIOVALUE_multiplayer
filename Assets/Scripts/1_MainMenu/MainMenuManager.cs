using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // Set by "BioValue/Build Main Menu".
    public Button startButton;
    public Image startFill;
    public TMP_Text startLabel;
    public GameObject startArrow;    // swapped for the spinner once pressed
    public GameObject startSpinner;
    public CanvasGroup quitGroup;    // dims once Start game is pressed
    public TMP_Text versionLabel;

    private static readonly Color Connecting = new Color32(0xC9, 0x3A, 0x84, 0xFF);
    private bool _starting;

    private void Start()
    {
        if (versionLabel) versionLabel.text = VersionLabel.Text;
    }

    public void StartGame()
    {
        if (_starting) return; // a second click would load the scene twice
        _starting = true;
        if (startLabel) startLabel.text = "Connecting…";
        if (startFill) startFill.color = Connecting;
        if (startButton) startButton.interactable = false;
        if (startArrow) startArrow.SetActive(false);
        if (startSpinner) startSpinner.SetActive(true);
        if (quitGroup)
        {
            quitGroup.alpha = 0.35f;
            quitGroup.interactable = false;
        }
        StartCoroutine(StartGameCoroutine());
        //copy instruments database to database copy

    }

    IEnumerator StartGameCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(1); //Loading
    }

    // A browser tab can't close itself: the web build leaves full screen and shows a goodbye screen.
    public void QuitGame()
    {
        WebBridge.Quit();
    }

    //TODO
    /*public void OpenSettings()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene("CreditsScene");
    }

    public void OpenTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    */

}

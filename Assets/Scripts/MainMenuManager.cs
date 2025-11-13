using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }
    
    IEnumerator StartGameCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(1); //Lobby
    }
    
    public void QuitGame()
    {
        Application.Quit();
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

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{

    
    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
        //copy instruments database to database copy
        
    }
    
    IEnumerator StartGameCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(0); //Lobby
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

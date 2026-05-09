using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public string gameScene = "Level_01";
    public string mainMenuScene = "MainMenu";

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }
}
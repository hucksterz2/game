using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Уровень 1")]
    public string firstLevelName = "Level1";

    public void PlayGame()
    {
        SceneManager.LoadScene(firstLevelName);
    }

    public void QuitGame()
    {
        Debug.Log("Выйти из игры");
        Application.Quit();
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Уровень 1")]
    public string firstLevelName = "Level1";

    void Start()
    {
        Time.timeScale = 1f;
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstLevelName);
    }

    public void QuitGame()
    {
        Debug.Log("Выйти из игры");
        Application.Quit();
    }
}
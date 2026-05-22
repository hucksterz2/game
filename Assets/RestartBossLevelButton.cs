using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartBossLevelButton : MonoBehaviour
{
    [Header("Имя сцены босса (если LastBossLevel пуст)")]
    public string fallbackLevel = "Final Level";

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        string scene = !string.IsNullOrEmpty(PlayerDefeatEnding.LastBossLevel)
            ? PlayerDefeatEnding.LastBossLevel
            : fallbackLevel;
        SceneManager.LoadScene(scene);
    }
}
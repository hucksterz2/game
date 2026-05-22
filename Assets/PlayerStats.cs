using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    public static int BonusMaxHP = 0;
    public static float BonusSpeed = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ApplyDelayed());
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyDelayed());
    }

    IEnumerator ApplyDelayed()
    {
        yield return null;
        yield return null;
        ApplyToPlayer();
    }

    public static void ApplyToPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;
        Transform player = playerObj.transform.root;

        if (BonusMaxHP > 0)
        {
            PlayerHealth ph = player.GetComponentInChildren<PlayerHealth>();
            if (ph != null)
            {
                ph.maxHP += BonusMaxHP;
                ph.currentHP = ph.maxHP;
            }
        }

        if (BonusSpeed > 0)
        {
            PlayerController pc = player.GetComponentInChildren<PlayerController>();
            if (pc != null) pc.speed += BonusSpeed;
        }
    }
}
using UnityEngine;

public class BossArenaTrigger : MonoBehaviour
{
    [Header("References")]
    public BossHealthBarUI bossHealthBar;

    [Header("Settings")]
    public string playerTag = "Player";
    public bool hideOnExit = false;

    private bool playerInside = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && !playerInside)
        {
            playerInside = true;
            
            if (bossHealthBar != null)
                bossHealthBar.ShowBar();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && hideOnExit)
        {
            playerInside = false;
            if (bossHealthBar != null)
                bossHealthBar.HideBar();
        }
    }
}
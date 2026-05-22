using UnityEngine;

public class DeathScreenSuppressor : MonoBehaviour
{
    private PlayerHealth ph;

    void Update()
    {
        GameObject dc = GameObject.Find("DeathCanvas");
        if (dc != null) Destroy(dc);

        if (ph == null) ph = FindFirstObjectByType<PlayerHealth>();

        if (ph != null && ph.currentHP <= 0)
        {
            ph.StopAllCoroutines();
        }
    }
}
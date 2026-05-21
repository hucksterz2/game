using UnityEngine;
using System.Collections;

public class BossArena : MonoBehaviour
{
    public HeavyBanditBoss boss;
    public GameObject[]    doors;

    bool activated;
    bool ready;

    IEnumerator Start()
    {
        yield return null;
        ready = true;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!ready || activated) return;
        if (!col.CompareTag("Player") && col.GetComponentInParent<PlayerController>() == null) return;
        activated = true;
        foreach (var d in doors)
            if (d != null) d.SetActive(true);
        if (boss != null) boss.Activate();
    }
}

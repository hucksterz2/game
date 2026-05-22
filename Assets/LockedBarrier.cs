using UnityEngine;

public class LockedBarrier : MonoBehaviour
{
    [Header("Какой флаг должен быть установлен чтобы исчезнуть")]
    public string requiredFlag = "hasKey1";

    [Header("Что делать когда флаг установлен")]
    public bool disableSelf = true;
    public bool destroyOnUnlock = false;

    void Update()
    {
        if (GameFlags.Get(requiredFlag))
        {
            if (destroyOnUnlock) Destroy(gameObject);
            else if (disableSelf) gameObject.SetActive(false);
        }
    }
}
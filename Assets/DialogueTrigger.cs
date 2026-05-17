using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerType { OnStart, OnPlayerEnter, Manual }

    [Header("ÊÎÃÄÀ")]
    public TriggerType trigger = TriggerType.OnStart;

    [Header("ÏÎĞÒĞÅÒ")]
    public Sprite portrait;

    [Header("ÑÎÎÁÙÅÍÈß")]
    [TextArea(2, 5)] public string[] lines;

    [Header("ñêîê ğàç")]
    public bool oneShot = true;

    private bool used = false;

    void Start()
    {
        if (trigger == TriggerType.OnStart) Trigger();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (trigger != TriggerType.OnPlayerEnter) return;
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            Trigger();
    }

    public void Trigger()
    {
        if (used && oneShot) return;
        used = true;

        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
        DialogueManager.Instance.StartDialogue(portrait, lines);
    }
}
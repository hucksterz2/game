using UnityEngine;

public class BossController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveDistance = 5f;
    public float pauseTime = 1f;

    private float startX;
    private float fixedY;
    private bool movingRight = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        startX = transform.position.x;
        fixedY = transform.position.y;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        animator.SetBool("isWalking", true);
    }

    void Update()
    {
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                movingRight = !movingRight;
                animator.SetBool("isWalking", true);
            }
            return;
        }

        float direction = movingRight ? 1f : -1f;
        Vector3 pos = transform.position;
        pos.x += direction * moveSpeed * Time.deltaTime;
        pos.y = fixedY;
        transform.position = pos;

        spriteRenderer.flipX = movingRight;

        float distanceMoved = Mathf.Abs(transform.position.x - startX);
        if (distanceMoved >= moveDistance)
        {
            isPaused = true;
            pauseTimer = pauseTime;
            animator.SetBool("isWalking", false);
        }
    }
}
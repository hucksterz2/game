using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    public int maxJumps = 2;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck; 

    private Rigidbody2D rb;
    private Animator anim;
    private int jumpsLeft;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();

        Animator[] animators = GetComponentsInChildren<Animator>();
        if (animators.Length > 0)
            anim = animators[0];

        jumpsLeft = maxJumps;
    }

    public LayerMask groundLayer; 

    bool IsGrounded()
    {
        if (groundCheck == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position, Vector2.down, 0.15f, groundLayer);

        Debug.DrawRay(groundCheck.position, Vector2.down * 0.15f,
            hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }

    void FixedUpdate()
    {
        if (IsGrounded())
            jumpsLeft = maxJumps;
    }

    void Update()
    {
        Debug.Log("IsGrounded: " + IsGrounded() + " | jumpsLeft: " + jumpsLeft);

        float move = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            move = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            move = 1f;

        rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);

        if (anim != null)
            anim.SetFloat("MoveSpeed", Mathf.Abs(move));

        if (move > 0) transform.localScale = new Vector3(-1, 1, 1);
        if (move < 0) transform.localScale = new Vector3(1, 1, 1);

        if (IsGrounded())
            jumpsLeft = maxJumps;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsLeft--;
        }
    }
}
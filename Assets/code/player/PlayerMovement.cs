using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal"); // A/D or ← →
        moveInput.y = Input.GetAxisRaw("Vertical");   // W/S or ↑ ↓
        moveInput = moveInput.normalized;

        HandleFlip(moveInput);
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * speed;
    }

    void HandleFlip(Vector2 dir)
    {
        if (dir.x > 0.1f)
            sr.flipX = true;

        else if (dir.x < -0.1f)
            sr.flipX = false;
    }
}
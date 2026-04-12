using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator anim;
    private SpriteRenderer sr;

    [Header("Audio Settings")]
    public float footstepInterval = 0.4f;
    private float footstepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();   // ← already here

        // Make sure we start facing right (default sprite direction)
        if (sr != null) sr.flipX = false;
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.sqrMagnitude > 0)
            moveInput = moveInput.normalized;

        HandleFlip(moveInput.x);

        bool isMoving = moveInput.magnitude > 0.1f;
        if (anim != null) anim.SetBool("walk", isMoving);

        HandleFootsteps(isMoving);
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * speed;
    }

    public void UpdateHealthStatus(int health) { } // you already have this

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving)
        {
            if (footstepTimer <= 0)
            {
                AudioManager.Instance.PlayWalk(false, 2f);
                footstepTimer = footstepInterval;
            }
            footstepTimer -= Time.deltaTime;
        }
        else
        {
            AudioManager.Instance.StopWalk();
            footstepTimer = 0;
        }
    }

    // ──────────────────────────────────────
    // FIXED: now uses flipX instead of localScale
    // ──────────────────────────────────────
    void HandleFlip(float horizontalInput)
    {
        if (sr == null) return;

        if (horizontalInput > 0.1f)
            sr.flipX = true;   // moving right  
        else if (horizontalInput < -0.1f)
            sr.flipX = false;    // moving left
    }
}
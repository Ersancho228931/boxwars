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
    private int currentHealth; // Local reference to health

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Only normalize if there is input, to avoid errors
        if (moveInput.sqrMagnitude > 0)
        {
            moveInput = moveInput.normalized;
        }

        HandleFlip(moveInput.x); // Pass only the X direction

        bool isMoving = moveInput.magnitude > 0.1f;
        if (anim != null) anim.SetBool("walk", isMoving);

        HandleFootsteps(isMoving);
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * speed;
    }

    // Called by PlayerHealth script
    public void UpdateHealthStatus(int health)
    {
        currentHealth = health;
    }

    // Change your HandleFootsteps to this:
    void HandleFootsteps(bool isMoving)
    {
        if (isMoving)
        {
            if (footstepTimer <= 0)
            {
                // Always play normal walk now
                AudioManager.Instance.PlayWalk(false, 0.4f);
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

    void HandleFlip(float horizontalInput)
    {
        // If moving right
        if (horizontalInput > 0.1f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        // If moving left
        else if (horizontalInput < -0.1f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}
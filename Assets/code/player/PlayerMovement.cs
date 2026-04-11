using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator anim;

    [Header("Audio Settings")]
    public float footstepInterval = 0.4f;
    private float footstepTimer;
    private int currentHealth; // Local reference to health

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        HandleFlip(moveInput);

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

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving)
        {
            // First step is instant, others follow interval
            if (footstepTimer <= 0)
            {
                bool isInjured = currentHealth < 20;

                // Plays special loud sound if health < 20
                AudioManager.Instance.PlayWalk(isInjured, 0.6f);
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

    void HandleFlip(Vector2 dir)
    {
        // By using transform.localScale, ALL children (items/weapons) flip too!
        if (dir.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (dir.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShooterController : MonoBehaviour
{
    [Header("AI")]
    public float moveSpeed = 1.5f;
    public float rotateSpeed = 120f; // degrees per second
    public float detectionRange = 10f;
    public float shootingInterval = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public int projectileDamage = 20;
    [Header("Sound")]
    public AudioClip shootSound;

    [Header("Pickup as weapon")]
    public bool canBePickedWhileAlive = true;
    public int allowedPickupHandIndex = 0;

    [Header("Converted block settings")]
    public bool shootWhenConverted = true;
    public float convertedDetectionRange = 8f;

    [Header("Targeting filter (optional)")]
    public bool onlyTargetNames = false;
    public string[] targetNameFilters;
    public bool onlyTargetTags = false;
    public string[] targetTagFilters;

    [Header("Debug")]
    public bool enableDebug = false;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private float lastShotTime = -999f;
    private float lastConvertedShotTime = -999f;
    private Camera mainCam;
    private SpriteRenderer sr;
    private Animator animator;

    private Vector2 fixedVelocity = Vector2.zero;
    private float fixedRotation = 0f;
    private bool fixedHasRotation = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            fixedRotation = rb.rotation;
        }
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            animator.applyRootMotion = false;
        }
        enemyHealth = GetComponent<EnemyHealth>();
        mainCam = Camera.main;
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        if (enableDebug)
            Debug.Log($"ShooterController.Start on {gameObject.name} — projectilePrefab is {(projectilePrefab != null ? "assigned" : "NULL")}");
    }

    void Update()
    {
        bool isCarried = IsCarried();
        if (isCarried)
            HandleCarried();
        else
            HandleAI();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (rb.bodyType != RigidbodyType2D.Static)
        {
            if (fixedVelocity.sqrMagnitude > 0.0001f)
            {
                rb.MovePosition(rb.position + fixedVelocity * Time.fixedDeltaTime);
                rb.velocity = fixedVelocity;
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
        }

        if (fixedHasRotation)
        {
            float step = rotateSpeed * Time.fixedDeltaTime;
            float targetRotation = Mathf.MoveTowardsAngle(rb.rotation, fixedRotation, step);
            
            // Static bodies don't support MoveRotation - set rotation directly
            if (rb.bodyType == RigidbodyType2D.Static)
            {
                transform.rotation = Quaternion.Euler(0, 0, targetRotation);
            }
            else
            {
                rb.MoveRotation(targetRotation);
            }
        }
        fixedHasRotation = false;
    }

    public bool IsCarried()
    {
        if (transform.parent == null) return false;
        if (rb == null) return false;
        if (rb.simulated) return false;
        return GetComponentInParent<PlayerCarry>() != null;
    }

    void HandleAI()
    {
        if (enemyHealth != null && enemyHealth.isDead && enemyHealth.isConvertedToBlock && shootWhenConverted)
        {
            HandleConvertedBlockBehavior();
            return;
        }

        if (player == null) return;

        if (enemyHealth != null && enemyHealth.isDead)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) fixedVelocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toPlayer = ((Vector2)player.position - currentPos);
        float dist = toPlayer.magnitude;

        if (rb != null && rb.bodyType != RigidbodyType2D.Static)
        {
            if (dist > 1.2f)
                fixedVelocity = toPlayer.normalized * moveSpeed;
            else
                fixedVelocity = Vector2.zero;
        }
        else
        {
            fixedVelocity = Vector2.zero;
        }

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            fixedRotation = angle;
            fixedHasRotation = true;
            HandleFlip(toPlayer);
        }

        if (dist <= detectionRange && Time.time > lastShotTime + shootingInterval)
        {
            if (enableDebug) Debug.Log($"{gameObject.name} shooting at player (dist {dist:F2})");
            Shoot((toPlayer.sqrMagnitude > 0.0001f) ? toPlayer.normalized : Vector2.up);
            lastShotTime = Time.time;
        }
    }

    void HandleConvertedBlockBehavior()
    {
        // Turrets only shoot at OTHER LIVE ENEMIES, not the player
        var enemies = FindObjectsOfType<EnemyHealth>();
        Transform target = null;
        float closestDist = Mathf.Infinity;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (e.gameObject == gameObject) continue;  // Skip self
            if (e.isDead) continue;                     // Skip dead enemies
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < closestDist && d <= convertedDetectionRange)
            {
                closestDist = d;
                target = e.transform;
            }
        }

        if (target == null) return;

        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toTarget = ((Vector2)target.position - currentPos);

        if (Time.time > lastConvertedShotTime + shootingInterval)
        {
            if (enableDebug) Debug.Log($"{gameObject.name} (turret) shooting at {target.name} (dist {closestDist:F2})");
            Shoot((toTarget.sqrMagnitude > 0.0001f) ? toTarget.normalized : Vector2.up);
            lastConvertedShotTime = Time.time;
        }
    }

    void HandleCarried()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 aimDir = (mouseWorld - transform.position);
        if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.up;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
        float step = rotateSpeed * Time.deltaTime;
        if (rb != null) rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time > lastShotTime + 0.1f)
            {
                if (enableDebug) Debug.Log($"{gameObject.name} player fired carried shooter");
                Vector2 dir = aimDir.normalized;
                Shoot(dir);
                lastShotTime = Time.time;
            }
        }
    }

    void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null) return;

        GameObject p = Instantiate(projectilePrefab, transform.position + (Vector3)(direction * 0.3f), Quaternion.identity);
        if (p == null) return;

        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.velocity = direction * projectileSpeed;
        }

        var proj = p.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = projectileDamage;
            proj.owner = gameObject;
            proj.ignoreCarrierPlayer = IsCarried();
            proj.allowPlayerDamage = !IsCarried();
            proj.allowedTargetNames = (onlyTargetNames ? targetNameFilters : null);
            proj.allowedTargetTags = (onlyTargetTags ? targetTagFilters : null);
        }

        // play sound via AudioManager if configured
        if (AudioManager.Instance != null && shootSound != null)
            AudioManager.Instance.PlayOneShot(shootSound);

        // IMPORTANT: Ignore collision between projectile and owner (so it doesn't immediately hit itself)
        var projColls = p.GetComponentsInChildren<Collider2D>();
        var ownerColls = gameObject.GetComponentsInChildren<Collider2D>();
        foreach (var pc in projColls)
            foreach (var oc in ownerColls)
                if (pc != null && oc != null) Physics2D.IgnoreCollision(pc, oc, true);

        if (IsCarried())
        {
            GameObject playerObj = player != null ? player.gameObject : GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                var playerColls = playerObj.GetComponentsInChildren<Collider2D>();
                foreach (var pc in projColls)
                    foreach (var plc in playerColls)
                        if (pc != null && plc != null) Physics2D.IgnoreCollision(pc, plc, true);
            }
        }
    }

    void HandleFlip(Vector2 dir)
    {
        if (dir.x > 0.1f)
            sr.flipX = true;

        else if (dir.x < -0.1f)
            sr.flipX = false;
    }
}
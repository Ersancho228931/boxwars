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

    [Header("Pickup as weapon")]
    public bool canBePickedWhileAlive = true;
    public int allowedPickupHandIndex = 0;

    [Header("Converted block settings")]
    public bool shootWhenConverted = true; // стрел€ть, будучи стеной (isConvertedToBlock)
    public float convertedDetectionRange = 8f;

    [Header("Targeting filter (optional)")]
    public bool onlyTargetNames = false;    // если true Ч использовать targetNameFilters (по имени)
    public string[] targetNameFilters;      // список имЄн/подстрок целей

    public bool onlyTargetTags = false;     // если true Ч использовать targetTagFilters (по тегу)
    public string[] targetTagFilters;       // список тегов целей

    [Header("Debug")]
    public bool enableDebug = false;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private float lastShotTime = -999f;
    private Camera mainCam;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        mainCam = Camera.main;
        var p = GameObject.Find("Player");
        if (p != null) player = p.transform;

        if (enableDebug)
            Debug.Log($"ShooterController.Start on {gameObject.name} Ч projectilePrefab is {(projectilePrefab != null ? "assigned" : "NULL")}");
    }

    void Update()
    {
        bool isCarried = IsCarried();
        if (isCarried)
            HandleCarried();
        else
            HandleAI();
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
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist > 1.2f)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = toPlayer.normalized * moveSpeed;
        }
        else
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = Vector2.zero;
        }

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            float step = rotateSpeed * Time.deltaTime;
            if (rb != null) rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);
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
        var enemies = FindObjectsOfType<EnemyHealth>();
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (e.gameObject == gameObject) continue;
            if (e.isDead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = e.transform;
            }
        }

        if (best == null)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (best.position - transform.position);
        float distToTarget = toTarget.magnitude;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            float step = rotateSpeed * Time.deltaTime;
            if (rb != null) rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);
        }

        if (distToTarget <= convertedDetectionRange && Time.time > lastShotTime + shootingInterval)
        {
            if (enableDebug) Debug.Log($"{gameObject.name} (converted) shooting at enemy {best.name} (dist {distToTarget:F2})");
            Shoot((toTarget.sqrMagnitude > 0.0001f) ? toTarget.normalized : Vector2.up);
            lastShotTime = Time.time;
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
            proj.ignoreCarrierPlayer = IsCarried();      // если переноситс€ Ч не ранить носител€
            proj.allowPlayerDamage = !IsCarried();      // если переноситс€ Ч не бьЄм игрока; иначе можно бить игрока
            proj.allowedTargetNames = (onlyTargetNames ? targetNameFilters : null);
            proj.allowedTargetTags = (onlyTargetTags ? targetTagFilters : null);
        }

        // ≈сли шутер в руках Ч исключаем столкновени€ с игроком и с owner (чтобы снар€д не убивалс€ моментально)
        if (IsCarried())
        {
            var projColls = p.GetComponentsInChildren<Collider2D>();
            GameObject playerObj = player != null ? player.gameObject : GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                var playerColls = playerObj.GetComponentsInChildren<Collider2D>();
                foreach (var pc in projColls)
                    foreach (var plc in playerColls)
                        if (pc != null && plc != null) Physics2D.IgnoreCollision(pc, plc, true);
            }

            var ownerColls = gameObject.GetComponentsInChildren<Collider2D>();
            foreach (var pc in projColls)
                foreach (var oc in ownerColls)
                    if (pc != null && oc != null) Physics2D.IgnoreCollision(pc, oc, true);
        }
    }
}

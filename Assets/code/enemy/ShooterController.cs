using System.Linq;
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
        {
            HandleCarried();
        }
        else
        {
            HandleAI();
        }
    }

    // публичный флаг Ч удобно дл€ других компонентов (например projectile) узнать состо€ние
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
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist > 1.2f)
        {
            if (rb != null) rb.velocity = toPlayer.normalized * moveSpeed;
        }
        else
        {
            if (rb != null) rb.velocity = Vector2.zero;
        }

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            float step = rotateSpeed * Time.deltaTime;
            if (rb != null) rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);
        }

        if (dist <= detectionRange && Time.time > lastShotTime + shootingInterval)
        {
            if (enableDebug) Debug.Log($"ShooterController.HandleAI: {gameObject.name} shooting at player (dist {dist:F2})");
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
            if (rb != null) rb.velocity = Vector2.zero;
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
            if (enableDebug) Debug.Log($"ShooterController.Converted: {gameObject.name} shooting at enemy {best.name} (dist {distToTarget:F2})");
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
                if (enableDebug) Debug.Log($"ShooterController.Carried: {gameObject.name} player fired carried shooter");
                Vector2 dir = aimDir.normalized;
                Shoot(dir);
                lastShotTime = Time.time;
            }
        }
    }

    void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            if (enableDebug) Debug.LogWarning($"ShooterController.Shoot: projectilePrefab is NULL on {gameObject.name} Ч cannot shoot.");
            return;
        }

        GameObject p = Instantiate(projectilePrefab, (Vector3)direction * 0.3f + transform.position, Quaternion.identity);
        if (p == null)
        {
            if (enableDebug) Debug.LogError($"ShooterController.Shoot: failed to Instantiate projectilePrefab on {gameObject.name}");
            return;
        }

        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.velocity = direction * projectileSpeed;
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"ShooterController.Shoot: projectile has no Rigidbody2D (prefab: {projectilePrefab.name})");
        }

        var proj = p.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = projectileDamage;
            proj.owner = gameObject;
            proj.ignoreCarrierPlayer = IsCarried(); // если переноситс€ Ч не ранть носител€
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"ShooterController.Shoot: projectile prefab has no Projectile component (prefab: {projectilePrefab.name})");
        }

        if (enableDebug) Debug.Log($"ShooterController.Shoot: {gameObject.name} spawned projectile {p.name}");
    }
}

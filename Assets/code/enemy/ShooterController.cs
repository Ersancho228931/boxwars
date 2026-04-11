using System.Collections;
using System.Collections.Generic;
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

    bool IsCarried()
    {
        // heuristics: parented to player's hand (PlayerCarry) and rigidbody not simulated
        if (transform.parent == null) return false;
        if (rb == null) return false;
        if (rb.simulated) return false;
        return GetComponentInParent<PlayerCarry>() != null;
    }

    void HandleAI()
    {
        if (player == null) return;
        if (enemyHealth != null && enemyHealth.isDead)
        {
            // dead: stop
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;

        // move slowly toward player if far
        if (dist > 1.2f)
        {
            rb.velocity = toPlayer.normalized * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

        // rotate slowly toward player
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        float step = rotateSpeed * Time.deltaTime;
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);

        // shoot periodically if in range
        if (dist <= detectionRange && Time.time > lastShotTime + shootingInterval)
        {
            Shoot(toPlayer.normalized);
            lastShotTime = Time.time;
        }
    }

    void HandleCarried()
    {
        // allow player to aim with mouse and shoot
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDir = (mouseWorld - transform.position);
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;
        float step = rotateSpeed * Time.deltaTime;
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, angle, step);

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time > lastShotTime + 0.1f) // tiny cooldown to avoid instant multi-shot
            {
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
        }
    }
}

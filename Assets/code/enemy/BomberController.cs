using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class BomberController : MonoBehaviour
{
    public float speed = 6f;
    public float fuseTime = 3f;
    public float explosionRadius = 2.5f;
    public int explosionDamage = 50;
    public Sprite explosionSprite;
    public Sprite deadSprite;
    public bool convertToBlockAfter = false; // если true Ч конвертируем в блок после взрыва

    private Transform player;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private SpriteRenderer sr;
    private float spawnTime;
    private bool exploded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        sr = GetComponent<SpriteRenderer>();
        var p = GameObject.Find("Player");
        if (p != null) player = p.transform;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (exploded) return;

        if (player == null) return;

        // бежим к игроку
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * speed;

        if (Time.time >= spawnTime + fuseTime)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // нанос€щий урон јќ≈
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var c in hits)
        {
            if (c == null) continue;
            var ph = c.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(explosionDamage);
            }

            var eh = c.GetComponent<EnemyHealth>();
            if (eh != null && eh.gameObject != gameObject)
            {
                eh.TakeDamage(explosionDamage);
            }
        }

        // смена спрайта на взрывной
        if (explosionSprite != null && sr != null) sr.sprite = explosionSprite;

        // небольша€ задержка, затем помечаем как труп / dead sprite
        Invoke(nameof(FinishExplosion), 0.35f);
    }

    void FinishExplosion()
    {
        if (deadSprite != null && sr != null) sr.sprite = deadSprite;

        // останавливаем движение и помечаем как мЄртвого
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (enemyHealth != null)
        {
            enemyHealth.isDead = true;
            if (convertToBlockAfter) enemyHealth.ConvertToBlock();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

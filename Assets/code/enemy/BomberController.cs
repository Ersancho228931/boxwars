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

    [Header("Sound")]
    public AudioClip explosionSound;
    [Range(0f, 1f)]
    public float explosionVolume = 1f;

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
        if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = dir * speed;

        if (Time.time >= spawnTime + fuseTime)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // воспроизвести звук взрыва через AudioManager или напр€мую, если назначен
        if (AudioManager.Instance != null)
        {
            if (explosionSound != null)
                AudioManager.Instance.PlayOneShot(explosionSound, explosionVolume);
            else
                AudioManager.Instance.PlayOneShot(AudioManager.Instance.enemyBreak, explosionVolume);
        }
        else
        {
            if (explosionSound != null)
            {
                // если AudioManager отсутствует Ч попробовать проиграть локально
                var temp = gameObject.AddComponent<AudioSource>();
                temp.PlayOneShot(explosionSound, explosionVolume);
                Destroy(temp, explosionSound.length + 0.1f);
            }
        }

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

        // показываем взрывной спрайт 0.5 секунды, затем уничтожаем объект
        Invoke(nameof(DestroySelf), 0.5f);
    }

    void DestroySelf()
    {
        // при необходимости можно запустить дополнительные эффекты здесь
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

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
    public bool convertToBlockAfter = false; // ���� true � ������������ � ���� ����� ������

    [Header("Sound")]
    public AudioClip explosionSound;
    [Range(0f, 1f)]
    public float explosionVolume = 2.5f;

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

        // ����� � ������
        Vector2 dir = (player.position - transform.position).normalized;
        if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.velocity = dir * speed;

        if (Time.time >= spawnTime + fuseTime)
        {
            Explode();
        }
    }

    void Explode()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.bomberExplosion);
        if (exploded) return;
        exploded = true;

        // Проигрываем звук взрыва
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
                var temp = gameObject.AddComponent<AudioSource>();
                temp.PlayOneShot(explosionSound, explosionVolume);
                Destroy(temp, explosionSound.length + 0.1f);
            }
        }

        // Наносим урон объектам
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

        // Показать спрайт взрыва
        if (explosionSprite != null && sr != null) sr.sprite = explosionSprite;

        // Спрайт взрыва исчезает через 0.3 секунды, а объект удаляется через 0.5 секунды
        Invoke(nameof(DestroySelf), 0.5f);
    }

    void DestroySelf()
    {
        // Удаляем объект сразу
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

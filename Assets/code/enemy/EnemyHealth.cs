using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public Sprite deadSprite;

    private SpriteRenderer sr;
    private EnemyFollow enemyFollow;
    private Animator anim;
    private Rigidbody2D rb;

    public bool isDead = false;
    public bool isConvertedToBlock = false;

    public event Action OnDeath;

    private float spawnTime;

    [Header("Block settings")]
    public int blockMaxHealth = 50; // тип изменён на int, чтобы убрать ошибку преобразования типов

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        enemyFollow = GetComponent<EnemyFollow>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        spawnTime = Time.time;
    }

    void Update()
    {
        // ☀️ Die in day after 5 sec
        if (!isDead && DayNightManager.instance.IsDay())
        {
            if (Time.time > spawnTime + 5f)
            {
                Die();
                // НЕ конвертируем в блок сразу — даём шанс игроку поднять тело.
                // Если нужно автоматически превратить через время, вызывайте ConvertToBlock() отдельно.
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        OnDeath?.Invoke(); // remove from spawner count

        if (deadSprite != null && sr != null)
            sr.sprite = deadSprite;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            // corpse is movable by default (so player can pick it up); keep as Dynamic
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 50f;
            rb.drag = 5f;
        }

        if (enemyFollow != null)
            enemyFollow.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = false;

        if (anim != null)
            Destroy(anim);

        // НЕ ставим isConvertedToBlock и НЕ меняем слой здесь.
        // ConvertToBlock() делает это явно.
    }

    // Вызывайте этот метод, когда тело должно стать стеной (PlayerConvert или таймер)
    public void ConvertToBlock()
    {
        if (isConvertedToBlock) return;

        isConvertedToBlock = true;

        // Если блока нет — создаём и инициализируем HP из поля enemy
        if (GetComponent<Block>() == null)
        {
            var b = gameObject.AddComponent<Block>();
            b.maxHealth = blockMaxHealth;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            gameObject.layer = wallLayer;
        }
        else
        {
            Debug.LogWarning("EnemyHealth.ConvertToBlock: layer 'Wall' not found. Создайте слой 'Wall' в Tags and Layers.");
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
            rb.velocity = Vector2.zero;
        }
    }
}
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
    public int blockMaxHealth = 50; // int — согласовано с Block

    [Header("Boss settings (optional)")]
    public bool isBoss = false;
    public string bossName = "THEBOSS";

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        enemyFollow = GetComponent<EnemyFollow>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        spawnTime = Time.time;

        // Если помечен как босс — показать баннер в UI
        if (isBoss && UIManager.Instance != null)
        {
            UIManager.Instance.ShowBoss(bossName);
        }
    }

    void Update()
    {
        // ☀️ Die in day after 5 sec
        if (!isDead && DayNightManager.instance != null && DayNightManager.instance.IsDay())
        {
            if (Time.time > spawnTime + 5f)
            {
                Die();
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

        // Если это Shooter — сразу конвертируем в блок (у вас уже реализовано)
        var shooter = GetComponent<ShooterController>();
        if (shooter != null)
        {
            ConvertToBlock();
        }

        // Если это босс — показать экран победы
        if (isBoss)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWin();
            else
                Debug.LogWarning("EnemyHealth.Die: boss died but UIManager.Instance == null");
        }
        else
        {
            // Вызов через BossController остаётся работоспособным, если он есть
            var boss = GetComponent<BossController>();
            if (boss != null && UIManager.Instance != null)
                UIManager.Instance.ShowWin();
        }
    }

    public void ConvertToBlock()
    {
        if (isConvertedToBlock) return;

        isConvertedToBlock = true;

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
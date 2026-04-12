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
    public int blockMaxHealth = 50;

    public bool isBoss = false;
    public string bossName = "THEBOSS";

    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.12f;

    private Color originalColor;
    private Coroutine flashRoutine;

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        enemyFollow = GetComponent<EnemyFollow>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originalColor = sr != null ? sr.color : Color.white;
        spawnTime = Time.time;

        if (isBoss && UIManager.Instance != null)
        {
            UIManager.Instance.ShowBoss(bossName);
            UIManager.Instance.SetBossMaxHealth(currentHealth);
            UIManager.Instance.UpdateBossHealth(currentHealth);
        }
    }

    void Update()
    {
        if (!isDead && DayNightManager.instance != null && DayNightManager.instance.IsDay())
        {
            if (Time.time > spawnTime + 5f)
                Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashDamage());

        if (isBoss && UIManager.Instance != null)
            UIManager.Instance.UpdateBossHealth(Mathf.Max(0, currentHealth));

        if (currentHealth <= 0)
            Die();
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (sr == null) yield break;
        sr.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        float t = 0f;
        float fade = Mathf.Max(0.05f, damageFlashDuration);
        Color from = sr.color;
        while (t < fade)
        {
            t += Time.deltaTime;
            sr.color = Color.Lerp(from, originalColor, t / fade);
            yield return null;
        }
        sr.color = originalColor;
        flashRoutine = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();  // IMPORTANT: Call this BEFORE changing sprite so spawner can track death

        if (deadSprite != null && sr != null)
            sr.sprite = deadSprite;

        // === FIXED: Make dead body truly static and prevent disappearing ===
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.interpolation = RigidbodyInterpolation2D.None; // prevents physics glitches
        }

        if (enemyFollow != null)
            enemyFollow.enabled = false;

        if (anim != null)
            Destroy(anim);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;           // solid collider
            col.enabled = true;              // ensure collider stays on
        }

        // Handle shooter → block conversion
        var shooter = GetComponent<ShooterController>();
        if (shooter != null)
            ConvertToBlock();

        if (isBoss && UIManager.Instance != null)
            UIManager.Instance.ShowWin();
        
        // IMPORTANT: Don't destroy the corpse automatically - let EnemySpawner manage corpse lifetime
        // The corpse will be cleaned up based on maxDeadBodies limit and delay
    }

    public void ConvertToBlock()
    {
        if (isConvertedToBlock) return;
        isConvertedToBlock = true;

        Block blockComp = GetComponent<Block>();
        if (blockComp == null)
        {
            blockComp = gameObject.AddComponent<Block>();
            blockComp.Initialize(blockMaxHealth);  // Properly initialize health
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        gameObject.layer = wallLayer >= 0 ? wallLayer : 0;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;  // Kinematic allows rotation, Static doesn't!
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            col.enabled = true;
        }
    }

    public int GetCurrentHealth() => currentHealth;
}
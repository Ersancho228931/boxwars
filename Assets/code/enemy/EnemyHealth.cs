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
    public int blockMaxHealth = 50;

    [Header("Boss settings (optional)")]
    public bool isBoss = false;
    public string bossName = "THEBOSS";

    [Header("Damage flash")]
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
            {
                Die();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // flash color
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashDamage());

        if (isBoss && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(Mathf.Max(0, currentHealth));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
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

    void Die()
    {
        if (isDead) return;

        isDead = true;

        OnDeath?.Invoke();

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

        var shooter = GetComponent<ShooterController>();
        if (shooter != null)
        {
            ConvertToBlock();
        }

        if (isBoss)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowWin();
            else
                Debug.LogWarning("EnemyHealth.Die: boss died but UIManager.Instance == null");
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

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
            rb.velocity = Vector2.zero;
        }
    }

    // helper: expose current for UI/inspector if needed
    public int GetCurrentHealth() => currentHealth;
}
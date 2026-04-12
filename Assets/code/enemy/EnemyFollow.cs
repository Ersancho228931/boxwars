using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public string playerName = "Player";
    private Transform player;
    private SpriteRenderer sr;

    public float speed = 3f;
    public int damage = 10;
    public float attackCooldown = 2f;

    private Rigidbody2D rb;
    private float lastAttackTime;
    private EnemyHealth enemyHealth;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();

        anim = GetComponent<Animator>();

        GameObject p = GameObject.Find(playerName);
        if (p != null)
            player = p.transform;

        // 📈 Scale with days
        int day = DayNightManager.instance.GetDay();
        damage += day * 2;
        speed += day * 0.2f;

        sr = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if (player == null || enemyHealth != null && enemyHealth.isDead)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * speed;
        HandleFlip(direction);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (enemyHealth != null && enemyHealth.isDead) return;

        if (Time.time < lastAttackTime + attackCooldown) return;

        // Пропускаем живых врагов - они не должны атаковать друг друга
        EnemyHealth targetEnemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
        if (targetEnemyHealth != null && !targetEnemyHealth.isDead) return;

        // 🧍 Player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                lastAttackTime = Time.time;
            }
            return;
        }

        // 🧱 Блоки и трупы врагов
        // Сначала проверяем Block (прямой удар по трупу)
        Block block = collision.gameObject.GetComponent<Block>();
        if (block != null)
        {
            block.TakeDamage(damage);
            lastAttackTime = Time.time;
            if (anim != null) anim.SetBool("brk", true);
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.enemyBreak);
            return;
        }

        // Проверяем мертвого врага (у которого есть EnemyHealth)
        if (targetEnemyHealth != null && targetEnemyHealth.isDead)
        {
            // Это мертвый враг - ломаем его
            targetEnemyHealth.TakeDamage(damage);
            lastAttackTime = Time.time;
            if (anim != null) anim.SetBool("brk", true);
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.enemyBreak);
            return;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (anim != null) anim.SetBool("brk", false);
        }
    }

    void HandleFlip(Vector2 dir)
    {
        if (dir.x > 0.1f)
            sr.flipX = true;

        else if (dir.x < -0.1f)
            sr.flipX = false;
    }
}
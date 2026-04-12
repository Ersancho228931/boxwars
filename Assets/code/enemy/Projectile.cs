using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 5f;
    public GameObject owner;
    public bool ignoreCarrierPlayer = false;
    public bool allowPlayerDamage = true;
    public string[] allowedTargetNames;
    public string[] allowedTargetTags;

    [Header("Debug")]
    public bool enableDebug = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        HandleHit(col?.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision?.gameObject);
    }

    void HandleHit(GameObject hit)
    {
        if (hit == null) return;

        // FIRST: Check if this is our owner and ignore it
        if (owner != null)
        {
            if (hit == owner || hit.transform.IsChildOf(owner.transform))
                return;
        }

        // Allow carried shooter (even when converted) to damage enemies
        bool isCarriedShooter = owner != null && owner.GetComponent<ShooterController>() != null
                                && owner.GetComponent<ShooterController>().IsCarried();

        // Enemy projectile that can break walls
        bool isEnemyProjectile = owner != null && owner.GetComponent<EnemyHealth>() != null
                                 && !isCarriedShooter;

        // Break walls with enemy shots
        if (isEnemyProjectile)
        {
            Block block = hit.GetComponent<Block>() ?? hit.GetComponentInParent<Block>();
            if (block != null)
            {
                block.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        var eh = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
        if (eh != null)
        {
            // Allow carried shooter to damage other enemies (important fix)
            if (isCarriedShooter)
            {
                eh.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            // Friendly fire prevention: живые враги не атакуют живых врагов
            // НО конвертированные в блоки турели МОГУТ атаковать врагов
            var ownerEh = owner != null ? owner.GetComponent<EnemyHealth>() : null;
            if (ownerEh != null && !ownerEh.isDead)  // Только живые враги не атакуют друг друга
            {
                Destroy(gameObject);
                return;
            }

            // Если это турель (мертвая и конвертирована) или другой projectile - наносим урон
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Player damage
        if (hit.CompareTag("Player"))
        {
            if (!allowPlayerDamage) return;
            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        Destroy(gameObject);
    }
}
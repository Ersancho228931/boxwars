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

    bool NameMatchesFilter(string objName)
    {
        if (allowedTargetNames == null || allowedTargetNames.Length == 0) return true;
        if (string.IsNullOrEmpty(objName)) return false;

        foreach (var f in allowedTargetNames)
        {
            if (string.IsNullOrEmpty(f)) continue;
            if (objName == f) return true;
            if (objName.Contains(f)) return true;
        }
        return false;
    }

    bool TagMatchesFilter(string tag)
    {
        if (allowedTargetTags == null || allowedTargetTags.Length == 0) return true;
        if (string.IsNullOrEmpty(tag)) return false;
        foreach (var t in allowedTargetTags)
        {
            if (string.IsNullOrEmpty(t)) continue;
            if (tag == t) return true;
        }
        return false;
    }

    bool HitIsCarrierOfOwner(GameObject hit)
    {
        if (owner == null || hit == null) return false;
        return owner.transform.IsChildOf(hit.transform);
    }

    void HandleHit(GameObject hit)
    {
        if (hit == null) return;

        if (enableDebug) Debug.Log($"Projectile.HandleHit: {name} hit {hit.name}");

        if (owner != null)
        {
            if (hit == owner)
            {
                if (enableDebug) Debug.Log("Projectile: hit owner -> ignore");
                return;
            }

            if (hit.transform.IsChildOf(owner.transform))
            {
                if (enableDebug) Debug.Log("Projectile: hit child of owner -> ignore");
                return;
            }

            if (HitIsCarrierOfOwner(hit))
            {
                if (enableDebug) Debug.Log("Projectile: hit is carrier of owner -> ignore");
                return;
            }
        }

        // Enemy: search in parents too
        var eh = hit.GetComponent<EnemyHealth>();
        if (eh == null)
            eh = hit.GetComponentInParent<EnemyHealth>();

        if (eh != null)
        {
            // if projectile was fired by an enemy (owner has EnemyHealth) and owner is NOT converted/block nor carried shooter => block friendly fire
            var ownerEh = owner != null ? owner.GetComponent<EnemyHealth>() : null;
            var ownerShooter = owner != null ? owner.GetComponent<ShooterController>() : null;
            bool ownerIsEnemy = ownerEh != null;

            bool ownerConvertedOrCarriedShooter = false;
            if (ownerEh != null && ownerEh.isConvertedToBlock) ownerConvertedOrCarriedShooter = true;
            if (ownerShooter != null && ownerShooter.IsCarried()) ownerConvertedOrCarriedShooter = true;

            if (ownerIsEnemy && !ownerConvertedOrCarriedShooter)
            {
                // enemy fired and not converted/carried => do not damage other enemies
                if (enableDebug) Debug.Log("Projectile: friendly fire prevented -> destroy projectile");
                Destroy(gameObject);
                return;
            }

            // tag/name filters apply to the root object of the EnemyHealth
            if (allowedTargetTags != null && allowedTargetTags.Length > 0)
            {
                if (!TagMatchesFilter(eh.gameObject.tag))
                {
                    if (enableDebug) Debug.Log($"Projectile: enemy {eh.gameObject.name} tag '{eh.gameObject.tag}' not allowed -> destroy projectile");
                    Destroy(gameObject);
                    return;
                }
            }

            if (allowedTargetNames != null && allowedTargetNames.Length > 0)
            {
                if (!NameMatchesFilter(eh.gameObject.name))
                {
                    if (enableDebug) Debug.Log($"Projectile: enemy {eh.gameObject.name} name not allowed -> destroy projectile");
                    Destroy(gameObject);
                    return;
                }
            }

            if (enableDebug) Debug.Log($"Projectile: dealing {damage} to enemy {eh.gameObject.name}");
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Player
        if (hit.CompareTag("Player"))
        {
            if (!allowPlayerDamage)
            {
                if (enableDebug) Debug.Log("Projectile: player hit but allowPlayerDamage == false -> ignore");
                return;
            }

            if (ignoreCarrierPlayer && HitIsCarrierOfOwner(hit))
            {
                if (enableDebug) Debug.Log("Projectile: hit is carrier of owner and ignoreCarrierPlayer == true -> ignore");
                return;
            }

            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                if (enableDebug) Debug.Log($"Projectile: dealing {damage} to player {hit.name}");
                ph.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        if (enableDebug) Debug.Log("Projectile: hit environment -> destroy");
        Destroy(gameObject);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 5f;
    public GameObject owner;

    // ƒополнительные настройки, задаютс€ ShooterController
    public bool ignoreCarrierPlayer = false; // не бить игрока-носител€
    public bool allowPlayerDamage = true;    // можно ли бить игрока вообще
    public string[] allowedTargetNames;      // если непустой Ч бьЄм “ќЋ№ ќ по цел€м с этими именами (или содержащим)
    public string[] allowedTargetTags;       // если непустой Ч бьЄм “ќЋ№ ќ по цел€м с этими тегами

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

    // ѕровер€ем, €вл€етс€ ли hit объектом-носителем владельца (owner находитс€ внутри иерархии hit)
    bool HitIsCarrierOfOwner(GameObject hit)
    {
        if (owner == null || hit == null) return false;
        // если owner Ч ребЄнок hit (owner находитс€ в иерархии hit), то hit Ч носитель/предок
        return owner.transform.IsChildOf(hit.transform);
    }

    void HandleHit(GameObject hit)
    {
        if (hit == null) return;

        if (enableDebug) Debug.Log($"Projectile.HandleHit: {name} hit {hit.name}");

        // »гнорируем пр€мое попадание по владельцу или по дет€м владельца
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

            // если владелец Ч ребЄнок hit (hit Ч носитель), также игнорируем (чтобы не ранить носител€)
            if (HitIsCarrierOfOwner(hit))
            {
                if (enableDebug) Debug.Log("Projectile: hit is carrier of owner -> ignore");
                return;
            }
        }

        // Enemy Ч основной случай: ищем компонент в самом объекте или в родител€х
        var eh = hit.GetComponent<EnemyHealth>();
        if (eh == null)
            eh = hit.GetComponentInParent<EnemyHealth>();

        if (eh != null)
        {
            // если задан фильтр по тегам Ч провер€ем тег корневого объекта Enemy
            if (allowedTargetTags != null && allowedTargetTags.Length > 0)
            {
                if (!TagMatchesFilter(eh.gameObject.tag))
                {
                    if (enableDebug) Debug.Log($"Projectile: enemy {eh.gameObject.name} tag '{eh.gameObject.tag}' not allowed -> destroy projectile");
                    Destroy(gameObject);
                    return;
                }
            }

            // если задан фильтр по именам Ч провер€ем им€ объекта с EnemyHealth
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
            // ≈сли снар€д не должен бить игрока Ч игнорируем попадание
            if (!allowPlayerDamage)
            {
                if (enableDebug) Debug.Log("Projectile: player hit but allowPlayerDamage == false -> ignore");
                return;
            }

            // ≈сли владелец переноситс€ игроком и флаг ignoreCarrierPlayer установлен Ч игнорируем попадание по носителю
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

        // иначе Ч стук по окружению -> уничтожить
        if (enableDebug) Debug.Log("Projectile: hit environment -> destroy");
        Destroy(gameObject);
    }
}

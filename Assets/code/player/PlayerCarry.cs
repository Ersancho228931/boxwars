using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    public Transform[] hands; // 4 hand slots
    public float pickupRange = 2f;
    public LayerMask enemyLayer;
    public KeyCode pickupKey = KeyCode.Space;
    public string carriedLayerName = "Carried";

    [Header("Carried attack (use dead corpse as weapon)")]
    public int carriedAttackDamage = 25;
    public float carriedAttackRadius = 1.5f;
    public float carriedAttackCooldown = 1f;
    public AudioClip carriedAttackSound;
    public float carriedAttackSoundVolume = 1f;

    private GameObject[] carriedObjects;
    private int carriedLayer;
    private float lastCarriedAttackTime = -999f;

    void Start()
    {
        if (hands == null || hands.Length == 0)
            Debug.LogError("PlayerCarry: hands не назначены в инспекторе!");

        carriedObjects = new GameObject[hands.Length];

        // Safe layer assignment
        carriedLayer = LayerMask.NameToLayer(carriedLayerName);
        if (carriedLayer < 0)
        {
            carriedLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (carriedLayer < 0) carriedLayer = 0;
        }

        if (enemyLayer.value == 0)
            Debug.LogWarning("PlayerCarry: enemyLayer равен 0. Используется авто-поиск.");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryUseCarriedAsWeapon();

        if (Input.GetKeyDown(pickupKey))
            TryPickupOrDrop();
    }

    void TryUseCarriedAsWeapon()
    {
        if (Time.time < lastCarriedAttackTime + carriedAttackCooldown) return;

        for (int i = 0; i < carriedObjects.Length; i++)
        {
            var obj = carriedObjects[i];
            if (obj == null) continue;

            var eh = obj.GetComponent<EnemyHealth>();
            var shooter = obj.GetComponent<ShooterController>();
            var block = obj.GetComponent<Block>();

            if (eh != null && eh.isDead && !eh.isConvertedToBlock && shooter == null)
            {
                PerformCarriedAttack();
                lastCarriedAttackTime = Time.time;
                return;
            }

            if (block != null) continue;
        }
    }

    void PerformCarriedAttack()
    {
        if (AudioManager.Instance != null && carriedAttackSound != null)
        {
            AudioManager.Instance.PlayOneShot(carriedAttackSound, carriedAttackSoundVolume);
        }
        else if (carriedAttackSound != null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.PlayOneShot(carriedAttackSound, carriedAttackSoundVolume);
            Destroy(src, carriedAttackSound.length + 0.1f);
        }

        Collider2D[] hits;
        if (enemyLayer.value != 0)
            hits = Physics2D.OverlapCircleAll(transform.position, carriedAttackRadius, enemyLayer.value);
        else
            hits = Physics2D.OverlapCircleAll(transform.position, carriedAttackRadius);

        int hitCount = 0;
        foreach (var c in hits)
        {
            if (c == null) continue;

            var target = c.GetComponent<EnemyHealth>() ?? c.GetComponentInParent<EnemyHealth>();
            if (target == null) continue;
            if (target.isDead) continue;
            if (target.gameObject == this.gameObject) continue;

            target.TakeDamage(carriedAttackDamage);
            hitCount++;
        }

        if (hitCount > 0)
            Debug.Log($"PlayerCarry: carried attack hit {hitCount} targets for {carriedAttackDamage} damage.");
    }

    void TryPickupOrDrop()
    {
        if (HasAnyCarried())
        {
            DropLast();
            return;
        }

        Collider2D[] hits;
        if (enemyLayer.value != 0)
            hits = Physics2D.OverlapCircleAll(transform.position, pickupRange, enemyLayer.value);
        else
            hits = Physics2D.OverlapCircleAll(transform.position, pickupRange);

        if (hits.Length == 0)
        {
            Debug.Log("PlayerCarry: нет объектов в радиусе.");
            return;
        }

        GameObject best = null;
        float bestDist = Mathf.Infinity;

        foreach (var c in hits)
        {
            if (c == null) continue;
            var obj = c.gameObject;
            if (obj == null) continue;
            if (obj.layer == carriedLayer) continue;
            if (IsAlreadyCarried(obj)) continue;
            if (obj.GetComponent<Collider2D>() == null || obj.GetComponent<Rigidbody2D>() == null) continue;

            var block = obj.GetComponent<Block>();
            var enemy = obj.GetComponent<EnemyHealth>();
            var shooter = obj.GetComponent<ShooterController>();

            if (enemy != null && !enemy.isDead)
            {
                if (shooter == null || !shooter.canBePickedWhileAlive)
                    continue;
            }

            float d = Vector2.Distance(transform.position, obj.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = obj;
            }
        }

        if (best == null)
        {
            Debug.Log("PlayerCarry: подходящих объектов не найдено.");
            return;
        }

        var bestBlock = best.GetComponent<Block>();
        var bestEnemy = best.GetComponent<EnemyHealth>();
        var bestShooter = best.GetComponent<ShooterController>();

        int handIndex = -1;

        // Live Shooter pickup
        if (bestEnemy != null && !bestEnemy.isDead && bestShooter != null && bestShooter.canBePickedWhileAlive)
        {
            int desired = bestShooter.allowedPickupHandIndex;
            if (desired < 0 || desired >= hands.Length) return;
            if (carriedObjects[desired] != null)
            {
                Debug.Log("PlayerCarry: целевая рука занята.");
                return;
            }
            handIndex = desired;
            PickupLiveShooter(best, handIndex);
            return;
        }

        handIndex = GetClosestFreeHand(best.transform.position);
        if (handIndex == -1)
        {
            Debug.Log("PlayerCarry: нет свободных рук.");
            return;
        }

        if (bestBlock != null || (bestEnemy != null && bestEnemy.isConvertedToBlock))
            PickupBlock(best, handIndex);
        else
            Pickup(best, handIndex);
    }

    bool HasAnyCarried()
    {
        for (int i = 0; i < carriedObjects.Length; i++)
            if (carriedObjects[i] != null) return true;
        return false;
    }

    bool IsAlreadyCarried(GameObject obj)
    {
        for (int i = 0; i < carriedObjects.Length; i++)
            if (carriedObjects[i] == obj) return true;

        Transform p = obj.transform.parent;
        if (p != null)
        {
            foreach (var h in hands)
                if (p == h) return true;
        }
        return false;
    }

    int GetClosestFreeHand(Vector2 targetPos)
    {
        float bestDist = Mathf.Infinity;
        int bestIndex = -1;
        for (int i = 0; i < hands.Length; i++)
        {
            if (carriedObjects[i] != null) continue;
            float dist = Vector2.Distance(hands[i].position, targetPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    void SetParentPreserveWorldScale(Transform child, Transform newParent)
    {
        Vector3 worldScale = child.lossyScale;
        child.SetParent(newParent, worldPositionStays: false);
        Vector3 parentScale = newParent != null ? newParent.lossyScale : Vector3.one;
        float px = parentScale.x == 0 ? 1f : parentScale.x;
        float py = parentScale.y == 0 ? 1f : parentScale.y;
        float pz = parentScale.z == 0 ? 1f : parentScale.z;
        child.localScale = new Vector3(worldScale.x / px, worldScale.y / py, worldScale.z / pz);
    }

    void PickupLiveShooter(GameObject obj, int handIndex)
    {
        carriedObjects[handIndex] = obj;
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SetParentPreserveWorldScale(obj.transform, hands[handIndex]);
        obj.transform.localPosition = Vector3.zero;
        obj.layer = carriedLayer;

        var ef = obj.GetComponent<EnemyFollow>();
        if (ef != null) ef.enabled = false;

        Debug.Log($"PlayerCarry: поднял живого shooter {obj.name} в руку {handIndex}");
    }

    void Pickup(GameObject obj, int handIndex)
    {
        var enemy = obj.GetComponent<EnemyHealth>();
        if (enemy == null || !enemy.isDead)
        {
            Debug.LogWarning($"PlayerCarry.Pickup: нельзя поднять {obj.name} (не труп).");
            return;
        }

        carriedObjects[handIndex] = obj;
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SetParentPreserveWorldScale(obj.transform, hands[handIndex]);
        obj.transform.localPosition = Vector3.zero;
        obj.layer = carriedLayer;

        Debug.Log($"PlayerCarry: поднял труп {obj.name} в руку {handIndex}");
    }

    void PickupBlock(GameObject obj, int handIndex)
    {
        var enemy = obj.GetComponent<EnemyHealth>();
        var block = obj.GetComponent<Block>();

        if (enemy != null && !enemy.isDead && (obj.GetComponent<ShooterController>() == null || !obj.GetComponent<ShooterController>().canBePickedWhileAlive))
        {
            Debug.LogWarning($"PlayerCarry.PickupBlock: {obj.name} живой — отказ.");
            return;
        }

        carriedObjects[handIndex] = obj;
        if (block != null) block.enabled = false;
        if (enemy != null) enemy.isConvertedToBlock = false;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = false;
            rb.velocity = Vector2.zero;
        }
        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SetParentPreserveWorldScale(obj.transform, hands[handIndex]);
        obj.transform.localPosition = Vector3.zero;
        obj.layer = carriedLayer;

        Debug.Log($"PlayerCarry: подобрал стену {obj.name} в руку {handIndex}");
    }

    void DropLast()
    {
        for (int i = hands.Length - 1; i >= 0; i--)
        {
            if (carriedObjects[i] != null)
            {
                Drop(i);
                return;
            }
        }
        Debug.Log("PlayerCarry: нечего бросить.");
    }

    void Drop(int index)
    {
        GameObject obj = carriedObjects[index];
        if (obj == null) return;

        Vector2 handPos = hands[index].position;
        Vector2 dir = (handPos - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        Vector2 dropPos = handPos + dir * 0.4f;

        // Swap with wall if dropping on wall
        int wallLayerId = LayerMask.NameToLayer("Wall");
        GameObject swappedIn = null;
        if (wallLayerId >= 0)
        {
            Collider2D existing = Physics2D.OverlapPoint(dropPos, 1 << wallLayerId);
            if (existing != null && existing.gameObject != obj)
            {
                swappedIn = existing.gameObject;
                var rbEx = swappedIn.GetComponent<Rigidbody2D>();
                if (rbEx != null)
                {
                    rbEx.simulated = false;
                    rbEx.velocity = Vector2.zero;
                }
                var colEx = swappedIn.GetComponent<Collider2D>();
                if (colEx != null) colEx.enabled = false;

                swappedIn.transform.SetParent(hands[index]);
                swappedIn.transform.localPosition = Vector3.zero;
                swappedIn.layer = carriedLayer;
            }
        }

        // Drop current object
        obj.transform.SetParent(null);
        obj.transform.position = dropPos;

        // SAFE layer assignment
        int enemyLayerId = LayerMask.NameToLayer("Enemy");
        obj.layer = enemyLayerId >= 0 ? enemyLayerId : 0;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // If it's a block / converted wall
        var block = obj.GetComponent<Block>();
        var eh = obj.GetComponent<EnemyHealth>();
        if (block != null || (eh != null && eh.isConvertedToBlock))
        {
            if (block != null) block.enabled = true;
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;
            obj.layer = wallLayerId >= 0 ? wallLayerId : 0;
        }

        carriedObjects[index] = swappedIn;

        if (swappedIn == null)
            Debug.Log($"Dropped {obj.name}");
        else
            Debug.Log($"Swapped {obj.name} with {swappedIn.name}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, carriedAttackRadius);
    }
}
using System.Collections.Generic;
using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    public Transform[] hands; // 4 hand slots
    public float pickupRange = 2f;
    public LayerMask enemyLayer; // optional: leave empty to auto-detect
    public KeyCode pickupKey = KeyCode.Space;
    public string carriedLayerName = "Carried"; // создайте этот слой в Project Settings или будет использован Ignore Raycast

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

        carriedLayer = LayerMask.NameToLayer(carriedLayerName);
        if (carriedLayer < 0)
        {
            carriedLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (carriedLayer < 0) carriedLayer = 0;
        }

        if (enemyLayer == 0)
            Debug.LogWarning("PlayerCarry: enemyLayer равен 0 (ничего). Если хочешь — назначь маску слоёв врагов в инспекторе, иначе используется авто-поиск по компонентам.");
    }

    void Update()
    {
        // Использование переносимого трупа — левая кнопка мыши
        if (Input.GetMouseButtonDown(0))
        {
            TryUseCarriedAsWeapon();
        }

        // Подбор/бросок — отдельной клавишей
        if (Input.GetKeyDown(pickupKey))
            TryPickupOrDrop();
    }

    void TryUseCarriedAsWeapon()
    {
        // перезарядка
        if (Time.time < lastCarriedAttackTime + carriedAttackCooldown) return;

        // ищем в руках первый подходящий объект: EnemyHealth, isDead == true, не converted в Block, и не Shooter
        for (int i = 0; i < carriedObjects.Length; i++)
        {
            var obj = carriedObjects[i];
            if (obj == null) continue;

            var eh = obj.GetComponent<EnemyHealth>();
            var shooter = obj.GetComponent<ShooterController>();
            var block = obj.GetComponent<Block>();

            // только мёртвый обычный враг (труп), не конвертированный в блок и не shooter
            if (eh != null && eh.isDead && !eh.isConvertedToBlock && shooter == null)
            {
                PerformCarriedAttack();
                lastCarriedAttackTime = Time.time;
                return;
            }

            // если держим стену/конвертированный труп — не используем как оружие
            if (block != null) continue;
        }
    }

    void PerformCarriedAttack()
    {
        // воспроизводим звук атаки
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

        // ищем врагов в радиусе вокруг игрока
        Collider2D[] hits;
        if (enemyLayer.value != 0)
            hits = Physics2D.OverlapCircleAll(transform.position, carriedAttackRadius, enemyLayer.value);
        else
            hits = Physics2D.OverlapCircleAll(transform.position, carriedAttackRadius);

        int hitCount = 0;
        foreach (var c in hits)
        {
            if (c == null) continue;

            // ищем EnemyHealth в самой коллайдерной иерархии
            var target = c.GetComponent<EnemyHealth>();
            if (target == null)
                target = c.GetComponentInParent<EnemyHealth>();

            if (target == null) continue;
            if (target.isDead) continue; // не наносим урон мёртвым
            if (target.gameObject == this.gameObject) continue;

            // предотвращаем friendly fire: если атакует enemy (редкий кейс) - уже контролируется в Projectile/других местах
            target.TakeDamage(carriedAttackDamage);
            hitCount++;
        }

        if (hitCount > 0)
            Debug.Log($"PlayerCarry: carried attack hit {hitCount} targets for {carriedAttackDamage} damage.");
    }

    // --- Pickup / Drop logic (full implementation) ---
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

        Debug.Log($"PlayerCarry: найдено {hits.Length} коллайдеров в радиусе {pickupRange}.");

        if (hits.Length == 0)
        {
            Debug.Log("PlayerCarry: нет подходящих объектов в радиусе. Подойдите ближе или проверьте enemyLayer/слои.");
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

            // нельзя поднимать живого врага, кроме shooter с разрешением
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
            Debug.Log("PlayerCarry: подходящих объектов не найдено (фильтры).");
            return;
        }

        var bestBlock = best.GetComponent<Block>();
        var bestEnemy = best.GetComponent<EnemyHealth>();
        var bestShooter = best.GetComponent<ShooterController>();

        int handIndex = -1;

        // если это живой shooter — используем его allowedPickupHandIndex
        if (bestEnemy != null && !bestEnemy.isDead && bestShooter != null && bestShooter.canBePickedWhileAlive)
        {
            int desired = bestShooter.allowedPickupHandIndex;
            if (desired < 0 || desired >= hands.Length)
            {
                Debug.LogWarning("PlayerCarry: shooter.allowedPickupHandIndex вне диапазона.");
                return;
            }
            if (carriedObjects[desired] == null)
                handIndex = desired;
            else
            {
                Debug.Log("PlayerCarry: целевая рука для поднятия shooter занята.");
                return;
            }

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

    // helper: сохраняем мировой масштаб объекта при смене родителя
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
        var shooter = obj.GetComponent<ShooterController>();
        var enemy = obj.GetComponent<EnemyHealth>();

        carriedObjects[handIndex] = obj;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; // важно для ShooterController.IsCarried()
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

        if (block != null)
            block.enabled = false;

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
        if (obj == null)
        {
            carriedObjects[index] = null;
            return;
        }

        Vector2 handPos = hands[index].position;
        Vector2 dir = (handPos - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        Vector2 dropPos = handPos + dir * 0.2f;

        var playerCol = GetComponent<Collider2D>();
        if (playerCol != null && playerCol.OverlapPoint(dropPos))
            dropPos = handPos + dir * 0.8f;

        int wallLayer = LayerMask.NameToLayer("Wall");
        GameObject swappedIn = null;

        if (wallLayer >= 0)
        {
            Collider2D existing = Physics2D.OverlapPoint(dropPos, 1 << wallLayer);
            if (existing != null && existing.gameObject != obj)
            {
                GameObject existingObj = existing.gameObject;

                var bc = existingObj.GetComponent<Block>();
                if (bc != null)
                {
                    bc.enabled = false;
                }

                var eh = existingObj.GetComponent<EnemyHealth>();
                if (eh != null) eh.isConvertedToBlock = false;

                var rbExisting = existingObj.GetComponent<Rigidbody2D>();
                if (rbExisting != null)
                {
                    rbExisting.bodyType = RigidbodyType2D.Dynamic;
                    rbExisting.simulated = false;
                    rbExisting.velocity = Vector2.zero;
                }

                var colExisting = existingObj.GetComponent<Collider2D>();
                if (colExisting != null) colExisting.enabled = false;

                existingObj.transform.SetParent(hands[index], worldPositionStays: true);
                existingObj.transform.position = hands[index].position;

                existingObj.layer = carriedLayer;
                swappedIn = existingObj;

                Debug.Log($"PlayerCarry: поднял существующую стену {existingObj.name} в руку {index} (замена).");
            }
        }

        obj.transform.SetParent(null);
        obj.transform.position = dropPos;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        var col = obj.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        var existingBlock = obj.GetComponent<Block>();
        var eh2 = obj.GetComponent<EnemyHealth>();
        if (existingBlock != null)
        {
            existingBlock.enabled = true;
        }
        else
        {
            var newBlock = obj.AddComponent<Block>();
            if (eh2 != null)
                newBlock.maxHealth = eh2.blockMaxHealth;
        }

        if (wallLayer >= 0) obj.layer = wallLayer;
        else Debug.LogWarning("PlayerCarry.Drop: layer 'Wall' not found.");

        if (eh2 != null) eh2.isConvertedToBlock = true;

        var ef = obj.GetComponent<EnemyFollow>();
        if (ef != null) Destroy(ef);

        carriedObjects[index] = swappedIn;

        Debug.Log($"PlayerCarry: сбросил {obj.name} из руки {index} в позицию {dropPos}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, carriedAttackRadius);
    }
}
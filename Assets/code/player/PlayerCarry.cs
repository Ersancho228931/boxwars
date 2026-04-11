using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    public Transform[] hands; // 4 hand slots
    public float pickupRange = 2f;
    public LayerMask enemyLayer; // optional: leave empty to auto-detect
    public KeyCode pickupKey = KeyCode.Space;
    public string carriedLayerName = "Carried"; // создайте этот слой в Project Settings или будет использован Ignore Raycast

    private GameObject[] carriedObjects;
    private int carriedLayer;

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
        if (Input.GetKeyDown(pickupKey))
            TryPickupOrDrop();
    }

    void TryPickupOrDrop()
    {
        // Если есть хотя бы один предмет в руках — первично бросаем/меняем (удобство управления)
        if (HasAnyCarried())
        {
            DropLast();
            return;
        }

        // Иначе — пытаемся подобрать
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

        // ищем ближайший подходящий объект (труп или стена или допустимый живой Shooter)
        GameObject best = null;
        float bestDist = Mathf.Infinity;

        foreach (var c in hits)
        {
            if (c == null) continue;
            var obj = c.gameObject;
            if (obj == null) continue;

            // игнорируем уже переносимые объекты по слою или по списку carriedObjects
            if (obj.layer == carriedLayer) continue;
            if (IsAlreadyCarried(obj)) continue;

            // обязаны иметь Collider2D и Rigidbody2D
            if (obj.GetComponent<Collider2D>() == null || obj.GetComponent<Rigidbody2D>() == null) continue;

            // фильтруем: либо это Block (стена), либо EnemyHealth (труп) или специальный Shooter (может быть живым)
            var block = obj.GetComponent<Block>();
            var enemy = obj.GetComponent<EnemyHealth>();
            var shooter = obj.GetComponent<ShooterController>();

            // нельзя поднимать живого врага, за исключением shooter с разрешением
            if (enemy != null && !enemy.isDead)
            {
                if (shooter == null || !shooter.canBePickedWhileAlive)
                    continue;
            }

            // считаем кандидатом
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

        // поднимаем найденный объект в соответствующую руку
        var bestBlock = best.GetComponent<Block>();
        var bestEnemy = best.GetComponent<EnemyHealth>();
        var bestShooter = best.GetComponent<ShooterController>();

        int handIndex = -1;

        // Если это живой shooter и он позволяет подбор — используем именно его allowedPickupHandIndex
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

        // Обычный путь для трупов/блоков
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

    void PickupLiveShooter(GameObject obj, int handIndex)
    {
        // Специальный путь подъёма для живого Shooter — делает объект переносимым и оставляет EnemyHealth.isDead == false
        var shooter = obj.GetComponent<ShooterController>();
        var enemy = obj.GetComponent<EnemyHealth>();

        carriedObjects[handIndex] = obj;

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false; // важное условие для ShooterController.IsCarried()
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        obj.transform.SetParent(hands[handIndex], worldPositionStays: true);
        obj.transform.position = hands[handIndex].position;

        obj.layer = carriedLayer;

        // Если нужно — отключаем движение/следование
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

        obj.transform.SetParent(hands[handIndex], worldPositionStays: true);
        obj.transform.position = hands[handIndex].position;

        obj.layer = carriedLayer;

        Debug.Log($"PlayerCarry: поднял труп {obj.name} в руку {handIndex}");
    }

    void PickupBlock(GameObject obj, int handIndex)
    {
        var enemy = obj.GetComponent<EnemyHealth>();
        var block = obj.GetComponent<Block>();

        // запрет на поднятие живых — дополнительная защита
        if (enemy != null && !enemy.isDead && (obj.GetComponent<ShooterController>() == null || !obj.GetComponent<ShooterController>().canBePickedWhileAlive))
        {
            Debug.LogWarning($"PlayerCarry.PickupBlock: {obj.name} живой — отказ.");
            return;
        }

        carriedObjects[handIndex] = obj;

        // НЕ удаляем компонент Block — отключаем его, чтобы не потерять инспекторные/текущие значения HP
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

        obj.transform.SetParent(hands[handIndex], worldPositionStays: true);
        obj.transform.position = hands[handIndex].position;

        obj.layer = carriedLayer;

        Debug.Log($"PlayerCarry: подобрал стену {obj.name} в руку {handIndex}");
    }

    void DropLast()
    {
        // бросаем последнюю занятыю руку (правее)
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

                // подготовим существующую стену к поднятию (swap)
                var bc = existingObj.GetComponent<Block>();
                if (bc != null)
                {
                    // отключаем, чтобы сохранить настройки HP и текущее состояние, а не удалять компонент
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

        // ставим текущий объект как стену
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

        // восстановим компонент Block если он был, либо создадим и инициализируем HP из EnemyHealth (если есть)
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

        // если swappedIn != null — рука теперь содержит существующую стену, иначе освобождаем руку
        carriedObjects[index] = swappedIn;

        Debug.Log($"PlayerCarry: сбросил {obj.name} из руки {index} в позицию {dropPos}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
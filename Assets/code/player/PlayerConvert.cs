using UnityEngine;

public class PlayerConvert : MonoBehaviour
{
    public float range = 2f;
    public LayerMask enemyLayer;
    public KeyCode convertKey = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyDown(convertKey))
        {
            TryConvert();
        }
    }

    void TryConvert()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null && enemy.isDead && !enemy.isConvertedToBlock)
            {
                ConvertToBlock(enemy);
                break;
            }
        }
    }

    void ConvertToBlock(EnemyHealth enemy)
    {
        enemy.isConvertedToBlock = true;
        GameObject obj = enemy.gameObject;

        var b = obj.AddComponent<Block>();
        b.maxHealth = enemy.blockMaxHealth;

        int wallLayer = LayerMask.NameToLayer("Wall");
        obj.layer = wallLayer >= 0 ? wallLayer : 0;

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
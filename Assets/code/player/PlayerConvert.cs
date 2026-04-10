using UnityEngine;

public class PlayerConvert : MonoBehaviour
{
    public float range = 2f;
    public LayerMask enemyLayer;
    public KeyCode convertKey = KeyCode.Space; // можно оставить Space, но смените pickupKey в PlayerCarry на E

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
                break; // only convert one at a time
            }
        }
    }

    void ConvertToBlock(EnemyHealth enemy)
    {
        enemy.isConvertedToBlock = true;

        GameObject obj = enemy.gameObject;

        // Add block script
        obj.AddComponent<Block>();

        // Make it solid wall
        obj.layer = LayerMask.NameToLayer("Wall");

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        Collider2D col = obj.GetComponent<Collider2D>();
        col.isTrigger = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
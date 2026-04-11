using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 5f;
    public GameObject owner;

    // Если true — игнорируем попадания по игроку, несущему владельца (кейс: носимый shooter)
    public bool ignoreCarrierPlayer = false;

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

        // ignore collisions with owner or owner's children
        if (owner != null)
        {
            if (hit == owner) return;
            if (hit.transform.IsChildOf(owner.transform)) return;
        }

        // Если снаряд помечен игнорировать носителя — не раним игрока
        if (ignoreCarrierPlayer && hit.CompareTag("Player")) return;

        // Enemy
        var eh = hit.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Player
        var ph = hit.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // иначе — стук по окружению -> уничтожить
        Destroy(gameObject);
    }
}

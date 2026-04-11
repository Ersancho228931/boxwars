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
        if (col == null) return;

        // ignore collisions with owner itself
        if (owner != null && col.gameObject == owner) return;

        // Если владелец переносится игроком и снаряд может повредить игрока — игнорируем попадания по игроку
        if (ignoreCarrierPlayer && col.gameObject.CompareTag("Player"))
            return;

        var eh = col.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        var ph = col.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // collide with environment -> destroy
        Destroy(gameObject);
    }
}

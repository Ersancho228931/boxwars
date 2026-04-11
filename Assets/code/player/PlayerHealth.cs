using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public TMP_Text healthText; // drag TMP text here

    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite damagedSprite;     // <50
    public Sprite almostDeadSprite;  // <20
    public Sprite deadSprite;        // 0

    private SpriteRenderer sr;
    private PlayerMovement movement;
    private PlayerCarry carry;
    private PlayerConvert convert;

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        carry = GetComponent<PlayerCarry>();
        convert = GetComponent<PlayerConvert>();
        UpdateUI();
        UpdateSprite();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateUI();
        UpdateSprite();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = "" + currentHealth;
        }
    }

    void UpdateSprite()
    {
        if (sr == null) return;

        if (currentHealth <= 0)
        {
            if (deadSprite != null) sr.sprite = deadSprite;
            return;
        }

        if (currentHealth < 20)
        {
            if (almostDeadSprite != null) sr.sprite = almostDeadSprite;
            return;
        }

        if (currentHealth < 50)
        {
            if (damagedSprite != null) sr.sprite = damagedSprite;
            return;
        }

        if (normalSprite != null) sr.sprite = normalSprite;
    }

    void Die()
    {
        // не уничтожаем объект Ч делаем из него мЄртвое тело: отключаем управление/подъЄм/конвертацию
        if (movement != null) movement.enabled = false;
        if (carry != null) carry.enabled = false;
        if (convert != null) convert.enabled = false;

        // отключаем физику передвижени€
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // пометить как труп (можно добавить компонент Block через PlayerConvert)
        // sprite уже установлен в UpdateSprite()

        // отключаем коллайдер как триггер=false (оставл€ем физический коллайдер чтобы тело можно было подн€ть)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;
    }
}
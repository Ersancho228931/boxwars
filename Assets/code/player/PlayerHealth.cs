using System.Collections;
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

    [Header("Damage visual")]
    public Color damageFlashColor = Color.white; // choose in inspector (white or red)
    public float flashDuration = 0.12f;

    [Header("Startup Info text")]
    public TMP_Text infoText;        // drag TMP text here
    [TextArea] public string infoMessage = "Welcome!";

    private SpriteRenderer sr;
    private PlayerMovement movement;
    private PlayerCarry carry;
    private PlayerConvert convert;

    private Color originalColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        carry = GetComponent<PlayerCarry>();
        convert = GetComponent<PlayerConvert>();
        originalColor = sr != null ? sr.color : Color.white;

        if (healthText != null) UpdateUI();
        UpdateSprite();

        // Show info text at the start of the game
        if (infoText != null)
        {
            infoText.text = infoMessage;
            infoText.gameObject.SetActive(true);
            StartCoroutine(HideInfoAfterStartup(15f));
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateUI();
        UpdateSprite();

        // flash sprite
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashDamage()
    {
        if (sr == null) yield break;
        sr.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

        // плавно вернуть цвет за короткое врем€
        float t = 0f;
        float fade = Mathf.Max(0.05f, flashDuration);
        Color from = sr.color;
        while (t < fade)
        {
            t += Time.deltaTime;
            sr.color = Color.Lerp(from, originalColor, t / fade);
            yield return null;
        }
        sr.color = originalColor;
        flashCoroutine = null;
    }

    IEnumerator HideInfoAfterStartup(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (infoText != null)
        {
            // Disables the text. Change to Destroy(infoText.gameObject); if you want it permanently deleted from the scene.
            infoText.gameObject.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = "" + Mathf.Max(0, currentHealth);
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

        // sprite уже установлен в UpdateSprite()

        // отключаем коллайдер как триггер=false (оставл€ем физический коллайдер чтобы тело можно было подн€ть)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        // UI: показать экран проигрыша Ч требуетс€ объект UIManager в сцене
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLose();
        }
        else
        {
            Debug.LogWarning("PlayerHealth.Die: UIManager.Instance == null Ч назначьте UIManager в сцене и укажите HUD/LoseScreen/WinScreen объекты.");
        }
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public TMP_Text healthText;

    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite damagedSprite;     // <100
    public Sprite damagedSprite2;     // <61
    public Sprite almostDeadSprite;  // <31
    public Sprite deadSprite;        // 0

    [Header("Damage visual")]
    public Color damageFlashColor = Color.white;
    public float flashDuration = 0.12f;

    [Header("Sound")]
    public AudioClip damageSound;

    [Header("Startup Info text")]
    public TMP_Text infoText;
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
        SyncHealthToMovement(); // Sync at start

        if (infoText != null)
        {
            infoText.text = infoMessage;
            infoText.gameObject.SetActive(true);
            StartCoroutine(HideInfoAfterStartup(15f));
        }
    }
    private bool playedInjuredSound = false;

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Play damage sound
        if (damageSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayOneShot(damageSound, 1.0f);

        // ONE-TIME INJURED SOUND LOGIC
        if (currentHealth <= 20 && !playedInjuredSound)
        {
            // Play the sound once (Assuming your AudioManager has a specific clip for this)
            AudioManager.Instance.PlayOneShot(AudioManager.Instance.playerWalkInjured, 1.0f);
            playedInjuredSound = true;
        }

        UpdateUI();
        UpdateSprite();
        SyncHealthToMovement();

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamage());

        if (currentHealth <= 0) Die();
    }

    // Tells the movement script how we are feeling
    void SyncHealthToMovement()
    {
        if (movement != null)
        {
            movement.UpdateHealthStatus(currentHealth);
        }
    }

    IEnumerator FlashDamage()
    {
        if (sr == null) yield break;
        sr.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

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
        if (infoText != null) infoText.gameObject.SetActive(false);
    }

    void UpdateUI()
    {
        if (healthText != null) healthText.text = "" + Mathf.Max(0, currentHealth);
    }

    void UpdateSprite()
    {
        if (sr == null) return;
        if (currentHealth <= 0) { if (deadSprite != null) sr.sprite = deadSprite; return; }
        if (currentHealth < 31) { if (almostDeadSprite != null) sr.sprite = almostDeadSprite; return; }
        if (currentHealth < 61) { if (damagedSprite2 != null) sr.sprite = damagedSprite2; return; }
        if (currentHealth < 100) { if (damagedSprite != null) sr.sprite = damagedSprite; return; }
        if (normalSprite != null) sr.sprite = normalSprite;
    }

    void Die()
    {
        if (movement != null) movement.enabled = false;
        if (carry != null) carry.enabled = false;
        if (convert != null) convert.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        if (UIManager.Instance != null) UIManager.Instance.ShowLose();
    }
}
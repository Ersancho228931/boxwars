using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD / Screens")]
    public GameObject hud;
    public GameObject winScreen;
    public GameObject loseScreen;

    [Header("Boss banner & health")]
    public GameObject bossPanel;
    public TMP_Text bossText;
    public Slider bossHealthBar;

    [Header("Sounds")]
    public AudioClip winSound;
    public AudioClip loseSound;

    void Awake()
    {
        // FIXED: just overwrite so the NEW scene always gets a fresh manager
        // (exactly like you already do in DayNightManager)
        Instance = this;
    }

    public void ShowWin()
    {
        StartCoroutine(WinSequence());
    }

    private System.Collections.IEnumerator WinSequence()
    {
        // Play win sound
        if (winSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayOneShot(winSound);

        // Fade out all other audio over 3 seconds, then stop
        if (AudioManager.Instance != null)
            yield return StartCoroutine(FadeOutAudio(3f));

        yield return new WaitForSeconds(2f);

        if (hud != null) hud.SetActive(false);
        if (winScreen != null) winScreen.SetActive(true);
        if (loseScreen != null) loseScreen.SetActive(false);
        HideBoss();
    }

    private System.Collections.IEnumerator FadeOutAudio(float duration)
    {
        if (AudioManager.Instance == null) yield break;
        
        float elapsed = 0f;
        AudioSource sfxSource = AudioManager.Instance.sfxSource;
        
        if (sfxSource == null) yield break;
        
        float startVolume = sfxSource.volume;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sfxSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        // Stop all audio
        sfxSource.volume = startVolume;  // Reset for next scene
        AudioManager.Instance.StopAllAudio();
    }

    public void ShowLose()
    {
        // Play lose sound
        if (loseSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayOneShot(loseSound);

        StartCoroutine(LoseSequence());
    }

    private System.Collections.IEnumerator LoseSequence()
    {
        // Fade out audio over 3 seconds
        if (AudioManager.Instance != null)
            yield return StartCoroutine(FadeOutAudio(3f));

        if (hud != null) hud.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(true);
        if (winScreen != null) winScreen.SetActive(false);
        HideBoss();
    }

    public void ShowBoss(string name)
    {
        if (bossText != null) bossText.text = name;
        if (bossPanel != null) bossPanel.SetActive(true);
    }

    public void HideBoss()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
    }

    public void SetBossMaxHealth(int max)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.maxValue = Mathf.Max(1, max);
            bossHealthBar.value = bossHealthBar.maxValue;
        }
    }

    public void UpdateBossHealth(int current)
    {
        if (bossHealthBar != null)
            bossHealthBar.value = Mathf.Clamp(current, 0, (int)bossHealthBar.maxValue);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
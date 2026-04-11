using System.Threading;
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
    public GameObject bossPanel; // show/hide
    public TMP_Text bossText;
    public Slider bossHealthBar;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void ShowWin()
    {
        int milliseconds = 2000;
        Thread.Sleep(milliseconds);

        if (hud != null) hud.SetActive(false);
        if (winScreen != null) winScreen.SetActive(true);
        if (loseScreen != null) loseScreen.SetActive(false);
        HideBoss();
    }

    public void ShowLose()
    {
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
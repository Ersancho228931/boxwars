    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD / Screens")]
    public GameObject hud;       // основное HUD, которое скрываем при смерти
    public GameObject winScreen;
    public GameObject loseScreen;

    [Header("Boss banner")]
    public GameObject bossPanel; // контейнер (можно пустой), скрывается/показывается
    public TMP_Text bossText;    // текст для имени босса

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void ShowWin()
    {
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

    public void HideHUD()
    {
        if (hud != null) hud.SetActive(false);
    }

    public void ShowHUD()
    {
        if (hud != null) hud.SetActive(true);
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

    // привязать к кнопке Restart в инспекторе
    public void Restart()
    {
        // опционально: сбросить Time.scale
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

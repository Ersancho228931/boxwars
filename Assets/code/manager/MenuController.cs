using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Difficulty scenes")]
    public int easySceneIndex = 1;
    public int mediumSceneIndex = 2;
    public int hardSceneIndex = 3;

    [Header("UI Panels")]
    public GameObject helpPanel;
    public GameObject difficultyPanel;

    public void LoadEasy() => LoadSceneIndex(easySceneIndex);
    public void LoadMedium() => LoadSceneIndex(mediumSceneIndex);
    public void LoadHard() => LoadSceneIndex(hardSceneIndex);

    public void LoadSceneIndex(int idx)
    {
        Time.timeScale = 1f;
        if (idx >= 0) SceneManager.LoadScene(idx);
    }

    public void ShowHelp(bool show)
    {
        if (helpPanel != null) helpPanel.SetActive(show);
    }

    // Новые методы для панели выбора сложности
    public void ShowDifficulty(bool show)
    {
        if (difficultyPanel != null) difficultyPanel.SetActive(show);
    }

    // прикрепите к кнопке Back внутри difficultyPanel
    public void BackFromDifficulty()
    {
        ShowDifficulty(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
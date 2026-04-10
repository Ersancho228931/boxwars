using UnityEngine;
using TMPro;

public class DayNightManager : MonoBehaviour
{
    public float dayDuration = 45f;
    public float nightDuration = 30f;

    public TMP_Text infoText;
    public GameObject nightOverlay; // drag UI image here

    private float timer;
    private bool isDay = true;
    private int dayCount = 1;

    public static DayNightManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        timer = dayDuration;
        UpdateUI();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            SwitchPhase();
        }
    }

    void SwitchPhase()
    {
        isDay = !isDay;

        if (isDay)
        {
            dayCount++;
            timer = dayDuration;

            FindObjectOfType<EnemySpawner>().IncreaseDifficulty(dayCount);
        }
        else
        {
            timer = nightDuration;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        string phase = isDay ? "DAY" : "NIGHT";
        infoText.text = "Day: " + dayCount + "\n" + phase;

        // 🌙 Night overlay
        if (nightOverlay != null)
            nightOverlay.SetActive(!isDay);
    }

    public bool IsDay()
    {
        return isDay;
    }

    public int GetDay()
    {
        return dayCount;
    }
}
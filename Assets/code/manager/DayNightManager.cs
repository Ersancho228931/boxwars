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
        // 🌑 Check if it is Night 5. If it is, we stop the timer so it never turns Day.
        if (!isDay && dayCount >= 5)
        {
            return; // Exit Update early, stopping the countdown
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            SwitchPhase();
        }
    }

    void SwitchPhase()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.dayNightChange, 1.5f);
        isDay = !isDay;

        if (isDay)
        {
            dayCount++;
            timer = dayDuration;

            // Make sure EnemySpawner exists before calling
            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.IncreaseDifficulty(dayCount);
            }
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

        // Custom text for the final forever night
        if (!isDay && dayCount >= 5)
        {
            infoText.text = "Day: " + dayCount + "\nFOREVER NIGHT";
        }
        else
        {
            infoText.text = "Day: " + dayCount + "\n" + phase;
        }

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
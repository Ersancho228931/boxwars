using UnityEngine;

public class DayNightGrass : MonoBehaviour
{
    [Header("Grass Sprites")]
    public Sprite dayGrassSprite;
    public Sprite nightGrassSprite;

    private SpriteRenderer sr;
    private bool wasNight = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("DayNightGrass: No SpriteRenderer found on " + gameObject.name);
            return;
        }

        UpdateGrass();
    }

    void Update()
    {
        if (DayNightManager.instance == null) return;

        bool isNight = !DayNightManager.instance.IsDay();

        if (isNight != wasNight)
        {
            UpdateGrass();
            wasNight = isNight;
        }
    }

    void UpdateGrass()
    {
        if (sr == null) return;

        bool isNight = !DayNightManager.instance.IsDay();

        if (isNight && nightGrassSprite != null)
            sr.sprite = nightGrassSprite;
        else if (!isNight && dayGrassSprite != null)
            sr.sprite = dayGrassSprite;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string typeName;
        public GameObject prefab;
        [Header("Day range")]
        public bool spawnEveryDay = false;
        public int spawnDayMin = 1;
        public int spawnDayMax = 1;
        [Header("Counts")]
        public int spawnCountDay = 1;
        public int spawnCountNight = 0;
        [Header("Timing per entry")]
        public float spawnIntervalMin = 0.5f;
        public float spawnIntervalMax = 1.5f;
    }

    [Header("Global spawn limits")]
    public int maxEnemiesDay = 5;
    public int maxEnemiesNight = 10;
    public int maxDeadBodies = 50;  // Increased to keep more dead bodies visible

    [Header("Default spawn timing (fallback)")]
    public float minSpawnTime = 2f;
    public float maxSpawnTime = 4f;

    public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    private int currentEnemies = 0;
    private List<GameObject> deadBodies = new List<GameObject>();
    private int lastDay = -1;
    private bool lastIsDay = false;
    private bool hasClearedNight5Bodies = false;

    void Start()
    {
        StartCoroutine(SpawnLoop());
        if (DayNightManager.instance != null)
        {
            lastDay = DayNightManager.instance.GetDay();
            lastIsDay = DayNightManager.instance.IsDay();
            if (lastIsDay)
                OnDayStart(lastDay);
            else
                OnNightStart(lastDay);
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            if (DayNightManager.instance != null)
            {
                int day = DayNightManager.instance.GetDay();
                bool isDay = DayNightManager.instance.IsDay();

                if (day != lastDay || isDay != lastIsDay)
                {
                    int prevDay = lastDay;
                    bool prevIsDay = lastIsDay;
                    lastDay = day;
                    lastIsDay = isDay;

                    if (isDay && !prevIsDay)
                        OnDayStart(day);
                    else if (!isDay && prevIsDay)
                        OnNightStart(day);
                    else if (day != prevDay && isDay)
                        OnDayStart(day);
                }
            }
        }
    }

    void OnNightStart(int day)
    {
        // NEW: Clear dead bodies on Night 5 if option enabled
        if (day >= 5 && DayNightManager.instance != null && DayNightManager.instance.clearDeadBodiesOnNight5)
        {
            if (!hasClearedNight5Bodies)
            {
                ClearAllDeadBodies();
                hasClearedNight5Bodies = true;
                Debug.Log("Night 5 Forever Night - All dead bodies cleared.");
            }
        }

        foreach (var entry in spawnEntries)
        {
            if (entry == null || entry.prefab == null) continue;
            if (entry.spawnEveryDay || (day >= entry.spawnDayMin && day <= entry.spawnDayMax))
            {
                int count = Mathf.Max(0, entry.spawnCountNight);
                if (count > 0)
                    StartCoroutine(SpawnEntryRoutine(entry, count));
            }
        }
    }

    void OnDayStart(int day)
    {
        foreach (var entry in spawnEntries)
        {
            if (entry == null || entry.prefab == null) continue;
            if (entry.spawnEveryDay || (day >= entry.spawnDayMin && day <= entry.spawnDayMax))
            {
                int count = Mathf.Max(0, entry.spawnCountDay);
                if (count > 0)
                    StartCoroutine(SpawnEntryRoutine(entry, count));
            }
        }
    }

    IEnumerator SpawnEntryRoutine(SpawnEntry entry, int count)
    {
        Vector3 prefabScale = entry.prefab != null ? entry.prefab.transform.localScale : Vector3.one;

        for (int i = 0; i < count; i++)
        {
            float wait = Random.Range(entry.spawnIntervalMin, entry.spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            int tries = 0;
            while (currentEnemies >= ((DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight))
            {
                yield return new WaitForSeconds(0.5f);
                tries++;
                if (tries > 40) break;
            }

            if (currentEnemies < ((DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight))
            {
                GameObject e = Instantiate(entry.prefab, transform.position, Quaternion.identity);
                e.transform.localScale = prefabScale;
                e.transform.SetParent(null);
                currentEnemies++;

                EnemyHealth eh = e.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.OnDeath += () =>
                    {
                        currentEnemies--;
                        deadBodies.Add(e);
                        // Only clean up old bodies if we exceed the limit significantly
                        if (deadBodies.Count > maxDeadBodies)
                        {
                            // Remove from list but DON'T destroy - let it stay as a corpse
                            GameObject oldBody = deadBodies[0];
                            deadBodies.RemoveAt(0);
                            // Destroy old bodies only after a delay to avoid sudden disappearing
                            if (oldBody != null)
                                Destroy(oldBody, 10f);  // Увеличено с 5f до 10f
                        }
                    };
                }
                else
                {
                    Destroy(e, 60f);
                }
            }
        }
    }

    public void IncreaseDifficulty(int day)
    {
        maxEnemiesDay += 1;
        maxEnemiesNight += 2;
        maxDeadBodies += 2;
    }

    public void ClearAllDeadBodies()
    {
        for (int i = deadBodies.Count - 1; i >= 0; i--)
        {
            if (deadBodies[i] != null)
                Destroy(deadBodies[i]);
        }
        deadBodies.Clear();
    }
}
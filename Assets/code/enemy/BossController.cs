    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    [Header("Boss")]
    public string bossName = "THEBOSS";

    private EnemyHealth enemyHealth;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        // Показать баннер босса при спавне
        if (UIManager.Instance != null)
            UIManager.Instance.ShowBoss(bossName);
    }

    void OnEnable()
    {
        enemyHealth = enemyHealth ?? GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.OnDeath += OnBossDeath;
    }

    void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnBossDeath;
    }

    void OnBossDeath()
    {
        // При смерти босса — экран победы
        if (UIManager.Instance != null)
            UIManager.Instance.ShowWin();
    }
}

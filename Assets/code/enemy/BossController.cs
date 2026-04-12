    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    [Header("Boss")]
    public string bossName = "THEBOSS";
    public AudioClip bossSpawnSound;

    private EnemyHealth enemyHealth;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        // Play boss spawn sound
        if (bossSpawnSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayOneShot(bossSpawnSound);

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
        // ��� ������ ����� � ����� ������
        if (UIManager.Instance != null)
            UIManager.Instance.ShowWin();
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.XRIStarterKit;

public class BossEventController : MonoBehaviour
{
    [Header("보스 Health 컴포넌트")]
    public EnemyHealth bossHealth;

    [Header("보스 체력이 이 값 이하가 되면 스포너 등장")]
    public float npcSpawnerTriggerHP = 30f;
    private bool npcSpawnerActivated = false;

    [Header("Npc Spawner 리스트 (여러개 등장 가능)")]
    public List<NpcSpawnerEntry> npcSpawners = new List<NpcSpawnerEntry>();
    [System.Serializable]
    public class NpcSpawnerEntry
    {
        public GameObject npcSpawnerPrefab;
        public Transform spawnPoint;
        public GameObject spawnEffectPrefab;
    }

    [Header("보스 사망시 소환 프리팹 리스트 (여러개 가능)")]
    public List<SpawnOnDeathEntry> spawnOnDeathList = new List<SpawnOnDeathEntry>();
    [System.Serializable]
    public class SpawnOnDeathEntry
    {
        public GameObject prefabToSpawn;
        public Transform spawnPoint;
    }

    [Header("보스 사망 이펙트 (파티클)")]
    public GameObject deathEffectPrefab;

    private bool isDead = false;

    void Awake()
    {
        if (!bossHealth)
            bossHealth = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        if (bossHealth)
            bossHealth.OnTakeDamage += OnBossTakeDamage;
    }

    void OnDisable()
    {
        if (bossHealth)
            bossHealth.OnTakeDamage -= OnBossTakeDamage;
    }

    private void OnBossTakeDamage(float damage)
    {
        if (isDead) return;

        float curHp = GetCurrentHp();

        // 1. 체력 이하일 때 스포너 소환
        if (!npcSpawnerActivated && curHp <= npcSpawnerTriggerHP && curHp > 0f)
        {
            npcSpawnerActivated = true;
            SpawnAllSpawners();
        }

        // 2. 체력 0 이하 -> 사망 처리
        if (curHp <= 0f)
        {
            isDead = true;

            // (1) 사망시 프리팹 소환
            foreach (var entry in spawnOnDeathList)
            {
                if (entry.prefabToSpawn && entry.spawnPoint)
                    Instantiate(entry.prefabToSpawn, entry.spawnPoint.position, entry.spawnPoint.rotation);
            }

            // (2) 사망 이펙트 (파티클)
            if (deathEffectPrefab)
            {
                GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 5f); // 파티클 지속시간 이후 제거
            }

            // (3) 보스 오브젝트 삭제 (코루틴으로 지연)
            StartCoroutine(DelayedDestroy());
        }
    }

    // NpcSpawner 여러개 소환
    private void SpawnAllSpawners()
    {
        foreach (var entry in npcSpawners)
        {
            if (entry.npcSpawnerPrefab && entry.spawnPoint)
            {
                Instantiate(entry.npcSpawnerPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

                if (entry.spawnEffectPrefab)
                    Instantiate(entry.spawnEffectPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
            }
        }
    }

    // EnemyHealth의 currentHealth를 리플렉션으로 안전하게 읽음
    private float GetCurrentHp()
    {
        var field = typeof(EnemyHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (float)field.GetValue(bossHealth);
        }
        return -1f;
    }

    // 보스 오브젝트 삭제를 지연시켜 파티클이 먼저 재생되도록 함
    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}

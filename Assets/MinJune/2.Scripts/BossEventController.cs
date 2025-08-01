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

    [Header("보스 사망 사운드 이름 (AudioManager에서 관리)")]
    public string deathSFXName = "BossDie";

    [Header("스폰된 오브젝트가 남아있는 동안 보스 무적")]
    public bool blockDamageWhileSpawnedObjectsExist = true;
    public string spawnedTag = "SpawnedNPC";
    private bool isBlockingDamage = false;

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
        if (blockDamageWhileSpawnedObjectsExist && isBlockingDamage)
        {
            return;
        }

        float curHp = GetCurrentHp();

        if (!npcSpawnerActivated && curHp <= npcSpawnerTriggerHP && curHp > 0f)
        {
            Debug.Log("보스 체력 트리거 이하 → 스포너 활성화");
            npcSpawnerActivated = true;
            StartCoroutine(SpawnAllSpawnersCoroutine());  // ★ 코루틴 호출!

            if (blockDamageWhileSpawnedObjectsExist)
            {
                isBlockingDamage = true;
                StartCoroutine(CheckSpawnedObjectsCoroutine());
            }
        }

        if (!isDead && Mathf.Approximately(curHp, 0f))
        {
            Debug.Log("보스 체력 0 이하 → 사망 처리 시작");
            isDead = true;

            // (1) 아이템 드롭
            foreach (var entry in spawnOnDeathList)
            {
                if (entry.prefabToSpawn && entry.spawnPoint)
                    Instantiate(entry.prefabToSpawn, entry.spawnPoint.position, entry.spawnPoint.rotation);
            }

            // (2) 사망 사운드 재생
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathSFXName))
                AudioManager.Instance.PlaySFX(deathSFXName, transform.position);

            // (3) 파티클 재생
            if (deathEffectPrefab)
            {
                Debug.Log("deathEffectPrefab 생성 시도");
                Vector3 effectPos = transform.position + Vector3.up * 1f;
                GameObject effect = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

                var ps = effect.GetComponent<ParticleSystem>();
                float destroyDelay = 5f;
                if (ps != null)
                {
                    destroyDelay = ps.main.duration + ps.main.startLifetime.constantMax;
                    ps.Play();
                }
                Destroy(effect, destroyDelay);
            }

            // (4) 보스 제거 딜레이
            StartCoroutine(DelayedDestroy());
        }
    }

    // ⭐️★ 핵심 코루틴! (파티클 → 대기 → NPC 소환 → 파티클 삭제)
    private IEnumerator SpawnAllSpawnersCoroutine()
    {
        foreach (var entry in npcSpawners)
        {
            GameObject fx = null;
            float maxDuration = 0.5f;

            // (1) 파티클 소환 및 시간 계산
            if (entry.spawnEffectPrefab && entry.spawnPoint)
            {
                fx = Instantiate(entry.spawnEffectPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
                var allParticles = fx.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in allParticles)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    if (duration > maxDuration) maxDuration = duration;
                }
            }

            // (2) 파티클 시간만큼 대기
            yield return new WaitForSeconds(maxDuration);

            // (3) NPC 스폰
            if (entry.npcSpawnerPrefab && entry.spawnPoint)
            {
                Instantiate(entry.npcSpawnerPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
            }

            // (4) 파티클 자연 삭제
            if (fx)
            {
                Destroy(fx, 1f); // 혹시 남아있을 때를 대비
            }
        }
    }

    private float GetCurrentHp()
    {
        var field = typeof(EnemyHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (float)field.GetValue(bossHealth);
        }
        return -1f;
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    private IEnumerator CheckSpawnedObjectsCoroutine()
    {
        while (true)
        {
            var spawnedObjs = GameObject.FindGameObjectsWithTag(spawnedTag);
            if (spawnedObjs.Length == 0)
            {
                isBlockingDamage = false;
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
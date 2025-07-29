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

    // -----------[여기서부터 추가]-----------
    [Header("스폰된 오브젝트가 남아있는 동안 보스 무적")]
    public bool blockDamageWhileSpawnedObjectsExist = true;
    public string spawnedTag = "SpawnedNPC";  // 스폰되는 NPC가 가지는 태그 (Inspector에서 지정)
    private bool isBlockingDamage = false;
    // -------------------------------------

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
        // ----------[여기 추가]-----------
        if (blockDamageWhileSpawnedObjectsExist && isBlockingDamage)
        {
            // Debug.Log("보스 무적 상태! 데미지 무시됨");
            return;
        }
        // ------------------------------

        float curHp = GetCurrentHp();

        if (!npcSpawnerActivated && curHp <= npcSpawnerTriggerHP && curHp > 0f)
        {
            Debug.Log("보스 체력 트리거 이하 → 스포너 활성화");
            npcSpawnerActivated = true;
            SpawnAllSpawners();

            // ---------------[여기 추가]---------------
            if (blockDamageWhileSpawnedObjectsExist)
            {
                isBlockingDamage = true;
                StartCoroutine(CheckSpawnedObjectsCoroutine());
            }
            // --------------------------------------
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

            // (2) 파티클 재생
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

            // (3) 보스 제거 딜레이
            StartCoroutine(DelayedDestroy());
        }
    }

    private void SpawnAllSpawners()
    {
        foreach (var entry in npcSpawners)
        {
            if (entry.npcSpawnerPrefab && entry.spawnPoint)
            {
                Instantiate(entry.npcSpawnerPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

                if (entry.spawnEffectPrefab)
                {
                    GameObject fx = Instantiate(entry.spawnEffectPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

                    // 모든 ParticleSystem에서 최대 재생 시간 구함
                    float maxDuration = 0f;
                    var allParticles = fx.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                        if (duration > maxDuration) maxDuration = duration;
                    }
                    if (maxDuration < 0.5f) maxDuration = 5f; // fallback

                    Destroy(fx, maxDuration);
                }
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

    // ----------------[여기 추가!]-------------------
    private IEnumerator CheckSpawnedObjectsCoroutine()
    {
        // 최소 0.5초마다 체크 (부하 적게)
        while (true)
        {
            var spawnedObjs = GameObject.FindGameObjectsWithTag(spawnedTag);
            if (spawnedObjs.Length == 0)
            {
                isBlockingDamage = false;
                // Debug.Log("스폰된 오브젝트 전부 파괴됨 → 보스 무적 해제");
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    // ---------------------------------------------
}
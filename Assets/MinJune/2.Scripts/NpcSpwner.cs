using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NpcSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public Transform spawnPoint;
        public GameObject npcPrefab;
    }

    [Header("스폰할 NPC 및 위치 정보")]
    public List<SpawnEntry> spawnEntries;

    [Header("등장 이펙트(파티클) 프리팹")]
    public GameObject spawnEffectPrefab;

    [Header("등장 사운드 이름 (AudioManager에서 관리)")]
    public string spawnSFXName = "EnemySpawn"; // 원하는 이름으로 바꿔도 됨

    [Header("스폰 시간 조절")]
    public float delayBetweenSpawns = 0.5f;
    public float npcSpawnDelayAfterEffectStart = 0.1f;

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    private bool hasSpawned = false;

    [Header("이 스테이지에서 포탈이 이동할 다음 씬 이름")]
    public string nextSceneName; // **Inspector에서 입력**

    private void OnTriggerEnter(Collider other)
    {
        if (hasSpawned) return;

        if (other.CompareTag(playerTag))
        {
            // 스폰 직전 enemyCount를 반드시 0으로!
            if (GameManager.instance != null)
            {
                GameManager.instance.enemyCount = 0;
                // ★ 이 스테이지의 다음 씬 이름을 GameManager에 전달!
                GameManager.instance.SetNextScene(nextSceneName);
            }

            StartCoroutine(SpawnRoutine());
            hasSpawned = true;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        foreach (SpawnEntry entry in spawnEntries)
        {
            yield return StartCoroutine(SpawnAtPoint(entry.spawnPoint, entry.npcPrefab));

            if (delayBetweenSpawns > 0)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    private IEnumerator SpawnAtPoint(Transform point, GameObject npcToSpawn)
    {
        GameObject instantiatedEffect = null;
        ParticleSystem ps = null;

        if (spawnEffectPrefab != null)
        {
            instantiatedEffect = Instantiate(spawnEffectPrefab, point.position, point.rotation);
            ps = instantiatedEffect.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }

        if (npcSpawnDelayAfterEffectStart > 0)
            yield return new WaitForSeconds(npcSpawnDelayAfterEffectStart);

        // 🔊 [여기 추가] 적 등장 효과음 재생
        if (!string.IsNullOrEmpty(spawnSFXName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(spawnSFXName, point.position);
        }

        if (npcToSpawn != null)
        {
            GameObject npc = Instantiate(npcToSpawn, point.position, point.rotation);
            if (GameManager.instance != null)
            {
                GameManager.instance.enemyCount++;
                Debug.Log("적 생성, 현재 적 수: " + GameManager.instance.enemyCount);
            }
        }

        if (instantiatedEffect != null)
        {
            if (ps != null)
                Destroy(instantiatedEffect, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(instantiatedEffect, 1f);
        }
    }
}
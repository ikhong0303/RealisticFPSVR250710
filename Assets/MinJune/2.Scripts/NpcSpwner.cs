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

    [Header("스폰 시간 조절")]
    public float delayBetweenSpawns = 0.5f;
    public float npcSpawnDelayAfterEffectStart = 0.1f;

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasSpawned) return;

        if (other.CompareTag(playerTag))
        {
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

        // **여기서 적 카운트 증가!**
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

using UnityEngine;
using System.Collections;

public class NpcSpawner : MonoBehaviour
{
    [Header("스폰할 NPC 프리팹")]
    public GameObject npcPrefab;

    [Header("등장 이펙트(파티클) 프리팹")]
    public GameObject spawnEffectPrefab;

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    [Header("스폰 위치들")]
    public Transform[] spawnPoints;

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
        foreach (Transform point in spawnPoints)
        {
            yield return StartCoroutine(SpawnAtPoint(point));
        }
    }

    private IEnumerator SpawnAtPoint(Transform point)
    {
        // 1) 파티클 이펙트 재생
        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, point.position, point.rotation);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                yield return new WaitWhile(() => ps.IsAlive(true));
            }
            else
            {
                // 파티클이 없는 경우 안전 대기
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 2) NPC 생성
        if (npcPrefab != null)
        {
            Instantiate(npcPrefab, point.position, point.rotation);
        }
    }
}
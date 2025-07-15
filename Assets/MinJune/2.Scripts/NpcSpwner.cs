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
        // 1) 파티클 이펙트 재생
        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, transform.position, transform.rotation);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                // 파티클 종료 대기
                yield return new WaitWhile(() => ps.IsAlive(true));
            }
        }

        // 2) NPC 생성
        if (npcPrefab != null)
        {
            Instantiate(npcPrefab, transform.position, transform.rotation);
        }
    }
}
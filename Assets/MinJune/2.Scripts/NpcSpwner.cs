using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List를 사용하기 위해 추가

public class NpcSpawner : MonoBehaviour
{
    // 각 스폰 지점마다 스폰할 NPC 정보를 담는 클래스
    [System.Serializable] // 인스펙터에 노출되도록 직렬화
    public class SpawnEntry
    {
        public Transform spawnPoint; // NPC가 스폰될 위치
        public GameObject npcPrefab;  // 이 위치에 스폰될 특정 NPC 프리팹
    }

    [Header("스폰할 NPC 및 위치 정보")]
    public List<SpawnEntry> spawnEntries; // 여러 스폰 지점과 NPC 프리팹 쌍을 담을 리스트

    [Header("등장 이펙트(파티클) 프리팹")]
    public GameObject spawnEffectPrefab;

    [Header("스폰 시간 조절")]
    [Tooltip("각 NPC 스폰 지점 사이의 지연 시간 (초). 이 값을 줄이면 다음 NPC가 더 빨리 스폰됩니다.")]
    public float delayBetweenSpawns = 0.5f; // 기본값: 0.5초
    [Tooltip("등장 이펙트 시작 후 NPC가 스폰될 때까지의 지연 시간 (초). 이 값을 줄이면 NPC가 이펙트와 동시에 혹은 더 빠르게 나타납니다. 0이면 이펙트 시작과 동시에 스폰.")]
    public float npcSpawnDelayAfterEffectStart = 0.1f; // 기본값: 0.1초

    [Header("플레이어 태그")]
    public string playerTag = "Player";

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasSpawned) return; // 이미 스폰했다면 리턴

        // 플레이어 태그와 충돌했는지 확인
        if (other.CompareTag(playerTag))
        {
            StartCoroutine(SpawnRoutine());
            hasSpawned = true; // 스폰 시작 플래그 설정
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // 모든 스폰 엔트리를 순회하며 NPC 스폰
        foreach (SpawnEntry entry in spawnEntries)
        {
            // 각 스폰 지점과 해당 NPC 프리팹을 SpawnAtPoint 코루틴에 전달
            yield return StartCoroutine(SpawnAtPoint(entry.spawnPoint, entry.npcPrefab));

            // 다음 NPC 스폰 지점으로 넘어가기 전에 지연 시간 적용
            if (delayBetweenSpawns > 0)
            {
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }

        // 스폰이 모두 끝난 후 스포너 트리거를 비활성화 (선택 사항)
        // GetComponent<Collider>().enabled = false; 
    }

    private IEnumerator SpawnAtPoint(Transform point, GameObject npcToSpawn)
    {
        GameObject instantiatedEffect = null;
        ParticleSystem ps = null;

        // 1) 파티클 이펙트 재생
        if (spawnEffectPrefab != null)
        {
            instantiatedEffect = Instantiate(spawnEffectPrefab, point.position, point.rotation);
            ps = instantiatedEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            else
            {
                Debug.LogWarning("SpawnEffectPrefab에 ParticleSystem 컴포넌트가 없습니다.");
            }
        }

        // 2) 이펙트 시작 후 NPC 스폰까지의 지연 시간 적용
        // 이 값을 줄여서 NPC가 더 빨리 나타나게 할 수 있습니다.
        if (npcSpawnDelayAfterEffectStart > 0)
        {
            yield return new WaitForSeconds(npcSpawnDelayAfterEffectStart);
        }
        // 만약 npcSpawnDelayAfterEffectStart가 0이거나 이펙트가 없다면, 즉시 다음 단계로 넘어감

        // 3) NPC 생성
        if (npcToSpawn != null)
        {
            Instantiate(npcToSpawn, point.position, point.rotation);
        }
        else
        {
            Debug.LogWarning($"SpawnPoint {point.name}에 할당된 NPC 프리팹이 없습니다. 이 지점에서는 NPC를 스폰하지 않습니다.");
        }

        // 4) 이펙트 오브젝트 파괴 (파티클 재생이 끝난 후)
        if (instantiatedEffect != null)
        {
            if (ps != null)
            {
                // 파티클 시스템의 전체 재생 시간 + 남은 수명만큼 기다린 후 파괴
                Destroy(instantiatedEffect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // ParticleSystem이 없는 경우 기본 1초 후 파괴
                Destroy(instantiatedEffect, 1f);
            }
        }
    }
}
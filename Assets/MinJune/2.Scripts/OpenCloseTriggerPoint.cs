using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorUnit
{
    public Transform door;
    public Vector3 targetPosition;
    public float moveSpeed = 2f;

    [HideInInspector] public Vector3 closedPosition;
}

public class OpenCloseTriggerPoint : MonoBehaviour
{
    [Header("모드 설정")]
    public bool isOpenMode = true; // true: 열기, false: 닫기

    [Header("문 리스트")]
    public List<DoorUnit> doors = new List<DoorUnit>();

    [Header("파티클 프리팹")]
    public ParticleSystem defaultParticlePrefab;
    public ParticleSystem triggeredParticlePrefab;

    [Header("파티클 소환 위치")]
    public Transform particleSpawnPoint; // 파티클 소환 위치 (없으면 트리거 위치)

    private ParticleSystem currentParticle; // 현재 소환된 파티클

    private bool isTriggered = false; // 트리거 상태 유지용

    private void Awake()
    {
        foreach (var d in doors)
            if (d.door != null)
                d.closedPosition = d.door.position;

        SpawnDefaultParticle();
    }

    void SpawnDefaultParticle()
    {
        DestroyCurrentParticleSmooth();

        if (defaultParticlePrefab)
        {
            Transform spawnPoint = particleSpawnPoint ? particleSpawnPoint : transform;
            currentParticle = Instantiate(defaultParticlePrefab, spawnPoint.position, spawnPoint.rotation);
            currentParticle.Play();
        }
    }

    void SpawnTriggeredParticle()
    {
        DestroyCurrentParticleSmooth();

        if (triggeredParticlePrefab)
        {
            Transform spawnPoint = particleSpawnPoint ? particleSpawnPoint : transform;
            currentParticle = Instantiate(triggeredParticlePrefab, spawnPoint.position, spawnPoint.rotation);
            currentParticle.Play();
        }
    }

    void DestroyCurrentParticleSmooth()
    {
        if (currentParticle)
        {
            currentParticle.Stop();
            Destroy(currentParticle.gameObject, currentParticle.main.duration + currentParticle.main.startLifetime.constantMax);
            currentParticle = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isTriggered) return; // 이미 트리거 상태면 무시

        isTriggered = true;
        SpawnTriggeredParticle();

        StopAllCoroutines();

        if (isOpenMode)
            StartCoroutine(OpenDoors());
        else
            StartCoroutine(CloseDoors());
    }

    private void OnTriggerExit(Collider other)
    {
        // 아무 것도 하지 않음 (트리거에서 벗어나도 파티클 상태 유지)
    }

    // ------------------- 문 열기 -------------------
    IEnumerator OpenDoors()
    {
        if (doors.Count < 3)
        {
            // 문이 3개 미만이면 모두 동시에 열기
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                if (d.door != null)
                    StartCoroutine(MoveDoor(d.door, d.door.position, d.targetPosition, d.moveSpeed));
            }
            yield break;
        }

        // 0,1번 문을 동시에 열기
        bool finished0 = false;
        bool finished1 = false;

        StartCoroutine(MoveDoorWithFlag(doors[0].door, doors[0].door.position, doors[0].targetPosition, doors[0].moveSpeed, () => finished0 = true));
        StartCoroutine(MoveDoorWithFlag(doors[1].door, doors[1].door.position, doors[1].targetPosition, doors[1].moveSpeed, () => finished1 = true));

        // 두 문이 모두 완료될 때까지 대기
        yield return new WaitUntil(() => finished0 && finished1);

        // 2번 문 열기 (GetComponent 없이 바로 DoorUnit 정보 사용!)
        if (doors[2].door != null)
            yield return MoveDoor(doors[2].door, doors[2].door.position, doors[2].targetPosition, doors[2].moveSpeed);
    }

    // ------------------- 문 닫기 -------------------
    IEnumerator CloseDoors()
    {
        if (doors.Count < 3)
        {
            // 문이 3개 미만이면 모두 동시에 닫기
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                if (d.door != null)
                    StartCoroutine(MoveDoor(d.door, d.door.position, d.closedPosition, d.moveSpeed));
            }
            yield break;
        }

        // 0,1번 문을 동시에 닫기
        bool finished0 = false;
        bool finished1 = false;

        StartCoroutine(MoveDoorWithFlag(doors[0].door, doors[0].door.position, doors[0].closedPosition, doors[0].moveSpeed, () => finished0 = true));
        StartCoroutine(MoveDoorWithFlag(doors[1].door, doors[1].door.position, doors[1].closedPosition, doors[1].moveSpeed, () => finished1 = true));

        // 두 문이 모두 완료될 때까지 대기
        yield return new WaitUntil(() => finished0 && finished1);

        // 2번 문 닫기 (GetComponent 없이 바로 DoorUnit 정보 사용!)
        if (doors[2].door != null)
            yield return MoveDoor(doors[2].door, doors[2].door.position, doors[2].closedPosition, doors[2].moveSpeed);
    }

    // ------------------- MoveDoor 코루틴 (flag 호출용) -------------------
    IEnumerator MoveDoorWithFlag(Transform door, Vector3 from, Vector3 to, float speed, System.Action onFinished)
    {
        yield return MoveDoor(door, from, to, speed);
        onFinished?.Invoke();
    }

    // ------------------- 실제 문 이동 코루틴 -------------------
    IEnumerator MoveDoor(Transform door, Vector3 from, Vector3 to, float speed)
    {
        float dist = Vector3.Distance(from, to);
        float duration = dist / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (door)
                door.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (door)
            door.position = to;
    }
}
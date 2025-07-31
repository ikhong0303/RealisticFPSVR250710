using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorUnit
{
    public Transform door;
    public Transform targetPosition;
    public float moveSpeed = 2f;

    [Header("문 열림 연기 파티클")]
    public ParticleSystem smokeParticlePrefab;
    public Transform smokeSpawnPoint;

    public enum ParticleTiming { AtStart, AtMiddle, AtEnd }
    [Tooltip("파티클이 나올 타이밍 선택 (시작/중간/끝)")]
    public ParticleTiming smokeParticleTiming = ParticleTiming.AtStart;

    [Header("문 열림 사운드")]
    public AudioClip doorOpenSFX; // 🔊 각 문별 효과음 커스터마이즈도 가능 (없으면 AudioManager 기본 사용)

    [HideInInspector] public Vector3 closedPosition;
}

public class OpenCloseTriggerPoint : MonoBehaviour
{
    [Header("모드 설정")]
    public bool isOpenMode = true;

    [Header("문 리스트")]
    public List<DoorUnit> doors = new List<DoorUnit>();

    [Header("파티클 프리팹")]
    public ParticleSystem defaultParticlePrefab;
    public ParticleSystem triggeredParticlePrefab;

    [Header("파티클 소환 위치")]
    public Transform particleSpawnPoint;

    [Header("문열림 기본 사운드 (AudioManager의 sfxNames에서 'DoorOpen'과 맞춰야 함)")]
    public string doorOpenSFXName = "DoorOpen"; // 🔊 이 이름으로 AudioManager에서 호출

    private ParticleSystem currentParticle;
    private bool isTriggered = false;

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
        Debug.Log("인식");
        if (isTriggered) return;
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
        // 아무것도 안함
    }

    IEnumerator OpenDoors()
    {
        if (doors.Count < 3)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                if (d.door != null)
                {
                    // 🔊 [추가] 문별 사운드 재생
                    PlayDoorOpenSound(d);
                    StartCoroutine(MoveDoorWithSmoke(d.door, d.door.position, d.targetPosition.position, d.moveSpeed, d.smokeParticlePrefab, d.smokeSpawnPoint, d.smokeParticleTiming));
                }
            }
            yield break;
        }

        bool finished0 = false, finished1 = false;
        // 🔊 [추가] 0번, 1번 문 동시에 소리
        PlayDoorOpenSound(doors[0]);
        StartCoroutine(MoveDoorWithSmokeAndFlag(doors[0], () => finished0 = true));
        PlayDoorOpenSound(doors[1]);
        StartCoroutine(MoveDoorWithSmokeAndFlag(doors[1], () => finished1 = true));
        yield return new WaitUntil(() => finished0 && finished1);

        if (doors[2].door != null)
        {
            PlayDoorOpenSound(doors[2]);
            yield return MoveDoorWithSmoke(doors[2].door, doors[2].door.position, doors[2].targetPosition.position, doors[2].moveSpeed, doors[2].smokeParticlePrefab, doors[2].smokeSpawnPoint, doors[2].smokeParticleTiming);
        }
    }

    IEnumerator CloseDoors()
    {
        if (doors.Count < 3)
        {
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                if (d.door != null)
                    StartCoroutine(MoveDoor(d.door, d.door.position, d.closedPosition, d.moveSpeed));
            }
            yield break;
        }

        bool finished0 = false, finished1 = false;
        StartCoroutine(MoveDoorWithFlag(doors[0].door, doors[0].door.position, doors[0].closedPosition, doors[0].moveSpeed, () => finished0 = true));
        StartCoroutine(MoveDoorWithFlag(doors[1].door, doors[1].door.position, doors[1].closedPosition, doors[1].moveSpeed, () => finished1 = true));
        yield return new WaitUntil(() => finished0 && finished1);

        if (doors[2].door != null)
            yield return MoveDoor(doors[2].door, doors[2].door.position, doors[2].closedPosition, doors[2].moveSpeed);
    }

    // ---------------------- 문 이동 + 파티클 (flag) ----------------------
    IEnumerator MoveDoorWithSmokeAndFlag(DoorUnit d, System.Action onFinished)
    {
        yield return MoveDoorWithSmoke(d.door, d.door.position, d.targetPosition.position, d.moveSpeed, d.smokeParticlePrefab, d.smokeSpawnPoint, d.smokeParticleTiming);
        onFinished?.Invoke();
    }

    // ---------------------- 실제 문 이동 + 파티클 ----------------------
    IEnumerator MoveDoorWithSmoke(Transform door, Vector3 from, Vector3 to, float speed, ParticleSystem smokeParticlePrefab, Transform smokeSpawnPoint, DoorUnit.ParticleTiming timing)
    {
        float dist = Vector3.Distance(from, to);
        float duration = dist / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        bool particleSpawned = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            if (smokeParticlePrefab && !particleSpawned)
            {
                bool spawnNow = false;
                switch (timing)
                {
                    case DoorUnit.ParticleTiming.AtStart:
                        spawnNow = (t >= 0f);
                        break;
                    case DoorUnit.ParticleTiming.AtMiddle:
                        spawnNow = (t >= 0.5f);
                        break;
                    case DoorUnit.ParticleTiming.AtEnd:
                        spawnNow = (t >= 1f);
                        break;
                }
                if (spawnNow)
                {
                    Transform spawnPoint = smokeSpawnPoint ? smokeSpawnPoint : door;
                    ParticleSystem smoke = Object.Instantiate(smokeParticlePrefab, spawnPoint.position, spawnPoint.rotation);
                    smoke.Play();
                    Object.Destroy(smoke.gameObject, smoke.main.duration + smoke.main.startLifetime.constantMax);
                    particleSpawned = true;
                }
            }

            if (door)
                door.position = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (door)
            door.position = to;

        // 만약 AtEnd인데 마지막에 도착해서도 아직 파티클 안나왔으면 생성
        if (smokeParticlePrefab && !particleSpawned && timing == DoorUnit.ParticleTiming.AtEnd)
        {
            Transform spawnPoint = smokeSpawnPoint ? smokeSpawnPoint : door;
            ParticleSystem smoke = Object.Instantiate(smokeParticlePrefab, spawnPoint.position, spawnPoint.rotation);
            smoke.Play();
            Object.Destroy(smoke.gameObject, smoke.main.duration + smoke.main.startLifetime.constantMax);
        }
    }

    IEnumerator MoveDoorWithFlag(Transform door, Vector3 from, Vector3 to, float speed, System.Action onFinished)
    {
        yield return MoveDoor(door, from, to, speed);
        onFinished?.Invoke();
    }

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

    // 🔊 문 오픈 사운드 처리 (문별로 AudioClip 있으면 그거 사용, 아니면 AudioManager)
    void PlayDoorOpenSound(DoorUnit d)
    {
        if (d.doorOpenSFX != null)
        {
            // 문별 오디오소스가 있다면 직접 재생 (필요하면 문에 AudioSource 붙여서 PlayOneShot 가능)
            // 예: (없으면 AudioManager.Instance.PlaySFX 사용)
            AudioSource.PlayClipAtPoint(d.doorOpenSFX, d.door.position, 1.0f);
        }
        else
        {
            // AudioManager에서 이름 기준으로 재생 (3D)
            if (!string.IsNullOrEmpty(doorOpenSFXName))
                AudioManager.Instance.PlaySFX(doorOpenSFXName, d.door.position);
        }
    }
}
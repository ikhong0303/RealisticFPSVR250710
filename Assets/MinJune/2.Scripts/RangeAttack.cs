using UnityEngine;
using System.Collections;

/// <summary>
/// VR 환경에서 지정된 타겟 위치를 추적한 뒤,
/// 일정 범위(거리) 내에 들어오면:
///   1) 목표 각도 이내로 바라보면 파티클 생성
///   2) 파티클 충돌 시에만 플레이어에게 데미지를 부여
///   3) 설정된 시간 후 파티클 자동 정지 및 쿨타임 후 재발사
/// </summary>
public class RangeAttack : MonoBehaviour
{
    [Header("추적 대상 설정")]
    [Tooltip("NPC가 바라볼 대상 Transform (없으면 MainCamera를 사용)")]
    public Transform trackingTarget;

    [Header("발사 범위 설정")]
    [Tooltip("파티클 발사 가능 최대 거리 (유닛 단위)")]
    public float fireRange = 10f;

    [Header("추적 설정")]
    [Tooltip("NPC가 대상 방향으로 회전하는 속도 (값이 클수록 빠름)")]
    public float rotationSpeed = 5f;
    [Tooltip("목표 인정 각도 차이 (도)")]
    public float fireAngleThreshold = 5f;

    [Header("발사 지점 설정")]
    [Tooltip("파티클이 생성될 위치 Transform")]
    public Transform firePoint;
    [Tooltip("firePoint 미설정 시 NPC 자신의 위치 사용")] public bool useFallbackToSelf = true;

    [Header("파티클 설정")]
    [Tooltip("발사할 파티클 시스템 프리팹")]
    public ParticleSystem particlePrefab;
    [Tooltip("파티클이 유지될 시간 (초)")]
    public float particleDuration = 2f;
    [Tooltip("파티클 재발사 전 대기 시간 (초)")]
    public float fireCooldown = 5f;

    [Header("데미지 설정")]
    [Tooltip("플레이어에게 줄 데미지량")]
    public int damageAmount = 10;
    [Tooltip("데미지 재적용 최소 간격 (초)")]
    public float damageInterval = 1f;

    private Transform currentTarget;
    private VRPlayerController playerController;
    private bool isCoolingDown = false;

    void Start()
    {
        // 추적 대상 설정
        if (trackingTarget != null)
        {
            currentTarget = trackingTarget;
        }
        else
        {
            var headObj = GameObject.FindWithTag("MainCamera");
            if (headObj != null)
                currentTarget = headObj.transform;
            else
                Debug.LogWarning("추적 대상이 설정되지 않았고, MainCamera도 찾을 수 없습니다.");
        }

        // 데미지 처리 컴포넌트 조회
        if (currentTarget != null)
        {
            playerController = currentTarget.GetComponentInParent<VRPlayerController>();
            if (playerController == null)
                Debug.LogWarning("VRPlayerController를 찾을 수 없습니다. (Parent 계층에서)");
        }

        // firePoint 설정
        if (firePoint == null && useFallbackToSelf)
            firePoint = this.transform;
    }

    void Update()
    {
        if (currentTarget == null || isCoolingDown)
            return;

        // 범위 검사
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > fireRange)
            return;

        // 방향 및 회전
        Vector3 dir = (currentTarget.position - transform.position).normalized;
        if (dir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        // 범위 내 각도 검증 후 파티클 발사
        if (Quaternion.Angle(transform.rotation, targetRot) <= fireAngleThreshold)
            StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        isCoolingDown = true;

        // 발사 위치 및 회전
        Vector3 spawnPos = firePoint.position;
        Quaternion spawnRot = firePoint.rotation;

        // 파티클 인스턴스 생성
        var psInstance = Instantiate(particlePrefab, spawnPos, spawnRot);
        var col = psInstance.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.sendCollisionMessages = true;

        // 데미지 핸들러 추가
        var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
        dmgHandler.damageAmount = damageAmount;
        dmgHandler.damageInterval = damageInterval;
        dmgHandler.playerController = playerController;

        psInstance.Play();

        // 파티클 유지 후 정지·파괴
        yield return new WaitForSeconds(particleDuration);
        psInstance.Stop();
        Destroy(psInstance.gameObject, psInstance.main.startLifetime.constantMax);

        // 재발사 쿨타임
        yield return new WaitForSeconds(fireCooldown);
        isCoolingDown = false;
    }
}

/// <summary>
/// 파티클 충돌 시 한 번만 데미지 적용 컴포넌트
/// </summary>
public class ParticleDamageOnCollision : MonoBehaviour
{
    [HideInInspector] public int damageAmount;
    [HideInInspector] public float damageInterval;
    [HideInInspector] public VRPlayerController playerController;
    private bool canDamage = true;

    void OnParticleCollision(GameObject other)
    {
        if (!canDamage || !other.CompareTag("Player")) return;
        playerController?.CalculateHP(-damageAmount, damageInterval);
        StartCoroutine(DamageCooldown());
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageInterval);
        canDamage = true;
    }
}

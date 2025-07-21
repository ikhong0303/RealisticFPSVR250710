using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

// MikeNspired.XRIStarterKit 네임스페이스를 사용하지 않는다면 제거해도 무방합니다.
// using MikeNspired.XRIStarterKit; 
// public class NpcRangedController : MonoBehaviour, IEnemy // IEnemy도 사용하지 않으면 제거

public class NpcRangedController : MonoBehaviour
{
    public enum NpcMode { patrol, attack, death }

    [Header("NPC 상태")]
    public NpcMode npcMode = NpcMode.patrol;

    [Header("순찰 및 감지")]
    public float patrolSpeed = 2f;
    public float searchDistance = 10f;
    public float fieldOfViewAngle = 100f;

    [Header("원거리 공격 파티클 설정")]
    public Transform firePoint; // 파티클 발사 지점
    public ParticleSystem particlePrefab; // 발사할 파티클 프리5
    public float particleDuration = 2f; // 발사된 파티클의 시각적 지속 시간
    public float fireCooldown = 2f; // 공격과 공격 사이의 쿨다운 시간

    [Header("파티클 데미지 설정")]
    public int particleDamageAmount = 10; // 파티클이 줄 데미지 (양수 값으로 입력)
    public float particleDamageInterval = 1f; // 데미지 중복 방지 간격

    // [참고: 필요하다면 아래 변수들 다시 활성화]
    // public float deathEffectDelay = 2f; 
    // private EnemyHealth enemyHealth;
    // private DissolveEffect dissolveEffect;

    private NavMeshAgent nav;
    private Animator anim;
    private Transform playerCamera;
    private VRPlayerController playerControllerRef; // 플레이어 컨트롤러 참조

    private float nextFireTime = 0f; // 다음 공격 가능한 시간
    private bool lockedInAttackMode = false; // 플레이어를 한 번 발견하면 공격 모드에 영구 고정

    private void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        // enemyHealth = GetComponent<EnemyHealth>(); // 필요시 활성화
        // dissolveEffect = GetComponentInChildren<DissolveEffect>(); // 필요시 활성화

        if (firePoint == null)
        {
            firePoint = transform;
            Debug.LogWarning(gameObject.name + ": Fire Point가 설정되지 않아 NPC 자신을 발사 지점으로 사용합니다.");
        }
    }

    private void Start()
    {
        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
        {
            playerCamera = origin.Camera.transform;
            // VRPlayerController는 XROrigin의 부모 또는 조상에 있을 수 있습니다.
            playerControllerRef = playerCamera.GetComponentInParent<VRPlayerController>();
            if (playerControllerRef == null)
            {
                Debug.LogWarning("VRPlayerController를 찾을 수 없습니다. 파티클 데미지 적용에 문제가 있을 수 있습니다.");
            }
        }
        else
        {
            Debug.LogWarning("Player XROrigin 또는 Player Camera Transform을 찾을 수 없습니다. NPC가 플레이어를 추적하지 못합니다.");
        }

        // [참고: 필요하다면 데미지, 죽음 처리 이벤트 연결 다시 추가]
        // if (enemyHealth != null)
        // {
        //     enemyHealth.OnTakeDamage += _ =>
        //     {
        //         if (npcMode != NpcMode.death && Random.value <= 0.1f)
        //             anim.SetTrigger("damage");
        //     };
        // }
    }

    private void Update()
    {
        if (playerCamera == null || npcMode == NpcMode.death)
            return;

        float distance = Vector3.Distance(transform.position, playerCamera.position);
        Vector3 dir = (playerCamera.position - transform.position).normalized;
        dir.y = 0;
        float angle = Vector3.Angle(transform.forward, dir);

        bool blocked = Physics.Linecast(transform.position + Vector3.up,
                                        playerCamera.position,
                                        out RaycastHit hit) && !hit.collider.CompareTag("Player");

        bool playerDetected = (distance < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked);

        // 한 번이라도 공격 모드에 진입했다면 영구적으로 공격 모드를 유지
        if (playerDetected || lockedInAttackMode)
        {
            if (npcMode != NpcMode.attack)
            {
                npcMode = NpcMode.attack;
                nav.ResetPath();
                lockedInAttackMode = true; // 영구 공격 모드 플래그 활성화
            }

            nav.isStopped = true;
            FaceTarget(playerCamera.position);

            // 쿨다운이 끝났을 때만 공격 시도
            if (Time.time >= nextFireTime)
            {
                AttackBehavior(); // 공격 애니메이션 트리거 및 파티클 발사
                nextFireTime = Time.time + fireCooldown; // 다음 공격 가능 시간 설정
            }
        }
        else // 플레이어 감지 실패 & 아직 공격 모드에 고정되지 않았을 때 (초기 순찰 상태)
        {
            if (npcMode != NpcMode.patrol)
            {
                npcMode = NpcMode.patrol;
            }

            PatrolMove();
        }
    }

    private void PatrolMove()
    {
        nav.speed = patrolSpeed;
        nav.isStopped = false;

        if (!nav.hasPath && RandomPoint(transform.position, 10f, out Vector3 nextPoint))
            nav.SetDestination(nextPoint);
    }

    private void AttackBehavior()
    {
        nav.ResetPath();
        nav.isStopped = true;
        FaceTarget(playerCamera.position);
        anim.SetTrigger("attack");

        FireParticle(); // 애니메이션 트리거 직후 파티클 발사 및 데미지 로직 처리
    }

    // 이 함수는 애니메이션 이벤트에 연결할 필요가 없습니다.
    public void OnAttackAnimationEnd()
    {
        // 필요하다면 여기에 애니메이션 종료 후의 추가 로직을 구현합니다.
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 look = target - transform.position;
        look.y = 0;
        Quaternion rot = Quaternion.LookRotation(look);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
    }

    // 파티클 생성 및 데미지 로직 통합
    private void FireParticle()
    {
        if (particlePrefab == null || firePoint == null)
        {
            Debug.LogWarning("💥 파티클 발사 실패: 파티클 프리팹 또는 발사 지점 없음. 인스펙터 설정을 확인하세요.");
            return;
        }

        var psInstance = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);

        // 파티클 시스템 Collision 모듈 설정
        var col = psInstance.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.sendCollisionMessages = true;
        col.collidesWith = LayerMask.GetMask("Player"); // "Player" 레이어 마스크를 가져옵니다.

        // ParticleDamageOnCollision 스크립트를 파티클 오브젝트에 추가하고 설정
        var dmgHandler = psInstance.gameObject.GetComponent<ParticleDamageOnCollision>();
        if (dmgHandler == null)
        {
            dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
        }

        dmgHandler.damageAmount = particleDamageAmount; // NpcRangedController의 변수 사용
        dmgHandler.damageInterval = particleDamageInterval; // NpcRangedController의 변수 사용
        dmgHandler.playerController = playerControllerRef; // Start()에서 찾은 VRPlayerController 참조 사용

        psInstance.Play();
        Destroy(psInstance.gameObject, particleDuration + psInstance.main.startLifetime.constantMax);
    }

    // [참고: 필요하다면 TakeDamage, Die, DeathRoutine 함수 다시 추가]
    // public void TakeDamage(float damage, GameObject attacker) { enemyHealth?.TakeDamage(damage, attacker); }
    // public void Die() { /* 사망 로직 */ }
    // private IEnumerator DeathRoutine() { /* 사망 코루틴 */ }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPos = center + Random.insideUnitSphere * range;
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
}
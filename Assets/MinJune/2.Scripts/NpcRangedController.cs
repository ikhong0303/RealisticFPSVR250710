using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;
using MikeNspired.XRIStarterKit;

[RequireComponent(typeof(EnemyHealth))]
public class NpcRangedController : MonoBehaviour, IEnemy
{
    [Header("감지 및 순찰")]
    public float searchDistance = 10f;
    public float fieldOfViewAngle = 100f;
    public float patrolSpeed = 2f;

    [Header("공격 설정")]
    public Transform firePoint;
    public ParticleSystem particlePrefab;
    public float particleDuration = 2f;
    public float fireCooldown = 2f;

    [Header("데미지 설정")]
    public int damageAmount = 10;
    public float damageInterval = 1f;

    [Header("회전 속도")]
    public float rotationSpeed = 5f;

    private NavMeshAgent nav;
    private Animator anim;
    private Transform playerCamera;
    private VRPlayerController playerController;
    private EnemyHealth health;

    private float nextFireTime = 0f;
    private bool lockedInAttackMode = false;

    private void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<EnemyHealth>();

        if (firePoint == null)
            firePoint = transform;
    }

    private void Start()
    {
        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
        {
            playerCamera = origin.Camera.transform;
            playerController = playerCamera.GetComponentInParent<VRPlayerController>();
        }
    }

    private void Update()
    {
        if (playerCamera == null || health == null) return;

        float dist = Vector3.Distance(transform.position, playerCamera.position);
        Vector3 dir = (playerCamera.position - transform.position).normalized;
        dir.y = 0f;
        float angle = Vector3.Angle(transform.forward, dir);

        bool blocked = Physics.Linecast(
            transform.position + Vector3.up,
            playerCamera.position,
            out RaycastHit hit) && !hit.collider.CompareTag("Player");

        bool detected = (dist < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked);

        if (detected || lockedInAttackMode)
        {
            lockedInAttackMode = true;
            nav.isStopped = true;
            nav.ResetPath();
            FaceTarget(dir);

            if (Time.time >= nextFireTime)
            {
                anim?.SetTrigger("attack");
                Fire();
                nextFireTime = Time.time + fireCooldown;
            }
        }
        else
        {
            nav.isStopped = false;
            Patrol();
        }
    }

    private void Patrol()
    {
        nav.speed = patrolSpeed;
        if (!nav.hasPath && RandomPoint(transform.position, searchDistance, out Vector3 pt))
            nav.SetDestination(pt);
    }

    private void FaceTarget(Vector3 dir)
    {
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotationSpeed);
    }

    private void Fire()
    {
        var ps = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);
        ps.Play();

        // 데미지 핸들러 연결
        var dmg = ps.gameObject.AddComponent<ParticleDamageOnCollision>();
        dmg.damageAmount = damageAmount;
        dmg.damageInterval = damageInterval;
        dmg.playerController = playerController;

        Destroy(ps.gameObject, particleDuration + ps.main.startLifetime.constantMax);
    }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 rand = center + Random.insideUnitSphere * range;
            if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = center;
        return false;
    }

    /// <summary>
    /// IEnemy 구현: EnemyHealth 에서 호출됩니다.
    /// </summary>
    public void Die()
    {
        // (여기에 사망 애니메이션/이펙트 추가 가능)
        Destroy(gameObject);
    }
}

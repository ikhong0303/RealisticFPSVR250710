using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class NpcRangedController : MonoBehaviour
{
    public float searchDistance = 10f;
    public float fieldOfViewAngle = 100f;

    public float patrolSpeed = 2f;
    public float fireCooldown = 5f;

    public Transform firePoint;
    public ParticleSystem particlePrefab;

    private float nextFireTime = 0f;

    private NavMeshAgent nav;
    private Transform player;
    private Animator anim;

    void Start()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
            player = origin.Camera.transform;

        if (firePoint == null)
            firePoint = transform; // fallback
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        bool blocked = Physics.Linecast(transform.position + Vector3.up, player.position, out RaycastHit hit)
                       && !hit.collider.CompareTag("Player");

        if (distance < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked)
        {
            nav.isStopped = true;
            FaceTarget(player.position);

            if (Time.time >= nextFireTime)
            {
                anim.SetTrigger("attack");
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

    void Patrol()
    {
        nav.speed = patrolSpeed;
        if (!nav.hasPath && RandomPoint(transform.position, 10f, out Vector3 next))
        {
            nav.SetDestination(next);
        }
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 look = target - transform.position;
        look.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
    }

    void Fire()
    {
        if (particlePrefab == null || firePoint == null)
        {
            Debug.LogWarning("💥 파티클 발사 실패: 프리팹 또는 위치 없음");
            return;
        }

        var ps = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
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

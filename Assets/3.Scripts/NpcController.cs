using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;
using MikeNspired.XRIStarterKit;

public enum NpcMode
{
    patrol,
    chase,
    death,
    damage
}

public interface IDamageable
{
    void TakeDamage(float damage, GameObject attacker);
}

public interface IEnemy
{
    void Die();
}

public class NpcController : MonoBehaviour, IDamageable, IEnemy
{
    [Header("기본 능력치")]
    public int attackPower = 1;

    [Header("이동 및 탐지 설정")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float searchDistance = 10f;
    public float fieldOfViewAngle = 100f;

    private NavMeshAgent navMeshAgent;
    private Animator anim;
    private Transform playerCamera;
    private float distance;
    public NpcMode npcMode = NpcMode.patrol;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>(); // 연결 핵심
    }

    private void Start()
    {
        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
            playerCamera = origin.Camera.transform;
    }

    private void Update()
    {
        if (playerCamera == null || npcMode == NpcMode.death) return;

        Vector2 npcXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(playerCamera.position.x, playerCamera.position.z);
        distance = Vector2.Distance(npcXZ, playerXZ);

        Vector3 directionToPlayer = (playerCamera.position - transform.position).normalized;
        directionToPlayer.y = 0;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        bool blocked = Physics.Linecast(
            transform.position + Vector3.up * 1f,
            playerCamera.position + Vector3.up * 0.5f,
            out RaycastHit hit
        ) && !hit.collider.CompareTag("Player");

        if (distance < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked)
        {
            npcMode = NpcMode.chase;
            ChaseMove(playerCamera.position);
        }
        else
        {
            npcMode = NpcMode.patrol;
            PatrolMove();
        }
    }

    private void PatrolMove()
    {
        navMeshAgent.speed = patrolSpeed;

        if (!navMeshAgent.hasPath)
        {
            if (RandomPoint(transform.position, 10f, out Vector3 randomPoint))
                navMeshAgent.SetDestination(randomPoint);
        }
    }

    private void ChaseMove(Vector3 targetPos)
    {
        navMeshAgent.speed = chaseSpeed;

        if (distance < 1.5f)
        {
            navMeshAgent.ResetPath();

            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("punch") && !anim.IsInTransition(0))
                anim.SetTrigger("punch");
        }
        else
        {
            navMeshAgent.SetDestination(targetPos);
            FaceTarget(targetPos);
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 lookPos = target - transform.position;
        lookPos.y = 0;

        if (Vector3.Angle(transform.forward, lookPos) > 5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    // IDamageable 구현 → EnemyHealth의 TakeDamage에서 호출
    public void TakeDamage(float damage, GameObject attacker)
    {
        enemyHealth?.TakeDamage(damage, attacker);
    }

    // IEnemy 구현 → EnemyHealth에서 체력 0이 되면 호출됨
    public void Die()
    {
        if (npcMode == NpcMode.death) return;

        npcMode = NpcMode.death;
        navMeshAgent.ResetPath();
        anim.SetTrigger("death");
        Destroy(gameObject, 2f);
    }

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

    // 무기 태그 충돌 시 데미지 처리 (테스트용)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Weapon"))
        {
            // 예시: enemyHealth.TakeDamage(5, collision.gameObject);
        }
    }
}
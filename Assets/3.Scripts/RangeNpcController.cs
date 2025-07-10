using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public enum RangedNpcState
{
    patrol,
    shoot,
    death
}

public class RangedNpcController : MonoBehaviour
{
    [Header("체력")]
    public int maxHp = 10;
    private int hp;

    [Header("탐지 및 공격 설정")]
    public float searchDistance = 20f;
    public float fieldOfView = 100f;
    public float maxAttackDistance = 15f;
    public float fireCooldown = 3f;
    public float loseSightDelay = 3f; // 시야 잃고도 공격 유지 시간
    private float fireTimer = 0f;
    private float loseSightTimer = 0f;
    private bool isSeeingPlayer = false;

    [Header("이동")]
    public float patrolSpeed = 2f;
    private NavMeshAgent agent;

    [Header("투사체")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("애니메이션")]
    public Animator anim;

    private Transform playerCamera;
    public float distanceToPlayer;
    public RangedNpcState state = RangedNpcState.patrol;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hp = maxHp;
    }

    void Start()
    {
        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
            playerCamera = origin.Camera.transform;
    }

    void Update()
    {
        if (state == RangedNpcState.death || playerCamera == null) return;

        // 거리 및 시야 체크
        Vector2 npcPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerPos = new Vector2(playerCamera.position.x, playerCamera.position.z);
        distanceToPlayer = Vector2.Distance(npcPos, playerPos);

        Vector3 dirToPlayer = (playerCamera.position - transform.position).normalized;
        dirToPlayer.y = 0;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        bool blocked = Physics.Linecast(transform.position + Vector3.up,
                                         playerCamera.position + Vector3.up * 0.5f,
                                         out RaycastHit hit) && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("npc");

        bool canSee = distanceToPlayer <= searchDistance && angle <= fieldOfView * 0.5f && !blocked;

        // 시야 판정에 따른 상태 유지 처리
        if (canSee)
        {
            isSeeingPlayer = true;
            loseSightTimer = 0f;
        }
        else
        {
            loseSightTimer += Time.deltaTime;
            if (loseSightTimer >= loseSightDelay)
            {
                isSeeingPlayer = false;
            }
        }

        // 상태 전이
        switch (state)
        {
            case RangedNpcState.patrol:
                if (isSeeingPlayer)
                {
                    state = RangedNpcState.shoot;
                }
                else
                {
                    Patrol();
                }
                break;

            case RangedNpcState.shoot:
                if (isSeeingPlayer)
                {
                    ShootLogic();
                }
                else
                {
                    state = RangedNpcState.patrol;
                    fireTimer = 0f;
                }
                break;
        }
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.hasPath)
        {
            if (RandomPoint(transform.position, 10f, out Vector3 point))
                agent.SetDestination(point);
        }
    }

    void ShootLogic()
    {
     
        fireTimer += Time.deltaTime;

        agent.ResetPath();
        FacePlayer();

        if (distanceToPlayer <= maxAttackDistance && fireTimer >= fireCooldown)
        {
            fireTimer = 0f;
            if (anim != null)
            {
                anim.SetTrigger("Shoot");
            }
            Invoke("Shoot", 1.5f);
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (playerCamera.position - firePoint.position).normalized;
            rb.linearVelocity = dir * 10f; // projectile speed
        }
    }

    void FacePlayer()
    {
        Vector3 dir = playerCamera.position - transform.position;
        dir.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    public void TakeDamage(int dmg)
    {
        if (state == RangedNpcState.death) return;

        hp -= dmg;
        if (hp <= 0)
        {
            Die();
        }
        else
        {
            if (anim != null) anim.SetTrigger("damage");
        }
    }

    void Die()
    {
        state = RangedNpcState.death;
        agent.ResetPath();
        if (anim != null) anim.SetTrigger("death");
        Destroy(gameObject, 2f);
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
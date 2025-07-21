using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;
using MikeNspired.XRIStarterKit;

[RequireComponent(typeof(EnemyHealth))]
public class NpcController : MonoBehaviour, IEnemy
{
    public enum NpcMode { Patrol, Chase, Death }
    public NpcMode npcMode = NpcMode.Patrol;

    [Header("이동 설정")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float searchDistance = 10f;
    public float fieldOfViewAngle = 100f;

    [Header("근접 공격 설정")]
    public int meleeDamage = 1;
    public float meleeCooldown = 1f;

    [Header("죽음 이펙트")]
    public float deathDelay = 2f;
    public ParticleSystem deathParticlePrefab; // 추가: 죽음 파티클 프리팹
    private DissolveEffect dissolveEffect; // (선택사항)

    private NavMeshAgent nav;
    private Animator anim;
    private Transform playerCamera;
    private VRPlayerController playerController;
    private EnemyHealth health;

    private float nextMeleeTime;

    private void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<EnemyHealth>();
        dissolveEffect = GetComponentInChildren<DissolveEffect>();
    }

    private void Start()
    {
        var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
        if (origin != null)
        {
            playerCamera = origin.Camera.transform;
            playerController = playerCamera.GetComponentInParent<VRPlayerController>();
        }

        health.OnTakeDamage += dmg =>
        {
            if (npcMode != NpcMode.Death)
                anim?.SetTrigger("damage");
        };
    }

    private void Update()
    {
        if (playerCamera == null || npcMode == NpcMode.Death) return;

        float dist = Vector3.Distance(transform.position, playerCamera.position);
        Vector3 dir = (playerCamera.position - transform.position).normalized;
        dir.y = 0f;
        float angle = Vector3.Angle(transform.forward, dir);

        if (dist < searchDistance && angle < fieldOfViewAngle * 0.5f)
        {
            npcMode = NpcMode.Chase;
            Chase(dir);
        }
        else
        {
            npcMode = NpcMode.Patrol;
            Patrol();
        }
    }

    private void Patrol()
    {
        nav.speed = patrolSpeed;
        if (!nav.hasPath && RandomPoint(transform.position, searchDistance, out Vector3 pt))
            nav.SetDestination(pt);
    }

    private void Chase(Vector3 dir)
    {
        nav.speed = chaseSpeed;
        float dist = Vector3.Distance(transform.position, playerCamera.position);

        if (dist <= nav.stoppingDistance && Time.time >= nextMeleeTime)
        {
            nav.ResetPath();
            anim?.SetTrigger("attack");
            nextMeleeTime = Time.time + meleeCooldown;
        }
        else
        {
            nav.SetDestination(playerCamera.position);
            Face(dir);
        }
    }

    private void Face(Vector3 dir)
    {
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 5f);
    }

    // 애니메이션 이벤트에서 호출: 실제 근접 대미지 적용
    public void OnMeleeHit()
    {
        if (playerController != null)
            playerController.CalculateHP(-meleeDamage);
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
        npcMode = NpcMode.Death;
        nav.ResetPath();
        anim?.SetTrigger("death");

        // 죽음 파티클 생성 및 재생
        if (deathParticlePrefab != null) //
        {
            var particle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity); //
            particle.Play(); //
            Destroy(particle.gameObject, particle.main.duration); // 파티클 재생이 끝나면 제거
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);
        if (dissolveEffect != null)
            dissolveEffect.Dissolve();
        yield return new WaitForSeconds(dissolveEffect?.disolveTime ?? 0f);
        Destroy(gameObject);
    }
}
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
    public ParticleSystem deathParticlePrefab;
    private DissolveEffect dissolveEffect;

    [Header("사운드 이름 (AudioManager에서 관리)")]
    public string walkSFXName = "EnemyWalk";    // 걷기(패트롤/체이스 공통)
    public string deathSFXName = "EnemyDie";    // 죽음

    private AudioSource moveAudioSource;
    private NpcMode lastMoveSfxMode = NpcMode.Death; // 상태중복 방지

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

        // 걷기 사운드용 AudioSource 생성
        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.spatialBlend = 1f; // 3D
        moveAudioSource.playOnAwake = false;
        moveAudioSource.loop = true;
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
        if (playerCamera == null || npcMode == NpcMode.Death)
        {
            StopMoveSFX();
            return;
        }

        float dist = Vector3.Distance(transform.position, playerCamera.position);
        Vector3 dir = (playerCamera.position - transform.position).normalized;
        dir.y = 0f;
        float angle = Vector3.Angle(transform.forward, dir);

        if (dist < searchDistance && angle < fieldOfViewAngle * 0.5f)
        {
            npcMode = NpcMode.Chase;
            Chase(dir);
            PlayMoveSFX(NpcMode.Chase);
        }
        else
        {
            npcMode = NpcMode.Patrol;
            Patrol();
            PlayMoveSFX(NpcMode.Patrol);
        }
    }

    private void Patrol()
    {
        nav.speed = patrolSpeed;
        if ((!nav.hasPath || nav.remainingDistance <= 1.5f) && RandomPoint(transform.position, searchDistance, out Vector3 pt))
            nav.SetDestination(pt);
    }

    private void Chase(Vector3 dir)
    {
        nav.speed = chaseSpeed;
        float dist = Vector3.Distance(transform.position, playerCamera.position);

        if (dist <= nav.stoppingDistance + 1f)
        {
            nav.ResetPath();
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("punch") || anim.IsInTransition(0)) return;
            anim?.SetTrigger("attack");
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
    /// 걷기(패트롤/체이스) 지속 사운드
    /// </summary>
    void PlayMoveSFX(NpcMode thisMode)
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(walkSFXName)) return;
        if (lastMoveSfxMode == thisMode && moveAudioSource.isPlaying) return; // 같은 상태+재생중이면 무시

        int idx = AudioManager.Instance.sfxNames.IndexOf(walkSFXName);
        if (idx < 0 || idx >= AudioManager.Instance.sfxClips.Count) return;

        AudioClip clip = AudioManager.Instance.sfxClips[idx];
        moveAudioSource.clip = clip;
        moveAudioSource.volume = AudioManager.Instance.sfxVolume;
        moveAudioSource.Play();
        lastMoveSfxMode = thisMode;
    }

    void StopMoveSFX()
    {
        if (moveAudioSource.isPlaying)
            moveAudioSource.Stop();
        lastMoveSfxMode = NpcMode.Death;
    }

    public void Die()
    {
        npcMode = NpcMode.Death;
        nav.ResetPath();
        anim?.SetTrigger("death");

        // 죽음 파티클 생성 및 재생
        if (deathParticlePrefab != null)
        {
            var particle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, particle.main.duration);
        }

        // 죽음 효과음(단발)
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathSFXName))
            AudioManager.Instance.PlaySFX(deathSFXName, transform.position);

        StopMoveSFX();

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
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

namespace MikeNspired.XRIStarterKit
{
    public class NpcRangedController : MonoBehaviour, IEnemy
    {
        [Header("NPC 상태")]
        public NpcMode npcMode = NpcMode.patrol;

        [Header("능력치")]
        public int attackPower = 1;

        [Header("순찰 및 감지")]
        public float patrolSpeed = 2f;
        public float searchDistance = 10f;
        public float fieldOfViewAngle = 100f;

        // [수정된 부분] RangeAttack 대신 NpcRangeAttack 참조
        [Header("원거리 공격 컴포넌트")]
        public NpcRangeAttack npcRangeAttack; // RangeAttack -> NpcRangeAttack으로 변경

        // [추가된 부분] 애니메이션 이벤트 후 파티클 발사까지의 지연 시간
        [Header("공격 타이밍 조절")]
        [Tooltip("공격 애니메이션 이벤트가 호출된 후 파티클이 발사되기까지의 추가 지연 시간입니다. (초)")]
        public float animationEventToParticleDelay = 0.0f; // 기본값 0

        [Header("죽음 후 Dissolve 지연시간")]
        public float deathEffectDelay = 2f;

        private NavMeshAgent nav;
        private Animator anim;
        private Transform playerCamera;
        private EnemyHealth enemyHealth;
        private DissolveEffect dissolveEffect;

        private bool isAttacking = false; // 공격 애니메이션이 재생 중인지
        private bool isPreparingAttack = false; // 공격 준비 중 (쿨다운 포함)

        private void Awake()
        {
            nav = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<Animator>();
            enemyHealth = GetComponent<EnemyHealth>();
            dissolveEffect = GetComponentInChildren<DissolveEffect>();

            // [수정된 부분] RangeAttack 대신 NpcRangeAttack 컴포넌트 가져오기
            if (npcRangeAttack == null)
                npcRangeAttack = GetComponent<NpcRangeAttack>(); // RangeAttack -> NpcRangeAttack으로 변경
        }

        private void Start()
        {
            var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
            if (origin != null)
                playerCamera = origin.Camera.transform;

            if (enemyHealth != null)
            {
                enemyHealth.OnTakeDamage += _ =>
                {
                    if (npcMode != NpcMode.death && Random.value <= 0.1f)
                        anim.SetTrigger("damage");
                };
            }
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

            // 플레이어 감지 조건
            if (distance < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked)
            {
                if (npcMode != NpcMode.attack) // 공격 모드로 진입
                {
                    npcMode = NpcMode.attack;
                    isPreparingAttack = false; // 초기화
                }

                // 공격 모드일 때, 현재 공격 준비 중이 아니고, NpcRangeAttack이 쿨다운이 아닐 때만 공격 시도
                if (npcMode == NpcMode.attack && !isPreparingAttack && npcRangeAttack != null && !npcRangeAttack.IsOnCooldown) // [수정] rangeAttack -> npcRangeAttack
                {
                    AttackBehavior();
                }
                // 공격 모드일 때는 항상 멈춰있도록 유지
                else if (npcMode == NpcMode.attack)
                {
                    nav.ResetPath();
                    nav.isStopped = true;
                    FaceTarget(playerCamera.position); // 계속 플레이어 응시
                }
            }
            else // 플레이어 감지 실패
            {
                if (npcMode != NpcMode.patrol)
                    npcMode = NpcMode.patrol;

                PatrolMove();
                isAttacking = false;
                isPreparingAttack = false; // 플레이어 놓치면 공격 준비 상태 해제
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
            if (isAttacking || isPreparingAttack) return; // 이미 공격 중이거나 준비 중이면 재진입 방지

            nav.ResetPath();
            nav.isStopped = true; // 공격 중 이동 멈춤

            FaceTarget(playerCamera.position);
            anim.SetTrigger("attack");
            isAttacking = true; // 공격 애니메이션 재생 시작
            isPreparingAttack = true; // 공격 준비 상태 시작
        }

        // 애니메이션 이벤트로 연결 필수 (공격 애니메이션이 파티클 발사 직전 또는 발사 시점에 이 함수를 호출해야 함)
        public void OnAttackAnimationEnd()
        {
            // 애니메이션 이벤트가 호출되면, 딜레이 코루틴 시작
            StartCoroutine(TriggerParticleWithDelay());
        }

        private IEnumerator TriggerParticleWithDelay()
        {
            // NpcRangedController에서 추가된 딜레이
            if (animationEventToParticleDelay > 0)
            {
                yield return new WaitForSeconds(animationEventToParticleDelay);
            }

            // 파티클 발사
            if (npcRangeAttack != null && !npcRangeAttack.IsOnCooldown) // [수정] rangeAttack -> npcRangeAttack
            {
                npcRangeAttack.TriggerFire(); // [수정] rangeAttack -> npcRangeAttack
            }

            // 파티클 발사 후 공격 애니메이션 상태 해제
            isAttacking = false;
            // isPreparingAttack은 npcRangeAttack의 쿨다운이 끝날 때까지 유지될 것임 (Update에서 !IsOnCooldown으로 제어)
        }

        private void FaceTarget(Vector3 target)
        {
            Vector3 look = target - transform.position;
            look.y = 0;
            Quaternion rot = Quaternion.LookRotation(look);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
        }

        public void TakeDamage(float damage, GameObject attacker)
        {
            enemyHealth?.TakeDamage(damage, attacker);
        }

        public void Die()
        {
            if (npcMode == NpcMode.death) return;
            npcMode = NpcMode.death;
            nav.ResetPath();
            nav.isStopped = true;
            anim.SetTrigger("death");
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(deathEffectDelay);

            if (dissolveEffect != null)
                dissolveEffect.Dissolve();

            if (dissolveEffect != null)
                yield return new WaitForSeconds(dissolveEffect.disolveTime);

            Destroy(gameObject);
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
    }
}
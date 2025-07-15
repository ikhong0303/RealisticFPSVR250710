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

        [Header("원거리 공격 컴포넌트")]
        public RangeAttack rangeAttack;

        [Header("죽음 후 Dissolve 지연시간")]
        public float deathEffectDelay = 2f;

        private NavMeshAgent nav;
        private Animator anim;
        private Transform playerCamera;
        private EnemyHealth enemyHealth;
        private DissolveEffect dissolveEffect;

        private bool isAttacking = false;

        private void Awake()
        {
            nav = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<Animator>();
            enemyHealth = GetComponent<EnemyHealth>();
            dissolveEffect = GetComponentInChildren<DissolveEffect>();
            if (rangeAttack == null)
                rangeAttack = GetComponent<RangeAttack>();
        }

        private void Start()
        {
            var origin = GameManager.instance?.player?.GetComponent<XROrigin>();
            if (origin != null)
                playerCamera = origin.Camera.transform;

            if (enemyHealth != null)
                enemyHealth.OnTakeDamage += _ =>
                {
                    if (npcMode != NpcMode.death && Random.value <= 0.1f)
                        anim.SetTrigger("damage");
                };
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

            if (distance < searchDistance && angle < fieldOfViewAngle * 0.5f && !blocked)
            {
                if (npcMode != NpcMode.attack)
                {
                    npcMode = NpcMode.attack;
                    AttackBehavior();
                }
            }
            else
            {
                if (npcMode != NpcMode.patrol)
                    npcMode = NpcMode.patrol;
                PatrolMove();
                isAttacking = false;
            }
        }

        private void PatrolMove()
        {
            nav.speed = patrolSpeed;
            if (!nav.hasPath && RandomPoint(transform.position, 10f, out Vector3 nextPoint))
                nav.SetDestination(nextPoint);
        }

        private void AttackBehavior()
        {
            if (isAttacking) return;

            nav.ResetPath();
            FaceTarget(playerCamera.position);
            anim.SetTrigger("attack");
            isAttacking = true;
        }

        public void OnAttackAnimationEnd()
        {
            isAttacking = false;

            if (rangeAttack != null && !rangeAttack.IsOnCooldown)
                rangeAttack.TriggerFire();
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

using System.Collections;
using UnityEngine;

public class BossTurretShooter : MonoBehaviour
{
    public enum AttackPattern { SingleShot, MultiShot }
    [Header("패턴 선택 (코드/Inspector에서 변경 가능)")]
    public AttackPattern currentPattern = AttackPattern.SingleShot;

    [Header("공통 - 플레이어 추적/회전")]
    public float rotationSpeed = 5f;
    public float fireRange = 20f;

    [Header("쿨타임")]
    public float singleShotCooldown = 5f;
    public float multiShotCooldown = 8f;
    private float nextFireTime = 0f;

    [Header("단발 패턴(레이저)")]
    public Transform[] singleFirePoints;
    public ParticleSystem singleParticlePrefab;
    public float singleParticleDuration = 2f;
    public int singleDamage = 10;
    public float singleDamageInterval = 1f;

    [Header("멀티샷 패턴(연사/동시발사)")]
    public Transform[] multiFirePoints;
    public ParticleSystem multiParticlePrefab;
    public float multiParticleDuration = 1.2f;
    public int multiDamage = 5;
    public float multiDamageInterval = 0.4f;

    [Header("사운드 효과음 이름 (AudioManager)")]
    public string singleShotSFXName = "SingleShot"; // Inspector에서 지정
    public string multiShotSFXName = "MultiShot";   // Inspector에서 지정

    public float minPatternDuration = 5f;
    public float maxPatternDuration = 10f;

    private Transform playerTarget;
    private VRPlayerController playerController;

    private int lastPatternIdx = -1; // 연속 중복 방지용

    void Start()
    {
        var cam = GameObject.FindWithTag("MainCamera");
        if (cam)
        {
            playerTarget = cam.transform;
            playerController = cam.GetComponentInParent<VRPlayerController>();
        }
        StartCoroutine(PatternRoutine());
    }

    void Update()
    {
        if (playerTarget == null) return;

        // 1. 플레이어 회전
        Vector3 toPlayer = playerTarget.position - transform.position;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
        }

        // 2. 쿨타임 & 사거리 체크 후 패턴별 발사
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (Time.time >= nextFireTime && dist <= fireRange)
        {
            if (currentPattern == AttackPattern.SingleShot)
            {
                FireSingle();
                nextFireTime = Time.time + singleShotCooldown;
            }
            else if (currentPattern == AttackPattern.MultiShot)
            {
                FireMulti();
                nextFireTime = Time.time + multiShotCooldown;
            }
        }
    }

    // 단일 발사 패턴
    void FireSingle()
    {
        foreach (Transform fp in singleFirePoints)
        {
            if (fp == null || singleParticlePrefab == null) continue;
            Vector3 dir = (playerTarget.position - fp.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            var psInstance = Instantiate(singleParticlePrefab, fp.position, lookRot);

            // 🔊 단일발사 사운드 (AudioManager에서 관리)
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(singleShotSFXName))
            {
                AudioManager.Instance.PlaySFX(singleShotSFXName, fp.position);
            }

            var col = psInstance.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.sendCollisionMessages = true;
            col.collidesWith = LayerMask.GetMask("Player");

            var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
            dmgHandler.damageAmount = singleDamage;
            dmgHandler.damageInterval = singleDamageInterval;
            dmgHandler.playerController = playerController;

            // 💡 파티클 자동정리
            var autoDestroy = psInstance.gameObject.AddComponent<AutoDestroyIfOwnerDead>();
            autoDestroy.owner = this.gameObject;

            psInstance.Play();
            Destroy(psInstance.gameObject, singleParticleDuration + psInstance.main.startLifetime.constantMax);
        }
    }

    // 멀티샷 패턴
    void FireMulti()
    {
        foreach (Transform fp in multiFirePoints)
        {
            if (fp == null || multiParticlePrefab == null) continue;
            Vector3 dir = (playerTarget.position - fp.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            var psInstance = Instantiate(multiParticlePrefab, fp.position, lookRot);

            // 🔊 멀티샷 사운드 (AudioManager에서 관리)
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(multiShotSFXName))
            {
                AudioManager.Instance.PlaySFX(multiShotSFXName, fp.position);
            }

            var col = psInstance.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.sendCollisionMessages = true;
            col.collidesWith = LayerMask.GetMask("Player");

            var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
            dmgHandler.damageAmount = multiDamage;
            dmgHandler.damageInterval = multiDamageInterval;
            dmgHandler.playerController = playerController;

            // 💡 파티클 자동정리
            var autoDestroy = psInstance.gameObject.AddComponent<AutoDestroyIfOwnerDead>();
            autoDestroy.owner = this.gameObject;

            psInstance.Play();
            Destroy(psInstance.gameObject, multiParticleDuration + psInstance.main.startLifetime.constantMax);
        }
    }

    // 외부(코드 등)에서 패턴 전환 가능
    public void SetPattern(int idx)
    {
        currentPattern = (AttackPattern)idx;
        lastPatternIdx = idx;
        nextFireTime = 0; // 패턴 변경 시 바로 발사 가능하도록 쿨타임 리셋
    }

    // 패턴을 일정 주기마다 "연속 중복 없이" 랜덤 변경
    IEnumerator PatternRoutine()
    {
        while (true)
        {
            int patternCount = System.Enum.GetValues(typeof(AttackPattern)).Length;
            int randomIdx;

            // 이전 패턴과 다를 때까지 랜덤
            do
            {
                randomIdx = Random.Range(0, patternCount);
            } while (randomIdx == lastPatternIdx && patternCount > 1);

            SetPattern(randomIdx);

            float patternDuration = Random.Range(minPatternDuration, maxPatternDuration);
            yield return new WaitForSeconds(patternDuration);
        }
    }
}
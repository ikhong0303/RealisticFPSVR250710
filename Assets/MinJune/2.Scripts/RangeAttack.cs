using UnityEngine;
using System.Collections;

public class RangeAttack : MonoBehaviour
{
    [Header("추적 대상 설정")]
    public Transform trackingTarget;

    [Header("발사 범위 설정")]
    public float fireRange = 10f;

    [Header("추적 설정")]
    public float rotationSpeed = 5f;
    public float fireAngleThreshold = 5f;

    [Header("발사 지점 설정")]
    public Transform firePoint;
    public bool useFallbackToSelf = true;

    [Header("파티클 설정")]
    public ParticleSystem particlePrefab;
    public float particleDuration = 2f;
    public float fireCooldown = 5f;

    [Header("데미지 설정")]
    public int damageAmount = 10;
    public float damageInterval = 1f;

    private Transform currentTarget;
    private VRPlayerController playerController;
    private bool isCoolingDown = false;

    public bool IsOnCooldown => isCoolingDown;

    void Start()
    {
        if (trackingTarget != null)
        {
            currentTarget = trackingTarget;
        }
        else
        {
            var headObj = GameObject.FindWithTag("MainCamera");
            if (headObj != null)
                currentTarget = headObj.transform;
            else
                Debug.LogWarning("추적 대상이 설정되지 않았고, MainCamera도 찾을 수 없습니다.");
        }

        if (currentTarget != null)
        {
            playerController = currentTarget.GetComponentInParent<VRPlayerController>();
            if (playerController == null)
                Debug.LogWarning("VRPlayerController를 찾을 수 없습니다.");
        }

        if (firePoint == null && useFallbackToSelf)
            firePoint = this.transform;
    }

    void Update()
    {
        if (currentTarget == null || isCoolingDown)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > fireRange)
            return;

        Vector3 dir = (currentTarget.position - transform.position).normalized;
        if (dir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        if (Quaternion.Angle(transform.rotation, targetRot) <= fireAngleThreshold)
        {
            TriggerFire();
        }
    }

    internal void TriggerFire()
    {
        if (isCoolingDown)
            return;

        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        isCoolingDown = true;

        Vector3 spawnPos = firePoint.position;
        Quaternion spawnRot = firePoint.rotation;

        // 파티클 생성
        var psInstance = Instantiate(particlePrefab, spawnPos, spawnRot);

        var col = psInstance.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.sendCollisionMessages = true;
        col.collidesWith = LayerMask.GetMask("Player");

        var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
        dmgHandler.damageAmount = damageAmount;
        dmgHandler.damageInterval = damageInterval;
        dmgHandler.playerController = playerController;

        psInstance.Play();

        yield return new WaitForSeconds(particleDuration);
        psInstance.Stop();
        Destroy(psInstance.gameObject, psInstance.main.startLifetime.constantMax);

        yield return new WaitForSeconds(fireCooldown);
        isCoolingDown = false;
    }
}
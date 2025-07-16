using UnityEngine;
using System.Collections;

public class RangeAttack : MonoBehaviour
{
    public enum FireMode
    {
        Single,  // 한 지점에서 발사 (레이저)
        Multi    // 여러 지점에서 동시에 발사 (머신건)
    }

    [Header("추적 대상 설정")]
    public Transform trackingTarget;

    [Header("발사 타입 설정")]
    public FireMode fireMode = FireMode.Single;

    [Header("발사 지점 설정")]
    public Transform[] firePoints;
    public bool useFallbackToSelf = true;

    [Header("조건 설정")]
    public float fireRange = 10f;
    public float rotationSpeed = 5f;
    public float fireAngleThreshold = 5f;

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
            currentTarget = trackingTarget;
        else
        {
            var headObj = GameObject.FindWithTag("MainCamera");
            if (headObj != null)
                currentTarget = headObj.transform;
            else
                Debug.LogWarning("MainCamera를 찾을 수 없습니다.");
        }

        if (currentTarget != null)
        {
            playerController = currentTarget.GetComponentInParent<VRPlayerController>();
            if (playerController == null)
                Debug.LogWarning("VRPlayerController를 찾을 수 없습니다.");
        }

        // fallback
        if ((firePoints == null || firePoints.Length == 0) && useFallbackToSelf)
            firePoints = new Transform[] { this.transform };
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
            TriggerFire();
    }

    internal void TriggerFire()
    {
        if (!isCoolingDown)
            StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        isCoolingDown = true;

        if (fireMode == FireMode.Single)
        {
            Transform firePoint = firePoints[0];
            FireParticle(firePoint);
        }
        else if (fireMode == FireMode.Multi)
        {
            foreach (Transform fp in firePoints)
                FireParticle(fp);
        }

        yield return new WaitForSeconds(particleDuration + fireCooldown);
        isCoolingDown = false;
    }

    private void FireParticle(Transform firePoint)
    {
        var psInstance = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);

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
        Destroy(psInstance.gameObject, particleDuration + psInstance.main.startLifetime.constantMax);
    }
}
using UnityEngine;
using System.Collections;

/// <summary>
/// VR ȯ�濡�� ������ Ÿ�� ��ġ�� ������ ��,
/// ���� ����(�Ÿ�) ���� ������:
///   1) ��ǥ ���� �̳��� �ٶ󺸸� ��ƼŬ ����
///   2) ��ƼŬ �浹 �ÿ��� �÷��̾�� �������� �ο�
///   3) ������ �ð� �� ��ƼŬ �ڵ� ���� �� ��Ÿ�� �� ��߻�
/// </summary>
public class RangeAttack : MonoBehaviour
{
    [Header("���� ��� ����")]
    [Tooltip("NPC�� �ٶ� ��� Transform (������ MainCamera�� ���)")]
    public Transform trackingTarget;

    [Header("�߻� ���� ����")]
    [Tooltip("��ƼŬ �߻� ���� �ִ� �Ÿ� (���� ����)")]
    public float fireRange = 10f;

    [Header("���� ����")]
    [Tooltip("NPC�� ��� �������� ȸ���ϴ� �ӵ� (���� Ŭ���� ����)")]
    public float rotationSpeed = 5f;
    [Tooltip("��ǥ ���� ���� ���� (��)")]
    public float fireAngleThreshold = 5f;

    [Header("�߻� ���� ����")]
    [Tooltip("��ƼŬ�� ������ ��ġ Transform")]
    public Transform firePoint;
    [Tooltip("firePoint �̼��� �� NPC �ڽ��� ��ġ ���")] public bool useFallbackToSelf = true;

    [Header("��ƼŬ ����")]
    [Tooltip("�߻��� ��ƼŬ �ý��� ������")]
    public ParticleSystem particlePrefab;
    [Tooltip("��ƼŬ�� ������ �ð� (��)")]
    public float particleDuration = 2f;
    [Tooltip("��ƼŬ ��߻� �� ��� �ð� (��)")]
    public float fireCooldown = 5f;

    [Header("������ ����")]
    [Tooltip("�÷��̾�� �� ��������")]
    public int damageAmount = 10;
    [Tooltip("������ ������ �ּ� ���� (��)")]
    public float damageInterval = 1f;

    private Transform currentTarget;
    private VRPlayerController playerController;
    private bool isCoolingDown = false;

    void Start()
    {
        // ���� ��� ����
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
                Debug.LogWarning("���� ����� �������� �ʾҰ�, MainCamera�� ã�� �� �����ϴ�.");
        }

        // ������ ó�� ������Ʈ ��ȸ
        if (currentTarget != null)
        {
            playerController = currentTarget.GetComponentInParent<VRPlayerController>();
            if (playerController == null)
                Debug.LogWarning("VRPlayerController�� ã�� �� �����ϴ�. (Parent ��������)");
        }

        // firePoint ����
        if (firePoint == null && useFallbackToSelf)
            firePoint = this.transform;
    }

    void Update()
    {
        if (currentTarget == null || isCoolingDown)
            return;

        // ���� �˻�
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > fireRange)
            return;

        // ���� �� ȸ��
        Vector3 dir = (currentTarget.position - transform.position).normalized;
        if (dir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        // ���� �� ���� ���� �� ��ƼŬ �߻�
        if (Quaternion.Angle(transform.rotation, targetRot) <= fireAngleThreshold)
            StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        isCoolingDown = true;

        // �߻� ��ġ �� ȸ��
        Vector3 spawnPos = firePoint.position;
        Quaternion spawnRot = firePoint.rotation;

        // ��ƼŬ �ν��Ͻ� ����
        var psInstance = Instantiate(particlePrefab, spawnPos, spawnRot);
        var col = psInstance.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.sendCollisionMessages = true;

        // ������ �ڵ鷯 �߰�
        var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
        dmgHandler.damageAmount = damageAmount;
        dmgHandler.damageInterval = damageInterval;
        dmgHandler.playerController = playerController;

        psInstance.Play();

        // ��ƼŬ ���� �� �������ı�
        yield return new WaitForSeconds(particleDuration);
        psInstance.Stop();
        Destroy(psInstance.gameObject, psInstance.main.startLifetime.constantMax);

        // ��߻� ��Ÿ��
        yield return new WaitForSeconds(fireCooldown);
        isCoolingDown = false;
    }
}

/// <summary>
/// ��ƼŬ �浹 �� �� ���� ������ ���� ������Ʈ
/// </summary>
public class ParticleDamageOnCollision : MonoBehaviour
{
    [HideInInspector] public int damageAmount;
    [HideInInspector] public float damageInterval;
    [HideInInspector] public VRPlayerController playerController;
    private bool canDamage = true;

    void OnParticleCollision(GameObject other)
    {
        if (!canDamage || !other.CompareTag("Player")) return;
        playerController?.CalculateHP(-damageAmount, damageInterval);
        StartCoroutine(DamageCooldown());
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageInterval);
        canDamage = true;
    }
}

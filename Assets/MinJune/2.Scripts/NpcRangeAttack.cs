using UnityEngine;
using System.Collections;

public class NpcRangeAttack : MonoBehaviour
{
    public enum FireMode
    {
        Single,  // 한 지점에서 발사 (레이저)
        Multi    // 여러 지점에서 동시에 발사 (머신건)
    }

    [Header("발사 타입 설정")]
    public FireMode fireMode = FireMode.Single;

    [Header("발사 지점 설정")]
    public Transform[] firePoints;
    public bool useFallbackToSelf = true;

    [Header("파티클 설정")]
    public ParticleSystem particlePrefab;
    public float particleDuration = 2f;
    public float fireCooldown = 5f;

    [Header("발사 범위 설정")]   // <-- [추가] 헤더
    public float fireRange = 10f; // <-- [추가] 인스펙터에서 조정 가능

    [Header("데미지 설정")]
    public int damageAmount = 10;
    public float damageInterval = 1f;

    private VRPlayerController playerController;
    private bool isCoolingDown = false;

    public bool IsOnCooldown => isCoolingDown;

    void Awake()
    {
        var playerObj = GameObject.FindWithTag("MainCamera");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponentInParent<VRPlayerController>();
            if (playerController == null)
                Debug.LogWarning("VRPlayerController를 찾을 수 없습니다. 데미지 적용이 안될 수 있습니다.");
        }
        else
        {
            Debug.LogWarning("MainCamera 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }

        if ((firePoints == null || firePoints.Length == 0) && useFallbackToSelf)
            firePoints = new Transform[] { this.transform };

        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogError(gameObject.name + ": NpcRangeAttack에 firePoints가 설정되지 않았습니다. 발사를 수행할 수 없습니다.");
        }
    }

    // TriggerFire를 호출할 때 fireRange 체크를 외부에서 해주거나, 혹은 내부에서 fireRange 체크를 추가로 넣을 수 있음
    internal void TriggerFire()
    {
        if (!isCoolingDown)
        {
            StartCoroutine(FireRoutine());
        }
    }

    private IEnumerator FireRoutine()
    {
        isCoolingDown = true;

        if (fireMode == FireMode.Single)
        {
            if (firePoints != null && firePoints.Length > 0 && firePoints[0] != null)
                FireParticle(firePoints[0]);
            else
                Debug.LogWarning(gameObject.name + ": Single FireMode이나 발사 지점을 찾을 수 없습니다.");
        }
        else if (fireMode == FireMode.Multi)
        {
            if (firePoints != null)
            {
                foreach (Transform fp in firePoints)
                {
                    if (fp != null) FireParticle(fp);
                }
            }
            else
            {
                Debug.LogWarning(gameObject.name + ": Multi FireMode이나 발사 지점이 없습니다.");
            }
        }

        yield return new WaitForSeconds(fireCooldown);
        isCoolingDown = false;
    }

    private void FireParticle(Transform firePoint)
    {
        Debug.Log("🔥 파티클 발사 시도됨!");

        if (particlePrefab == null)
        {
            Debug.LogWarning("⚠️ particlePrefab이 null이라서 못 쏨!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("⚠️ firePoint가 null이야!");
            return;
        }

        var psInstance = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);
        Debug.Log("✅ 파티클 인스턴스 생성됨: " + psInstance.name);

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
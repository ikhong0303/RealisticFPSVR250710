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
    public float fireCooldown = 5f; // 이 쿨다운은 NpcRangedController에서 공격 주기를 제어할 때 사용됩니다.

    [Header("데미지 설정")]
    public int damageAmount = 10;
    public float damageInterval = 1f;

    private VRPlayerController playerController;
    private bool isCoolingDown = false;

    // 외부에서 쿨다운 상태를 확인할 수 있도록 public 속성으로 노출
    public bool IsOnCooldown => isCoolingDown;

    void Awake()
    {
        // VR 플레이어 컨트롤러를 찾아서 참조합니다.
        // 현재 게임에 VRPlayerController가 어떤 방식으로 존재하는지에 따라 이 로직은 변경될 수 있습니다.
        // 예를 들어, GameManager에 플레이어 참조가 있다면 그곳에서 가져올 수도 있습니다.
        var playerObj = GameObject.FindWithTag("MainCamera"); // 플레이어 카메라(HMD) 기준으로 찾음
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

        // 발사 지점이 설정되지 않았을 경우, 스크립트가 붙은 오브젝트 자체를 발사 지점으로 사용
        if ((firePoints == null || firePoints.Length == 0) && useFallbackToSelf)
            firePoints = new Transform[] { this.transform };

        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogError(gameObject.name + ": NpcRangeAttack에 firePoints가 설정되지 않았습니다. 발사를 수행할 수 없습니다.");
        }
    }

    // 이 스크립트는 스스로 공격 조건을 판단하여 발사하지 않습니다.
    // 오직 NpcRangedController.cs 에서 TriggerFire()를 호출할 때만 발사됩니다.
    // void Update() { /* 이 부분은 의도적으로 비워둡니다. */ }

    // NpcRangedController에서 호출하여 실제 공격을 트리거하는 함수
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

        // 파티클 지속 시간 + 쿨다운 시간만큼 기다림
        // NpcRangedController의 animationEventToParticleDelay와는 별개의 RangeAttack 자체의 쿨다운
        yield return new WaitForSeconds(fireCooldown);
        // 참고: particleDuration은 ParticleSystem 자체의 지속 시간이며,
        // 파티클 오브젝트 파괴는 FireParticle 내부에서 이루어지므로,
        // 여기서는 오직 fireCooldown만 기다리면 됩니다.

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

        // 파티클 시스템 충돌 설정
        var col = psInstance.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.sendCollisionMessages = true;
        col.collidesWith = LayerMask.GetMask("Player"); // "Player" 레이어와만 충돌하도록 설정

        // 데미지 처리 컴포넌트 추가
        var dmgHandler = psInstance.gameObject.AddComponent<ParticleDamageOnCollision>();
        dmgHandler.damageAmount = damageAmount;
        dmgHandler.damageInterval = damageInterval;
        dmgHandler.playerController = playerController; // 이전에 찾은 VRPlayerController 참조 전달

        psInstance.Play();

        // 파티클 시스템 재생이 끝나면 오브젝트를 파괴 (파티클의 startLifetime을 고려)
        Destroy(psInstance.gameObject, particleDuration + psInstance.main.startLifetime.constantMax);
    }
}
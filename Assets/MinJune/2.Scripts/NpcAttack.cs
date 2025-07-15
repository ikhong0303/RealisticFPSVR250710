using UnityEngine;

public class NpcAttack : MonoBehaviour
{
    public GameObject hitFxPrefab; // 피격 이펙트 프리팹
    public int power = 1;           // 공격력
    public float attackCooldown = 1f; // 공격 쿨타임 (초)

    private bool isOnCooldown = false; // 쿨타임 상태 여부

    private void OnEnable()
    {
        isOnCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 쿨타임 중이면 무시
        if (isOnCooldown) return;

        // VR 플레이어에게 충돌했을 때만 처리
        if (other.TryGetComponent<VRPlayerController>(out VRPlayerController player))
        {
            // 피격 이펙트 생성 (가장 가까운 지점)
            if (hitFxPrefab != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Instantiate(hitFxPrefab, hitPoint, Quaternion.identity);
            }

            // 플레이어에게 데미지 전달
            player.CalculateHP(-power);

            // 쿨타임 시작
            isOnCooldown = true;
            Invoke(nameof(ResetCooldown), attackCooldown);
        }
    }

    // 쿨타임 리셋 함수
    private void ResetCooldown()
    {
        isOnCooldown = false;
    }

    // 애니메이션 이벤트용 함수 (애니메이션에서 호출 시 이 공격기 활성화)
    public void EnableAttackHitbox()
    {
        gameObject.SetActive(true);
    }

    // 애니메이션 이벤트용 함수 (공격 종료 시 호출해서 비활성화)
    public void DisableAttackHitbox()
    {
        gameObject.SetActive(false);
    }
}

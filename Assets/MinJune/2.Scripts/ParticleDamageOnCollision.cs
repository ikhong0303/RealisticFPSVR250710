using UnityEngine;

public class ParticleDamageOnCollision : MonoBehaviour
{
    public int damageAmount = 10; // 파티클 데미지 양 (양수 값으로 입력)
    public float damageInterval = 1f; // 데미지 중복 방지를 위한 간격
    public VRPlayerController playerController; // 플레이어의 VRPlayerController 참조

    public int count;

    private float lastDamageTime; // 마지막으로 데미지를 준 시간

    void OnParticleCollision(GameObject other)
    {
        Debug.Log("충돌");
        // 충돌한 오브젝트가 "Player" 태그를 가지고 있는지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log("데미지");
            count++;
            if (count == 10)
            {
                // VRPlayerController의 CalculateHP 함수를 호출하고, 데미지 양을 음수로 전달
                playerController = other.GetComponent<VRPlayerController>();
                playerController.CalculateHP(1);
                count = 0;
            }
        }


    }
}

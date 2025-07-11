using UnityEngine;

public class NPCParticleShooter : MonoBehaviour
{
    public float rotationSpeed = 5f;

    private Transform playerHead;

    void Start()
    {
        // VR 플레이어의 HMD(머리)를 추적 대상으로 설정 (MainCamera 태그 사용)
        GameObject head = GameObject.FindWithTag("MainCamera");
        if (head != null)
        {
            playerHead = head.transform;
        }
        else
        {
            Debug.LogWarning("MainCamera (VR Head) not found. NPC cannot track player.");
        }
    }

    void Update()
    {
        if (playerHead == null) return;

        // NPC가 플레이어의 머리 방향을 향해 부드럽게 회전
        Vector3 direction = (playerHead.position - transform.position).normalized;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
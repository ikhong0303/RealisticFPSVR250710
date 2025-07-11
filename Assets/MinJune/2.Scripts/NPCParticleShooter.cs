using UnityEngine;

public class NPCParticleShooter : MonoBehaviour
{
    public float rotationSpeed = 5f;

    private Transform playerHead;

    void Start()
    {
        // VR �÷��̾��� HMD(�Ӹ�)�� ���� ������� ���� (MainCamera �±� ���)
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

        // NPC�� �÷��̾��� �Ӹ� ������ ���� �ε巴�� ȸ��
        Vector3 direction = (playerHead.position - transform.position).normalized;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
using UnityEngine;

public class NpcAttack : MonoBehaviour
{
    public GameObject hitFxPrefab; // �ǰ� ����Ʈ ������
    public int power = 1;           // ���ݷ�
    public float attackCooldown = 1f; // ���� ��Ÿ�� (��)

    private bool isOnCooldown = false; // ��Ÿ�� ���� ����

    private void OnEnable()
    {
        isOnCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ��Ÿ�� ���̸� ����
        if (isOnCooldown) return;

        // VR �÷��̾�� �浹���� ���� ó��
        if (other.TryGetComponent<VRPlayerController>(out VRPlayerController player))
        {
            // �ǰ� ����Ʈ ���� (���� ����� ����)
            if (hitFxPrefab != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Instantiate(hitFxPrefab, hitPoint, Quaternion.identity);
            }

            // �÷��̾�� ������ ����
            player.CalculateHP(-power);

            // ��Ÿ�� ����
            isOnCooldown = true;
            Invoke(nameof(ResetCooldown), attackCooldown);
        }
    }

    // ��Ÿ�� ���� �Լ�
    private void ResetCooldown()
    {
        isOnCooldown = false;
    }

    // �ִϸ��̼� �̺�Ʈ�� �Լ� (�ִϸ��̼ǿ��� ȣ�� �� �� ���ݱ� Ȱ��ȭ)
    public void EnableAttackHitbox()
    {
        gameObject.SetActive(true);
    }

    // �ִϸ��̼� �̺�Ʈ�� �Լ� (���� ���� �� ȣ���ؼ� ��Ȱ��ȭ)
    public void DisableAttackHitbox()
    {
        gameObject.SetActive(false);
    }
}

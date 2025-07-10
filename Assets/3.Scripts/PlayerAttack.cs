using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float bulletSpeed = 20f;
    public int damage = 10;
    public float lifeTime = 2f;
    public GameObject hitEffectPrefab;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * bulletSpeed; // ⚠️ linearVelocity → velocity
        }
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 태그를 지정해서 데미지를 줄 대상만 처리
        if (other.CompareTag("NPC") || other.CompareTag("Enemy"))
        {
            // 충돌한 오브젝트에서 데미지를 받는 스크립트를 찾음
            var damageTarget = other.GetComponent<MonoBehaviour>();
            if (damageTarget != null)
            {
                // 리플렉션 없이 간단히 메서드 호출을 시도
                var method = damageTarget.GetType().GetMethod("TakeDamage");
                if (method != null)
                {
                    method.Invoke(damageTarget, new object[] { damage });
                }
            }
        }

        if (hitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
            Destroy(hitEffect, 1f);
        }

        Destroy(gameObject);
    }
}
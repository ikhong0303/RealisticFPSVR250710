// File: Scripts/BossAI.cs
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Player Target")]
    public Transform player;

    [Header("Attack Settings")]
    public float attackInterval = 3f;
    public Transform firePoint;
    private float attackTimer;

    public GameObject[] attackPrefabs;

    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Detection")]
    public float detectRange = 30f;

    void Start()
    {
        currentHealth = maxHealth;
        if (player == null && Camera.main != null)
            player = Camera.main.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectRange) return;

        // 바라보기 (Y축만)
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 2f);
        }

        // 공격 타이머
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            //PerformRandomAttack();
        }
    }

    void PerformRandomAttack()
    {
        if (attackPrefabs.Length == 0 || firePoint == null) return;

        int i = Random.Range(0, attackPrefabs.Length);
        GameObject proj = Instantiate(attackPrefabs[i], firePoint.position, firePoint.rotation);

        // 방향 추적형은 내부에서 동작
        //HomingProjectile homing = proj.GetComponent<HomingProjectile>();
        //if (homing) homing.SetTarget(player);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // TODO: 사망 이펙트, 보상 처리 등
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            //WeaponDamage weapon = other.GetComponent<WeaponDamage>();
            //if (weapon != null)
            //{
            //    TakeDamage(weapon.damage);
            //}
        }
    }
}

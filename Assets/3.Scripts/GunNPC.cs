using UnityEngine;

public class GunNPC : MonoBehaviour
{


    public GameObject bulletPrefab;        // 총알 프리팹
    public Transform firePoint;            // 총알 발사 위치
    public float attackRange = 15f;        // 사정거리
    public float fireRate = 1f;            // 발사 간격(초)
    public float bulletForce = 20f;

    private float fireCooldown = 0f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            transform.LookAt(player.position);

            if (fireCooldown <= 0f)
            {
                Shoot();
                fireCooldown = fireRate;
            }
        }

        fireCooldown -= Time.deltaTime;
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(firePoint.forward * bulletForce, ForceMode.Impulse);
    }
}


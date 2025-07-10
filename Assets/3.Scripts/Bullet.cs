using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            VRPlayerController hp = collision.gameObject.GetComponent<VRPlayerController>();
            if (hp != null)
                 hp.CalculateHP(-damage);
               
        }

        Destroy(gameObject); // ¹«Á¶°Ç ÆÄ±«
    }
}

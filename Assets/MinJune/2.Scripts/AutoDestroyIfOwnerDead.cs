using UnityEngine;

public class AutoDestroyIfOwnerDead : MonoBehaviour
{
    public GameObject owner;
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (owner == null)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(gameObject);
        }
    }
}

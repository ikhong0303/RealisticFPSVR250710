using UnityEngine;
using System.Collections;

public class ParticleDamageOnCollision : MonoBehaviour
{
    [HideInInspector] public int damageAmount;
    [HideInInspector] public float damageInterval;
    [HideInInspector] public VRPlayerController playerController;

    private bool canDamage = true;

    void OnParticleCollision(GameObject other)
    {
        if (!canDamage || !other.CompareTag("Player")) return;

        playerController?.CalculateHP(-damageAmount, damageInterval);
        StartCoroutine(DamageCooldown());
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageInterval);
        canDamage = true;
    }
}

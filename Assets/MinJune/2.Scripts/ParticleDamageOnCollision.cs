using System.Collections;
using UnityEngine;

public class ParticleDamageOnCollision : MonoBehaviour
{
    [HideInInspector] public int damageAmount;
    [HideInInspector] public float damageInterval;
    [HideInInspector] public VRPlayerController playerController;

    private bool canDamage = true;

    void OnParticleCollision(GameObject other)
    {
        if (!canDamage || !other.CompareTag("Player")) return;
        if (playerController == null) return;

        playerController.CalculateHP(-damageAmount);
        StartCoroutine(DamageCooldown());
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageInterval);
        canDamage = true;
    }
}

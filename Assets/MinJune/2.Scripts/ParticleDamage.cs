using UnityEngine;
using System.Collections;

public class ParticleDamage : MonoBehaviour
{
    public float delayBeforeNextFire = 2f;
    public int damage = 1;

    private bool hasHit = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<VRPlayerController>();
            if (player != null)
            {
                player.CalculateHP(-damage);
                hasHit = true;
                StartCoroutine(DisableTemporarily());
            }
        }
    }

    IEnumerator DisableTemporarily()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(delayBeforeNextFire);
        hasHit = false;
        gameObject.SetActive(true);
    }
}
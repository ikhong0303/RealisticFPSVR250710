using UnityEngine;

public class NauseaTrigger_CC : MonoBehaviour
{
    public float nauseaDuration = 3f;
    public float launchVelocity = 15f;
    public float cameraShakeAmount = 2f;
    public float spinSpeed = 360f;
    public string playerTag = "Player";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            var cc = other.GetComponent<CharacterController>();
            var nausea = other.GetComponent<VRNauseaEffect_CC>();
            if (cc != null)
            {
                triggered = true;

                if (nausea == null)
                    nausea = other.gameObject.AddComponent<VRNauseaEffect_CC>();

                nausea.StartNausea(nauseaDuration, launchVelocity, cameraShakeAmount, spinSpeed);
            }
        }
    }
}
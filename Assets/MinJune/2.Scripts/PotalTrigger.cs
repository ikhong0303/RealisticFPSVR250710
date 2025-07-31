using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    public string targetSceneName;
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.LoadSceneByName(targetSceneName);
            }
        }
    }
}
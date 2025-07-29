using UnityEngine;

public class EnemyDestory : MonoBehaviour
{
    private void OnDestroy()
    {
        if (GameManager.instance != null)
            GameManager.instance.EnemyDefeated(this.gameObject);
    }
}

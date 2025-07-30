using UnityEngine;
using UnityEngine.SceneManagement;

public class CubeSocketTrigger : MonoBehaviour
{
    [Header("씬 전환 이름")]
    public string nextSceneName = "EndingScene";

    [Header("파티클 프리팹")]
    public ParticleSystem particlePrefab;

    [Header("파티클이 재생될 위치")]
    public Transform particleSpawnPoint;

    [Header("큐브 태그 (ex: EndingCube)")]
    public string cubeTag = "EndingCube";

    [Header("파티클 재생 후 씬 전환 지연 시간")]
    public float delayBeforeSceneLoad = 3f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag(cubeTag))
        {
            triggered = true;

            // 파티클 재생
            if (particlePrefab != null && particleSpawnPoint != null)
            {
                Instantiate(particlePrefab, particleSpawnPoint.position, Quaternion.identity);
            }

            // 씬 전환
            Invoke(nameof(LoadNextScene), delayBeforeSceneLoad);
        }
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
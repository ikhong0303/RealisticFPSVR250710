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

    [Header("소켓 효과음 이름 (AudioManager에서 관리)")]
    public string socketSFXName = "SocketTrigger";  // 인스펙터에서 원하는 효과음 이름 등록

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

            // 효과음 재생 (3D 위치)
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(socketSFXName))
            {
                AudioManager.Instance.PlaySFX(socketSFXName, particleSpawnPoint != null ? particleSpawnPoint.position : transform.position);
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
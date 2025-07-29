using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject player;

    [Header("포탈 프리팹과 생성 위치")]
    public GameObject portalPrefab;          // 포탈 프리팹
    public Transform portalSpawnPoint;       // 포탈 위치(빈 오브젝트)

    [Header("적 관리")]
    public int enemyCount = 0;               // 현재 남은 적 수

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (player == null)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                player = origin.gameObject;
        }

        if (player?.GetComponent<XROrigin>() == null)
            Debug.LogWarning("GameManager: player에 XROrigin이 없습니다.");

        // (주의) NpcSpawner에서 enemyCount를 증가시킴. 여기서 자동 카운트 X.
    }

    // 적 전멸시 포탈 생성 호출
    public void SpawnPortal()
    {
        if (portalPrefab != null && portalSpawnPoint != null)
        {
            Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
            Debug.Log("포탈 생성됨!");
        }
        else
        {
            Debug.LogWarning("portalPrefab 또는 portalSpawnPoint가 연결되지 않았습니다.");
        }
    }

    // 씬 이름으로 이동
    public void LoadSceneByName(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.Log("씬 이동: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"씬 이름이 잘못되었거나 빌드 세팅에 없음: {sceneName}");
        }
    }

    public void EnemyDefeated(GameObject enemy)
    {
        Debug.Log("적이 처치됨: " + enemy.name);

        enemyCount--;

        Debug.Log("남은 적 수: " + enemyCount);

        if (enemyCount <= 0)
        {
            SpawnPortal();
        }
    }
}

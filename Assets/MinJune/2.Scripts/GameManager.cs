using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject player;

    [Header("포탈 프리팹")]
    public GameObject portalPrefab;
    [HideInInspector]
    public Transform portalSpawnPoint;

    [Header("적 관리")]
    public int enemyCount = 0; // NpcSpawner에서만 관리

    private string nextSceneName; // 현재 스테이지의 "다음 씬" 이름

    [Header("포탈 사운드 이름 (AudioManager에서 관리)")]
    public string portalSpawnSFXName = "PortalSpawn";  // 인스펙터에서 원하는 효과음 이름 지정

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AssignSceneReferences();
    }

    private void AssignSceneReferences()
    {
        // 플레이어 할당
        var origin = FindFirstObjectByType<XROrigin>();
        if (origin != null)
            player = origin.gameObject;
        else
            player = null;

        // 포탈 스폰 포인트 자동 할당
        var psp = GameObject.Find("PortalSpawnPoint");
        if (psp != null)
            portalSpawnPoint = psp.transform;
        else
            portalSpawnPoint = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignSceneReferences();
    }

    // **포탈 생성 시 이동할 씬 이름을 꼭 넘겨준다!**
    public void SpawnPortal(string targetSceneName)
    {
        if (portalPrefab != null && portalSpawnPoint != null)
        {
            GameObject portal = Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);

            // 포탈에 이동할 씬 이름 동적으로 세팅!
            PortalTrigger trigger = portal.GetComponent<PortalTrigger>();
            if (trigger != null)
            {
                trigger.targetSceneName = targetSceneName;
            }

            // === 포탈 소환 사운드 재생 ===
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(portalSpawnSFXName))
            {
                AudioManager.Instance.PlaySFX(portalSpawnSFXName, portalSpawnPoint.position);
            }

            Debug.Log("포탈 생성됨! 이동 씬: " + targetSceneName);
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

    // 적 처치 및 포탈 소환 트리거 (적 다 죽으면 호출)
    // 반드시 NpcSpawner에서 enemyCount를 리셋하고,
    // 적 소환 때마다 enemyCount++,
    // EnemyDestory에서 이 함수 호출
    public void EnemyDefeated(GameObject enemy)
    {
        Debug.Log("적이 처치됨: " + enemy.name);

        enemyCount--;

        Debug.Log("남은 적 수: " + enemyCount);

        // ★ nextSceneName을 반드시 이 시점에 결정해서 넘겨야 함!
        if (enemyCount <= 0)
        {
            // 예시: 각 스테이지마다 nextSceneName 다르게 설정 가능
            SpawnPortal(nextSceneName);
        }
    }

    // 각 스테이지에서 호출해서 "이 포탈이 이동할 다음 씬"을 지정
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
        Debug.Log("이 스테이지의 다음 씬: " + sceneName);
    }
}
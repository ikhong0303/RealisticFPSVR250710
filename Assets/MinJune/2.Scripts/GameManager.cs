using UnityEngine;
using Unity.XR.CoreUtils;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject player; // VR 플레이어 오브젝트 (XROrigin)

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
        // 자동으로 플레이어 찾기
        if (player == null)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                player = origin.gameObject;
        }

        // 경고 메시지
        if (player?.GetComponent<XROrigin>() == null)
            Debug.LogWarning("GameManager: player에 XROrigin이 없습니다.");
    }
}

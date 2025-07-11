using UnityEngine;
using Unity.XR.CoreUtils;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject player; // VR �÷��̾� ������Ʈ (XROrigin)

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
        // �ڵ����� �÷��̾� ã��
        if (player == null)
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                player = origin.gameObject;
        }

        // ��� �޽���
        if (player?.GetComponent<XROrigin>() == null)
            Debug.LogWarning("GameManager: player�� XROrigin�� �����ϴ�.");
    }
}

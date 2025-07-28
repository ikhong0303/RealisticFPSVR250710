using UnityEngine;

public class VRPlayerController : MonoBehaviour
{
    public int hp = 100;
    public int maxHp = 100;

    // 👇 이동 감지용 필드 추가
    private Vector3 _lastPosition;

    void Start()
    {
        _lastPosition = transform.position;
    }

    void Update()
    {
        // 👇 위치가 바뀌면
        if (_lastPosition != transform.position)
        {
            DestroyAllMuzzleFx();      // 머즐플래시 파티클 싹 삭제
            _lastPosition = transform.position;
        }
    }

    // 👇 머즐플래시(파티클) 자동 삭제 함수
    void DestroyAllMuzzleFx()
    {
        var fxList = GameObject.FindGameObjectsWithTag("MuzzleFx");
        foreach (var fx in fxList)
        {
            Destroy(fx);
        }
    }

    /// <summary>
    /// damage 값을 hp에 더하고 0~maxHp로 클램프
    /// </summary>
    public void CalculateHP(int damage)
    {
        hp += damage;
        hp = Mathf.Clamp(hp, 0, maxHp);
    }
}

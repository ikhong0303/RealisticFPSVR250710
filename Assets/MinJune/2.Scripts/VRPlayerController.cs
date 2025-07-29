using UnityEngine;

public class VRPlayerController : MonoBehaviour
{
    public int hp = 100;
    public int maxHp = 100;
    RedOutFx redOutFx;

    [Header("자동 체력 회복 설정")]
    public float healInterval = 1.0f; // 몇 초마다 회복할지
    public int healAmount = 5;        // 한 번에 회복하는 양

    private float healTimer = 0f;

    // 👇 이동 감지용 필드 추가
    private Vector3 _lastPosition;

    private void Awake()
    {
        redOutFx = GetComponent<RedOutFx>();
    }

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

        // 🟢 자동 체력 회복
        if (hp < maxHp)
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healInterval)
            {
                CalculateHP(healAmount); // 체력 회복
                healTimer = 0f;
            }
        }
        else
        {
            healTimer = 0f; // 만피일 때는 타이머 초기화(비권장, 하지만 불필요한 회복 방지)
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
    public void CalculateHP(int damage = -10)
    {
        hp += damage;
        hp = Mathf.Clamp(hp, 0, maxHp);
        redOutFx.RedOut((float)hp / maxHp);
    }
}
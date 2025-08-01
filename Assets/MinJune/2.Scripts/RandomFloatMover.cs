using UnityEngine;

public class RandomFloatMover : MonoBehaviour
{
    public float floatRadius = 2f; // 최대 이동 범위(좌우, 상하)
    public float floatSpeed = 1f;  // 이동 속도 (느리게~빠르게)
    private Vector3 origin;        // 원래 위치
    private Vector3 offset;

    void Start()
    {
        origin = transform.position;
        // 랜덤 offset 주면 각 오브젝트가 서로 다르게 움직임
        offset = new Vector3(Random.value * 10f, Random.value * 10f, Random.value * 10f);
    }

    void Update()
    {
        // X, Y, Z 각 방향으로 Sin, Cos 곱해 랜덤하게 떠다님
        float x = Mathf.Sin(Time.time * floatSpeed + offset.x) * floatRadius;
        float y = Mathf.Sin(Time.time * floatSpeed * 0.8f + offset.y) * floatRadius;
        float z = Mathf.Cos(Time.time * floatSpeed * 0.6f + offset.z) * floatRadius;

        // 부드럽게 원래 위치 + 부유값
        transform.position = origin + new Vector3(x, y, z);
    }
}
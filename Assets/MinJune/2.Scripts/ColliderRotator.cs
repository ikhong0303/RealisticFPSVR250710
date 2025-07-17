using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ColliderRotator : MonoBehaviour
{
    [Header("회전 설정")]
    public Vector3 rotationAxis = Vector3.up;   // 회전 축 (Inspector에서 변경 가능)
    public float rotationSpeed = 30f;           // 회전 속도 (degrees/sec)

    void Update()
    {
        // 매 프레임 지정한 축으로 회전
        transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.Self);
    }
}
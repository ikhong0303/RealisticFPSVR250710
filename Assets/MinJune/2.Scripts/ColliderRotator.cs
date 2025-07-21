using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ColliderRotator : MonoBehaviour
{
    [Header("È¸Àü")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 30f;

    void Update()
    {
        transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.Self);
    }
}

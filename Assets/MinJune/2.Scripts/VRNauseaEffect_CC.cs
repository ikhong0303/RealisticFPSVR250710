using UnityEngine;

public class VRNauseaEffect_CC : MonoBehaviour
{
    private float endTime;
    private float launchVel;
    private float shakeAmount;
    private float spinSpeed;

    private bool active = false;
    private CharacterController cc;
    private Transform cam;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private float verticalVelocity;

    public void StartNausea(float duration, float launch, float shake, float spin)
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (cam == null)
        {
            cam = Camera.main.transform; // XR 카메라 트랜스폼
            originalLocalPos = cam.localPosition;
            originalLocalRot = cam.localRotation;
        }

        launchVel = launch;
        shakeAmount = shake;
        spinSpeed = spin;
        endTime = Time.time + duration;
        verticalVelocity = launchVel;
        active = true;
    }

    private void Update()
    {
        if (!active) return;

        float t = endTime - Time.time;
        if (t <= 0f)
        {
            // 원상복구
            cam.localPosition = originalLocalPos;
            cam.localRotation = originalLocalRot;
            active = false;
            Destroy(this);
            return;
        }

        // 하늘로 쏘기 (gravity 없이!)
        Vector3 move = Vector3.up * verticalVelocity * Time.deltaTime;
        cc.Move(move);

        // 점점 느려지게(중력 비슷한 효과)
        verticalVelocity -= 9.8f * Time.deltaTime * 1.5f;

        // 무작위 흔들림 + 빠른 y축 회전
        Vector3 shakeOffset = Random.insideUnitSphere * shakeAmount * (t / (endTime - (endTime - t)));
        cam.localPosition = originalLocalPos + shakeOffset;
        cam.localRotation = originalLocalRot * Quaternion.Euler(0, spinSpeed * Time.deltaTime * Random.Range(0.8f, 1.2f), 0);
    }
}
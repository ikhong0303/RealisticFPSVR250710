// ProjectileWeapon.cs (Audio null-check 적용된 소총용 스크립트)
// 작성자: MikeNspired
// 이 스크립트는 VR 환경에서 소총 발사, 무제한 탄약, 발사 딜레이, 반동 및 햅틱 기능을 구현합니다.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// VR용 소총 스크립트
    /// - 무제한 탄약 지원
    /// - 연속 발사 딜레이
    /// - 반동 및 햅틱 피드백
    /// </summary>
    public class ProjectileWeapon : MonoBehaviour
    {
        [Header("발사 설정")]
        [SerializeField] private Transform firePoint;              // 총알 발사 위치
        [SerializeField] private Rigidbody projectilePrefab;       // 발사체 프리팹
        [SerializeField] private AudioSource fireAudio;            // 발사 사운드 (Inspector에서 할당 필요)
        [SerializeField] private float bulletSpeed = 150f;         // 발사 속도
        [SerializeField] private float fireDelay = 0.2f;           // 연속 발사 간 딜레이 (초)

        [Header("반동 및 햅틱")]
        [SerializeField] public float recoilAmount = -0.03f;      // 밀림 거리
        [SerializeField] public float recoilRotation = 1f;        // 회전 반동 각도
        [SerializeField] private float recoilTime = 0.06f;         // 반동 지속 시간 (초)
        [SerializeField] private float hapticStrength = 0.5f;      // 햅틱 강도
        [SerializeField] private float hapticDuration = 0.1f;      // 햅틱 지속 시간 (초)

        // 내부 상태
        private XRGrabInteractable interactable;
        private XRBaseInteractor controller;
        private Transform recoilTracker;
        private float nextFireTime = 0f;
        private bool isRecoiling = false;
        private float recoilTimer = 0f;
        private Vector3 recoilStartPos, recoilEndPos;
        private Quaternion recoilStartRot, recoilEndRot;
        private Vector3 controllerOffset;

        // 총알 발사 이벤트
        public UnityEvent BulletFiredEvent;

        private void Awake()
        {
            interactable = GetComponent<XRGrabInteractable>();
            // Activate 이벤트에 발사 로직 연결
            interactable.activated.AddListener(_ => FireGun());
            // Grab/Release 시 반동 트래커 생성/파괴
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
        }

        /// <summary>
        /// FireGun: 발사 메서드
        /// - fireDelay로 연속 발사 제한
        /// - 총알 인스턴스 생성 → 힘 적용
        /// - 사운드 및 햅틱 → 이벤트 호출 → 반동 시작
        /// </summary>
        public void FireGun()
        {
            if (Time.time < nextFireTime)
                return; // 딜레이 중엔 발사 무시

            nextFireTime = Time.time + fireDelay;

            // 총알 생성 및 발사
            Rigidbody bullet = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            bullet.AddForce(firePoint.forward * bulletSpeed, ForceMode.VelocityChange);

            // 발사 사운드 (fireAudio가 할당되어 있고 clip이 존재할 때만 재생)
            if (fireAudio != null && fireAudio.clip != null)
                fireAudio.PlayOneShot(fireAudio.clip);

            // 햅틱 피드백
            if (controller)
            {
                var haptic = controller.GetComponentInParent<HapticImpulsePlayer>();
                haptic?.SendHapticImpulse(hapticStrength, hapticDuration);
            }

            // 외부 확장용 이벤트
            BulletFiredEvent?.Invoke();

            // 반동 시작
            StartRecoil();
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            controller = args.interactorObject as XRBaseInteractor;
            StartCoroutine(SetupRecoilTracker());
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            StopAllCoroutines();
            if (recoilTracker) Destroy(recoilTracker.gameObject);
            isRecoiling = false;
        }

        /// <summary>
        /// 반동 트래커 생성
        /// </summary>
        private IEnumerator SetupRecoilTracker()
        {
            if (controller == null) yield break;
            recoilTracker = new GameObject("RecoilTracker").transform;
            recoilTracker.parent = controller.attachTransform;
            yield return null;
        }

        /// <summary>
        /// StartRecoil: 반동 시작 시 위치/회전 목표 계산
        /// </summary>
        private void StartRecoil()
        {
            if (!recoilTracker)
                StartCoroutine(SetupRecoilTracker());

            recoilStartPos = recoilTracker.localPosition;
            recoilStartRot = recoilTracker.localRotation;

            recoilEndPos = recoilStartPos + recoilTracker.forward * recoilAmount;
            recoilEndRot = recoilStartRot * Quaternion.Euler(-recoilRotation, 0, 0);

            controllerOffset = transform.position - recoilTracker.position;
            recoilTimer = 0f;
            isRecoiling = true;
        }

        private void OnEnable() => Application.onBeforeRender += RecoilUpdate;
        private void OnDisable() => Application.onBeforeRender -= RecoilUpdate;

        /// <summary>
        /// RecoilUpdate: 렌더 직전 반동 보정
        /// </summary>
        [UnityEngine.BeforeRenderOrder(101)]
        private void RecoilUpdate()
        {
            if (!isRecoiling) return;

            float half = recoilTime / 2f;
            if (recoilTimer < half)
            {
                float t = recoilTimer / half;
                recoilTracker.localPosition = Vector3.Lerp(recoilStartPos, recoilEndPos, t);
                recoilTracker.localRotation = Quaternion.Slerp(recoilStartRot, recoilEndRot, t);
            }
            else
            {
                float t = (recoilTimer - half) / half;
                recoilTracker.localPosition = Vector3.Lerp(recoilEndPos, Vector3.zero, t);
                recoilTracker.localRotation = Quaternion.Slerp(recoilEndRot, Quaternion.identity, t);
                transform.position = recoilTracker.position + controllerOffset;
            }

            recoilTimer += Time.deltaTime;
            if (recoilTimer >= recoilTime)
                isRecoiling = false;
        }
    }
}
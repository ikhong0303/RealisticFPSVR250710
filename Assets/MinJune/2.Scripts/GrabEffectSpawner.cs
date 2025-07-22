using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class GrabEffectSpawner : MonoBehaviour
    {
        [Header("잡을 때 나올 파티클 프리팹")]
        public GameObject grabEffectPrefab;

        [Header("파티클 소환 위치(옵션)")]
        public Transform effectSpawnPoint;

        private XRGrabInteractable grabInteractable;

        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
                Debug.LogWarning("GrabEffectSpawner: XRGrabInteractable가 필요합니다.");
        }

        void OnEnable()
        {
            if (grabInteractable != null)
                grabInteractable.selectEntered.AddListener(OnGrabbed);
        }

        void OnDisable()
        {
            if (grabInteractable != null)
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
           
          

            if (grabEffectPrefab != null) 
            { 
                var particlel = Instantiate(grabEffectPrefab, transform.position, transform.rotation);
                particlel.transform.parent = transform;
            }
             
        }
    }
}
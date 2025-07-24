using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트리거에 플레이어가 들어오면 여러 대상 오브젝트의 메테리얼을 전부 지정한 것으로 변경,
/// 플레이어가 나가면 모두 원래 메테리얼로 복원.
/// </summary>
public class ChangeOtherMaterialOnTrigger : MonoBehaviour
{
    [System.Serializable]
    public class TargetMaterialPair
    {
        [Header("메테리얼 바꿀 대상")]
        public GameObject targetObject;

        [Header("새로 적용할 메테리얼")]
        public Material newMaterial;

        [HideInInspector]
        public Material originalMaterial;
        [HideInInspector]
        public Renderer renderer;
    }

    [Header("동시에 바꿀 오브젝트/메테리얼 목록")]
    public List<TargetMaterialPair> targets = new List<TargetMaterialPair>();

    void Start()
    {
        // 각 대상의 Renderer와 원본 Material 저장
        foreach (var t in targets)
        {
            if (t.targetObject)
            {
                t.renderer = t.targetObject.GetComponent<Renderer>();
                if (t.renderer)
                    t.originalMaterial = t.renderer.material;
                else
                    Debug.LogWarning($"{t.targetObject.name}에 Renderer가 없습니다.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"트리거 진입: {other.name}, 태그: {other.tag}");

        if (!other.CompareTag("Player")) return;

        foreach (var t in targets)
        {
            if (t.renderer == null)
                Debug.LogWarning($"[{t.targetObject?.name}]에 Renderer가 없습니다!");
            else if (t.newMaterial == null)
                Debug.LogWarning($"[{t.targetObject?.name}]에 적용할 New Material이 없습니다!");
            else
            {
                Debug.Log($"[{t.targetObject.name}] 메테리얼 교체!");
                t.renderer.material = t.newMaterial;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"{other.name} 트리거 이탈!");
        if (!other.CompareTag("Player")) return;
        foreach (var t in targets)
        {
            if (t.renderer && t.originalMaterial)
                t.renderer.material = t.originalMaterial;
        }
    }

}

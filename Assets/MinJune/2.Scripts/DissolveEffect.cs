using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveEffect : MonoBehaviour
{
    public Renderer[] renderers;
    List<Material> mats = new List<Material>();
    public float disolveTime = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       // mat = GetComponent<Renderer>().materials[0]; //Renderer 컴포넌트의 material을 가져옴
        foreach(Renderer ren in renderers)
        {
            mats.Add(ren.material);
        }
    }

    [ContextMenu("disolve")]
    public void Dissolve()
    {
        StartCoroutine(DissolveFx());
      
    }

    IEnumerator DissolveFx()
    {
        float t = 0f; // 0부터 시작해서 1까지 증가
        while (t < disolveTime)
        {
            t += Time.deltaTime;
            float amount = Mathf.Clamp01(t / disolveTime); // 0~1 사이 값 보장

            foreach (var mat in mats)
            {
                mat.SetFloat("_DissolveAmount", amount);
            }

            yield return null;
        }

        // 최종적으로 정확히 1로 설정 (오차 보정)
        foreach (var mat in mats)
        {
            mat.SetFloat("_DissolveAmount", 1f);
        }
    }
}

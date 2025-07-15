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
       // mat = GetComponent<Renderer>().materials[0]; //Renderer ÄÄÆ÷³ÍÆ®ÀÇ materialÀ» °¡Á®¿È
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
        float t = disolveTime;
        while (true)
        {
            t -= Time.deltaTime;
            foreach (var mat in mats)
            {
                mat.SetFloat("_dissolvePower", t / disolveTime);
                if (t <= 0)
                {
                    mat.SetFloat("_dissolvePower", 0);
                    break;
                }
            }
            yield return null;
        }
    }
}

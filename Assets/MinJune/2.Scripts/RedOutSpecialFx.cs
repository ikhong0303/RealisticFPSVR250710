using UnityEngine;
using System.Collections;

public class RedOutSpecialFx : MonoBehaviour
{
    [Header("적용할 렌더러")]
    public Renderer targetRenderer;

    [Header("이펙트 타이밍 (초 단위)")]
    public float fadeInDuration = 0.8f;
    public float holdDuration = 0.5f;
    public float fadeOutDuration = 1f;

    [Header("Aperture Size 값")]
    public float minApertureSize = 0f;
    public float maxApertureSize = 1f;

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(RedOutSequence());
    }

    private IEnumerator RedOutSequence()
    {
        Material mat = targetRenderer.materials[0];

        // 페이드 인
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            mat.SetFloat("_ApertureSize", Mathf.Lerp(minApertureSize, maxApertureSize, t));
            yield return null;
        }
        mat.SetFloat("_ApertureSize", maxApertureSize);

        // 유지
        yield return new WaitForSeconds(holdDuration);

        // 페이드 아웃
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutDuration;
            mat.SetFloat("_ApertureSize", Mathf.Lerp(maxApertureSize, minApertureSize, t));
            yield return null;
        }
        mat.SetFloat("_ApertureSize", minApertureSize);
    }
}
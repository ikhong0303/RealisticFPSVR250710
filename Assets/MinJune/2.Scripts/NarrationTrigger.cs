using UnityEngine;
using TMPro;
using System.Collections;

public class NarrationTrigger : MonoBehaviour
{
    public int narrationBGMIndex = -1; // (선택) 오디오 매니저에서 나레이션 BGM 재생시 사용
    public TextMeshProUGUI subtitleText; // 자막 UI (NarrationPanel 하위)
    public GameObject narrationCanvas;   // 🎯 NarrationCanvas 오브젝트를 Inspector에서 직접 연결!
    [TextArea(3, 5)] public string[] subtitles;
    public float subtitleDuration = 3f;
    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            hasPlayed = true;
            if (narrationBGMIndex >= 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayBGM(narrationBGMIndex);
            StartCoroutine(PlayNarrationWithAutoHide());
        }
    }

    IEnumerator PlayNarrationWithAutoHide()
    {
        // 자막 한 줄씩 표시
        for (int i = 0; i < subtitles.Length; i++)
        {
            subtitleText.text = subtitles[i];
            subtitleText.alpha = 1;
            yield return new WaitForSeconds(subtitleDuration);
        }

        // (옵션) BGM이 끝날 때까지 기다리기 (AudioManager 구조에 맞게 수정 가능)
        AudioSource narrSource = null;
        if (AudioManager.Instance != null)
            narrSource = AudioManager.Instance.bgmSource;
        if (narrSource != null)
        {
            while (narrSource.isPlaying)
                yield return null;
        }

        // 자막 페이드 아웃
        yield return StartCoroutine(FadeOutSubtitle());

        // 🎯 NarrationCanvas만 비활성화!
        if (narrationCanvas != null)
            narrationCanvas.SetActive(false);
    }

    IEnumerator FadeOutSubtitle()
    {
        for (float i = 1; i >= 0; i -= 0.05f)
        {
            subtitleText.alpha = i;
            yield return new WaitForSeconds(0.05f);
        }
        subtitleText.text = "";
    }
}
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM 클립들 (씬별로 등록)")]
    public List<AudioClip> bgmClips;

    [Header("효과음 클립들")]
    public List<AudioClip> sfxClips;

    [Header("효과음 이름")]
    public List<string> sfxNames;

    public AudioSource bgmSource;
    public AudioSource sfxSourcePrefab;

    [Range(0, 1)] public float bgmVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // ⭐️ BGM 재생 함수!
    public void PlayBGM(int index)
    {
        if (bgmClips == null || bgmClips.Count == 0) return;
        if (index < 0 || index >= bgmClips.Count) return;
        if (bgmSource.isPlaying) bgmSource.Stop();
        bgmSource.clip = bgmClips[index];
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    // 효과음 이름으로 3D 재생
    public void PlaySFX(string sfxName, Vector3 pos, float spatialBlend = 1f)
    {
        int idx = sfxNames.IndexOf(sfxName);
        if (idx >= 0 && idx < sfxClips.Count)
        {
            AudioSource src = Instantiate(sfxSourcePrefab, pos, Quaternion.identity);
            src.clip = sfxClips[idx];
            src.volume = sfxVolume;
            src.spatialBlend = spatialBlend;
            src.Play();
            Destroy(src.gameObject, src.clip.length);
        }
    }
}
using UnityEngine;

/// <summary>
/// 자기 위치에서 둥둥 떠다니고, 사라질 때(Die 또는 Destroy 전) 파티클 생성
/// </summary>
public class IdleFloating : MonoBehaviour
{
    [Header("위아래 진폭(최대 높이)")]
    public float floatAmplitude = 0.4f;

    [Header("위아래 파동 속도")]
    public float floatFrequency = 1.2f;

    [Header("좌우(x) 미세 흔들림 세기")]
    public float swayAmplitudeX = 0.2f;

    [Header("앞뒤(z) 미세 흔들림 세기")]
    public float swayAmplitudeZ = 0.2f;

    [Header("x/z 흔들림 파동 속도 (랜덤 추천)")]
    public float swayFrequencyX = 0.7f;
    public float swayFrequencyZ = 0.9f;

    [Header("죽을 때(파괴 직전) 생성할 파티클 프리팹")]
    public GameObject deathEffectPrefab;

    [Header("파티클 지속시간")]
    public float deathEffectDuration = 4f;

    private Vector3 initialPosition;
    private float phaseX, phaseZ;
    private bool isDead = false;

    void Start()
    {
        initialPosition = transform.position;
        // 파동 위상 랜덤화
        phaseX = Random.Range(0f, Mathf.PI * 2f);
        phaseZ = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = Time.time;

        float floatY = Mathf.Sin(t * floatFrequency) * floatAmplitude;
        float swayX = Mathf.Sin(t * swayFrequencyX + phaseX) * swayAmplitudeX;
        float swayZ = Mathf.Sin(t * swayFrequencyZ + phaseZ) * swayAmplitudeZ;

        transform.position = initialPosition + new Vector3(swayX, floatY, swayZ);
    }

    /// <summary>
    /// 외부에서 호출하거나, 직접 테스트 시 수동 호출: 사라지며 파티클 소환
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffectPrefab)
        {
            Vector3 effectPos = transform.position + Vector3.up * 0.5f; // 살짝 위에 생성
            GameObject effect = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

            var ps = effect.GetComponent<ParticleSystem>();
            if (ps) ps.Play();

            Destroy(effect, deathEffectDuration);
        }

        Destroy(gameObject); // 자기 자신 제거
    }

    // (옵션) 외부에서 Destroy(obj)로 파괴해도 자동 파티클 나오게 하고 싶다면
    void OnDestroy()
    {
        if (!isDead && deathEffectPrefab)
        {
            Vector3 effectPos = transform.position + Vector3.up * 0.5f;
            GameObject effect = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

            var ps = effect.GetComponent<ParticleSystem>();
            if (ps) ps.Play();

            Destroy(effect, deathEffectDuration);
        }
    }
}
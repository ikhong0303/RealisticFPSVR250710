using UnityEngine;
using MikeNspired.XRIStarterKit;

/// <summary>
/// 자기 위치에서 둥둥 떠다니고, 사라질 때(Die 또는 Destroy 전) 파티클 생성.
/// EnemyHealth의 OnTakeDamage에 이벤트 연결.
/// </summary>
public class IdleFloating : MonoBehaviour
{
    [Header("EnemyHealth 연결 (Inspector에서 드래그)")]
    public EnemyHealth bossHealth; // Inspector에 노출

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

    private EnemyHealth health;

    void Awake()
    {
        initialPosition = transform.position;
        phaseX = Random.Range(0f, Mathf.PI * 2f);
        phaseZ = Random.Range(0f, Mathf.PI * 2f);

        // Inspector 연결 우선, 없으면 GetComponent
        if (!bossHealth)
            bossHealth = GetComponent<EnemyHealth>();
        health = bossHealth;

        if (health != null)
            health.OnTakeDamage += OnTakeDamage;
        else
            Debug.LogError("[IdleFloating] EnemyHealth 컴포넌트가 없습니다!", this);
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (isDead) return;
        if (!deathEffectPrefab) return;
        if (health != null)
            health.OnTakeDamage -= OnTakeDamage;

        Vector3 effectPos = transform.position + Vector3.up * 0.5f;
        GameObject effect = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

        var ps = effect.GetComponent<ParticleSystem>();
        if (ps) ps.Play();
        Destroy(effect, deathEffectDuration);
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
    /// EnemyHealth에 연결된 데미지 이벤트 (보스처럼 커스텀 효과 가능)
    /// </summary>
    private void OnTakeDamage(float damage)
    {
        Debug.Log($"[IdleFloating] {name}가 데미지 {damage}를 받음!");
        // 데미지 효과 등 원하는 기능 추가 가능
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
            Vector3 effectPos = transform.position + Vector3.up * 0.5f;
            GameObject effect = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

            var ps = effect.GetComponent<ParticleSystem>();
            if (ps) ps.Play();
            Destroy(effect, deathEffectDuration);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 체력 직접 접근 예시
    /// </summary>
    public float GetCurrentHealth()
    {
        if (bossHealth != null)
        {
            var field = typeof(EnemyHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                return (float)field.GetValue(bossHealth);
        }
        return -1f;
    }
}
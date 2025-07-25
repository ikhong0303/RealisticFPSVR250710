using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorUnit
{
    public Transform door;
    public Vector3 targetPosition;
    public float moveSpeed = 2f;

    [HideInInspector] public Vector3 closedPosition;
}

public class OpenCloseTriggerPoint : MonoBehaviour
{
    [Header("모드 설정")]
    public bool isOpenMode = true; // true: 열기, false: 닫기

    [Header("문 리스트")]
    public List<DoorUnit> doors = new List<DoorUnit>();

    [Header("파티클")]
    public ParticleSystem defaultParticle;
    public ParticleSystem triggeredParticle;

    private void Awake()
    {
        foreach (var d in doors)
            if (d.door != null)
                d.closedPosition = d.door.position;

        if (defaultParticle) defaultParticle.Play();
        if (triggeredParticle) triggeredParticle.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (defaultParticle) defaultParticle.Stop();
        if (triggeredParticle) triggeredParticle.Play();

        StopAllCoroutines(); // 재진입시 중복방지

        if (isOpenMode)
            StartCoroutine(OpenDoors());
        else
            StartCoroutine(CloseDoors());
    }

    private void OnTriggerExit(Collider other)
    {
        // 원하면 파티클 다시 바꾸기 (선택)
        if (defaultParticle) defaultParticle.Play();
        if (triggeredParticle) triggeredParticle.Stop();
    }

    IEnumerator OpenDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            var d = doors[i];
            if (d.door != null)
                yield return StartCoroutine(MoveDoor(d.door, d.door.position, d.targetPosition, d.moveSpeed));
        }
    }

    IEnumerator CloseDoors()
    {
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            var d = doors[i];
            if (d.door != null)
                yield return StartCoroutine(MoveDoor(d.door, d.door.position, d.closedPosition, d.moveSpeed));
        }
    }

    IEnumerator MoveDoor(Transform door, Vector3 from, Vector3 to, float speed)
    {
        float dist = Vector3.Distance(from, to);
        float duration = dist / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (door)
                door.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (door)
            door.position = to;
    }
}
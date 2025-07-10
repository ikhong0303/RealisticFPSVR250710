using UnityEngine;

public class MakeLine : MonoBehaviour
{

    public Transform[] ropePoints; // 큐브, 공1~공4의 Transform들을 순서대로 넣기
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = ropePoints.Length;
    }

    void LateUpdate()
    {
        for (int i = 0; i < ropePoints.Length; i++)
        {
            lineRenderer.SetPosition(i, ropePoints[i].position);
        }
    }
}



using UnityEngine;

public class MakeLine : MonoBehaviour
{

    public Transform[] ropePoints; // ť��, ��1~��4�� Transform���� ������� �ֱ�
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



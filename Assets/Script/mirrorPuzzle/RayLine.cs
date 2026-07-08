using System.Collections.Generic;
using UnityEngine;

public class RayLine
{
    public Vector2 startPoint;
    public Vector2 startDir;

    Vector2 curDir;
    Vector2 curPoint;

    List<Vector2> points = new List<Vector2>();

    public void calculateWay()
{
    points.Clear();
    points.Add(startPoint);

    curDir = startDir.normalized;
    curPoint = startPoint;

    for (int i = 0; i < 20; i++)
    {
        RaycastHit2D hit = Physics2D.Raycast(curPoint, curDir, 20f);

        if (hit)
        {
            FlowManager.Instance.WriteLog($"Hit {i}: {hit.collider.name} | tag: {hit.collider.tag} | distance: {hit.distance} | point: {hit.point}");

            points.Add(hit.point);

            if (hit.collider.CompareTag("Target"))
            {
                FlowManager.Instance.Clear();
                break;
            }
            else if (hit.collider.CompareTag("Boundary"))
            {
                break;
            }
            else if (hit.collider.CompareTag("Mirror"))
            {
                Mirror hitM = hit.transform.GetComponent<Mirror>();

                curDir = hitM.calculateReflexDgree(curDir).normalized;
                curPoint = hit.point + curDir * 0.02f;
            }
        }
        else
        {
            FlowManager.Instance.WriteLog($"No hit after point: {curPoint} | dir: {curDir}");
            points.Add(curPoint + curDir * 20f);
            break;
        }
    }

    DrawLine();
}

    LineRenderer lineRenderer;
    void DrawLine()
    {
        if (lineRenderer == null)
        {
            lineRenderer = MirrorManager.Instance.GetComponent<LineRenderer>();
        }

        lineRenderer.positionCount = points.Count;
        
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
    }
}

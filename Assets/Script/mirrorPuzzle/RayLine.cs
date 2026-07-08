using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;
using static UnityEngine.RuleTile.TilingRuleOutput;

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

        curDir = startDir;
        curPoint = startPoint;

        for (int i = 0; i < 20; i++)
        {
            Ray ray = new Ray(curPoint, curDir);
            RaycastHit2D hit = Physics2D.Raycast(curPoint, curDir, 20f);
            Debug.DrawRay(curPoint, curDir.normalized * 20f, Color.red, 1f);
            if (hit)
            {
                if (hit.transform != null)
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

                    curDir = hitM.calculateReflexDgree(curDir); 
                    curPoint = hit.point + curDir.normalized * 0.02f;
                }
            }
            else
            {
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

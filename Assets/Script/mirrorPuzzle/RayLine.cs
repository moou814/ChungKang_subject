using System.Collections.Generic;
using UnityEngine;

public class RayLine
{
    public Vector2 startPoint;
    public Vector2 startDir;
    public BeamColor beamColor;

    public Collider2D ignoreCollider;

    Vector2 curDir;
    Vector2 curPoint;

    List<Vector2> points = new List<Vector2>();

    RaycastHit2D GetValidHit(Vector2 origin, Vector2 dir, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, distance);

        RaycastHit2D bestHit = default;
        float bestDistance = Mathf.Infinity;

        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null)
                continue;

            if (h.collider == ignoreCollider)
                continue;

            if (h.distance < 0.01f)
                continue;

            if (h.distance < bestDistance)
            {
                bestHit = h;
                bestDistance = h.distance;
            }
        }

        return bestHit;
    }

    public void calculateWay()
    {
        points.Clear();
        points.Add(startPoint);

        curDir = startDir.normalized;
        curPoint = startPoint;

        for (int i = 0; i < 20; i++)
        {
            RaycastHit2D hit = GetValidHit(curPoint, curDir, 20f);

            if (hit)
            {
                FlowManager.Instance.WriteLog($"Hit {i}: {hit.collider.name} | tag: {hit.collider.tag} | distance: {hit.distance} | point: {hit.point}");
                
                if (hit.collider == null) continue;
                if (hit.collider == ignoreCollider) continue;

                points.Add(hit.point);

                if (hit.collider.CompareTag("Target"))
                {
                    hit.transform.GetComponent<Target>().isClear = true;
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
                else if (hit.collider.CompareTag("Prism"))
                {
                    Vector2 nextPoint;
                    Vector2 nextDir;

                    nextDir = new Vector2(-curDir.y, curDir.x);
                    nextPoint = hit.point + nextDir * 0.1f;
                    MirrorManager.Instance.newRay(nextPoint, nextDir, BeamColor.Red, hit.collider);

                    nextDir = new Vector2(curDir.x, curDir.y);
                    nextPoint = hit.point + nextDir * 0.1f;
                    MirrorManager.Instance.newRay(nextPoint, nextDir, BeamColor.Green, hit.collider);

                    nextDir = new Vector2(curDir.y, -curDir.x);
                    nextPoint = hit.point + nextDir * 0.1f;
                    MirrorManager.Instance.newRay(nextPoint, nextDir, BeamColor.Blue, hit.collider);
                    break;
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

    void DrawLine()
    {
        LineRenderer rend = MirrorManager.Instance.lineRenderers[0];

        switch (beamColor)
        {
            case BeamColor.White: rend = MirrorManager.Instance.lineRenderers[0]; break;
            case BeamColor.Red: rend = MirrorManager.Instance.lineRenderers[0]; break;
            case BeamColor.Green: rend = MirrorManager.Instance.lineRenderers[1]; break;
            case BeamColor.Blue: rend = MirrorManager.Instance.lineRenderers[2]; break;
        }

        rend.positionCount = points.Count;
        
        for (int i = 0; i < points.Count; i++)
        {
            rend.SetPosition(i, points[i]);
        }
    }
}

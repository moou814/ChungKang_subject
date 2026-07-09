using System.Collections.Generic;
using UnityEngine;

public class RayLine
{
    private const int MaxReflectionCount = 20;
    private const float RayDistance = 20f;
    private const float MinHitDistance = 0.01f;
    private const float MirrorOffset = 0.02f;
    private const float PrismOffset = 0.1f;

    public Vector2 startPoint;
    public Vector2 startDir;
    public BeamColor beamColor;
    public Collider2D ignoreCollider;

    private readonly List<Vector2> points = new List<Vector2>();

    public void CalculateWay()
    {
        points.Clear();
        points.Add(startPoint);

        Vector2 currentDirection = startDir.normalized;
        Vector2 currentPoint = startPoint;

        for (int i = 0; i < MaxReflectionCount; i++)
        {
            RaycastHit2D hit = GetValidHit(currentPoint, currentDirection, RayDistance);

            if (!hit)
            {
                FlowManager.Instance.WriteLog($"No hit after point: {currentPoint} | dir: {currentDirection}");
                points.Add(currentPoint + currentDirection * RayDistance);
                break;
            }

            FlowManager.Instance.WriteLog(
                $"Hit {i}: {hit.collider.name} | tag: {hit.collider.tag} | distance: {hit.distance} | point: {hit.point}"
            );

            points.Add(hit.point);

            if (hit.collider.CompareTag("Target"))
            {
                Target target = hit.transform.GetComponent<Target>();
                target.TryClear(beamColor);
                break;
            }

            if (hit.collider.CompareTag("Boundary"))
            {
                break;
            }

            if (hit.collider.CompareTag("Mirror"))
            {
                Mirror mirror = hit.transform.GetComponent<Mirror>();
                currentDirection = mirror.CalculateReflectDirection(currentDirection).normalized;
                currentPoint = hit.point + currentDirection * MirrorOffset;
                continue;
            }

            if (hit.collider.CompareTag("Prism"))
            {
                AddPrismRays(hit.point, currentDirection, hit.collider);
                break;
            }
        }

        DrawLine();
    }

    // Kept for older code references.
    public void calculateWay()
    {
        CalculateWay();
    }

    private RaycastHit2D GetValidHit(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);

        RaycastHit2D bestHit = default;
        float bestDistance = Mathf.Infinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == ignoreCollider || hit.distance < MinHitDistance)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = hit.distance;
            }
        }

        return bestHit;
    }

    private void AddPrismRays(Vector2 hitPoint, Vector2 incomingDirection, Collider2D prismCollider)
    {
        AddSplitRay(new Vector2(-incomingDirection.y, incomingDirection.x), BeamColor.Red, hitPoint, prismCollider);
        AddSplitRay(incomingDirection, BeamColor.Green, hitPoint, prismCollider);
        AddSplitRay(new Vector2(incomingDirection.y, -incomingDirection.x), BeamColor.Blue, hitPoint, prismCollider);
    }

    private void AddSplitRay(Vector2 direction, BeamColor color, Vector2 hitPoint, Collider2D prismCollider)
    {
        Vector2 normalizedDirection = direction.normalized;
        MirrorManager.Instance.AddRay(
            hitPoint + normalizedDirection * PrismOffset,
            normalizedDirection,
            color,
            prismCollider
        );
    }

    private void DrawLine()
    {
        LineRenderer renderer = MirrorManager.Instance.GetLineRenderer(beamColor);
        if (renderer == null)
        {
            return;
        }

        renderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            renderer.SetPosition(i, points[i]);
        }
    }
}

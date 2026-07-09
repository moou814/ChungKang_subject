using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광선 하나의 경로를 계산하는 클래스 (MonoBehaviour가 아닌 순수 C# 클래스).
/// 시작점에서 레이캐스트를 반복하며 거울에서는 반사, 프리즘에서는 3갈래 분기,
/// 타겟/경계에서는 정지한다. 계산된 꼭짓점들을 LineRenderer로 그린다.
/// </summary>
public class RayLine
{
    private const int MaxBounces = 20;          // 무한 반사 방지
    private const float RayDistance = 20f;      // 한 번에 쏘는 최대 거리
    private const float MinHitDistance = 0.01f; // 표면에서 다시 시작할 때 자기 자신을 맞는 것 방지
    private const float SurfaceOffset = 0.02f;  // 반사 후 표면에서 살짝 띄우는 거리

    private readonly Vector2 startPoint;
    private readonly Vector2 startDir;
    private readonly BeamColor beamColor;
    private readonly Collider2D ignoreCollider; // 프리즘 분기 광선이 부모 프리즘을 다시 맞지 않도록

    private readonly List<Vector2> points = new List<Vector2>();

    // RaycastAll 대신 미리 만든 버퍼를 재사용해 GC 할당을 없앤다
    private static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

    // RaycastAll과 동일한 조건(기본 레이어, 트리거 설정)의 필터
    private static readonly ContactFilter2D rayFilter = CreateRayFilter();

    private static ContactFilter2D CreateRayFilter()
    {
        ContactFilter2D filter = default;
        filter.useTriggers = Physics2D.queriesHitTriggers;
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        return filter;
    }

    public RayLine(Vector2 startPoint, Vector2 startDir, BeamColor beamColor, Collider2D ignoreCollider = null)
    {
        this.startPoint = startPoint;
        this.startDir = startDir.normalized;
        this.beamColor = beamColor;
        this.ignoreCollider = ignoreCollider;
    }

    /// <summary>원점에서 dir 방향으로 쏜 것 중, 무시 대상을 제외한 가장 가까운 히트를 찾는다.</summary>
    private RaycastHit2D GetValidHit(Vector2 origin, Vector2 dir, float distance)
    {
        int hitCount = Physics2D.Raycast(origin, dir, rayFilter, hitBuffer, distance);

        RaycastHit2D bestHit = default;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hitBuffer[i];

            if (hit.collider == null) continue;
            if (hit.collider == ignoreCollider) continue;
            if (hit.distance < MinHitDistance) continue;

            if (hit.distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = hit.distance;
            }
        }

        return bestHit;
    }

    /// <summary>광선 경로를 처음부터 끝까지 계산하고 그린다.</summary>
    public void CalculateWay()
    {
        points.Clear();
        points.Add(startPoint);

        Vector2 curDir = startDir;
        Vector2 curPoint = startPoint;

        for (int i = 0; i < MaxBounces; i++)
        {
            RaycastHit2D hit = GetValidHit(curPoint, curDir, RayDistance);

            if (!hit)
            {
                FlowManager.Instance.WriteLog($"No hit after point: {curPoint} | dir: {curDir}");
                points.Add(curPoint + curDir * RayDistance);
                break;
            }

            FlowManager.Instance.WriteLog(
                $"Hit {i}: {hit.collider.name} | tag: {hit.collider.tag} | point: {hit.point}");

            points.Add(hit.point);

            if (hit.collider.CompareTag("Target"))
            {
                if (hit.transform.TryGetComponent(out Target target))
                {
                    target.isClear = true;
                }
                break;
            }

            if (hit.collider.CompareTag("Boundary"))
            {
                break;
            }

            if (hit.collider.CompareTag("Mirror"))
            {
                // 반사 벡터 공식으로 새 진행 방향을 얻고, 표면에서 살짝 띄워 다시 진행
                if (!hit.transform.TryGetComponent(out Mirror mirror)) break;

                curDir = mirror.CalculateReflection(curDir);
                curPoint = hit.point + curDir * SurfaceOffset;
                continue;
            }

            if (hit.collider.CompareTag("Prism"))
            {
                // 프리즘: 진행 방향 기준 왼쪽 수직(Red), 직진(Green), 오른쪽 수직(Blue)으로 분기.
                // 수직 벡터는 (x, y) -> (-y, x) 회전 공식을 이용한다.
                SpawnSplitRay(new Vector2(-curDir.y, curDir.x), hit, BeamColor.Red);
                SpawnSplitRay(curDir, hit, BeamColor.Green);
                SpawnSplitRay(new Vector2(curDir.y, -curDir.x), hit, BeamColor.Blue);
                break;
            }
        }

        DrawLine();
    }

    private static void SpawnSplitRay(Vector2 dir, RaycastHit2D hit, BeamColor color)
    {
        Vector2 origin = hit.point + dir * 0.1f;
        MirrorManager.Instance.NewRay(origin, dir, color, hit.collider);
    }

    /// <summary>계산된 꼭짓점들을 색상별 LineRenderer에 반영한다. (White와 Red는 같은 렌더러를 공유)</summary>
    private void DrawLine()
    {
        LineRenderer rend;

        switch (beamColor)
        {
            case BeamColor.Green: rend = MirrorManager.Instance.lineRenderers[1]; break;
            case BeamColor.Blue: rend = MirrorManager.Instance.lineRenderers[2]; break;
            default: rend = MirrorManager.Instance.lineRenderers[0]; break; // White, Red
        }

        if (rend == null) return;

        rend.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            rend.SetPosition(i, points[i]);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public enum HitKind
{
    None,
    Mirror,
    Target,
    Boundary,
    Prism
}

public enum BeamColor { White, Red, Green, Blue }

/// <summary>
/// 거울 퍼즐 매니저.
/// 스테이지 데이터를 2차원 배열(HitKind)로 읽어 맵을 구성하고,
/// 광선(RayLine)들의 경로 계산과 클리어 판정을 총괄한다.
/// 거울이 회전할 때마다 DrawRay()가 호출되어 광선 전체를 다시 계산한다.
/// </summary>
public class MirrorManager : MonoBehaviour
{
    public static MirrorManager Instance { get; private set; }

    private const int MaxRayCount = 20;   // 프리즘 분기로 광선이 무한히 늘어나는 것을 방지

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private MirrorStageData[] stageData;

    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject boundaryPrefab;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject prismPrefab;

    [FormerlySerializedAs("puzzleB")]
    [SerializeField] private Transform puzzleRoot;

    /// <summary>[0]: White/Red, [1]: Green, [2]: Blue 광선용</summary>
    public LineRenderer[] lineRenderers = new LineRenderer[3];

    public int stage;

    private HitKind[,] map;
    private readonly List<RayLine> rays = new List<RayLine>();
    private readonly List<Target> targets = new List<Target>();

    // 시작 광선 (DrawRay마다 이 광선부터 다시 계산한다. 매번 새로 만들지 않고 재사용)
    private RayLine primaryRay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        stage = FlowManager.Instance.stage;

        if (stage < 0 || stage >= stageData.Length || stageData[stage] == null)
        {
            FlowManager.Instance.WriteLog($"[Error] No Mirror stage data for stage {stage}");
            return;
        }

        BuildMap();
        DrawRay();
    }

    /// <summary>스테이지 데이터의 1차원 배열을 2차원 그리드로 읽어 오브젝트를 배치한다.</summary>
    private void BuildMap()
    {
        MirrorStageData data = stageData[stage];
        int width = data.mapSize[0];
        int height = data.mapSize[1];

        map = new HitKind[height, width];
        int targetIdx = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map[y, x] = data.map[y * width + x];
                Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(x, -y));

                switch (map[y, x])
                {
                    case HitKind.Mirror:
                        Instantiate(mirrorPrefab, puzzleRoot).transform.position = worldPos;
                        break;

                    case HitKind.Boundary:
                        Instantiate(boundaryPrefab, puzzleRoot).transform.position = worldPos;
                        break;

                    case HitKind.Prism:
                        Instantiate(prismPrefab, puzzleRoot).transform.position = worldPos;
                        break;

                    case HitKind.Target:
                        GameObject targetObj = Instantiate(targetPrefab, puzzleRoot);
                        targetObj.transform.position = worldPos;

                        Target target = targetObj.GetComponent<Target>();
                        target.targetColor = data.targets[targetIdx++];
                        target.isClear = false;
                        targets.Add(target);

                        targetObj.SetActive(true);
                        break;
                }
            }
        }

        primaryRay = new RayLine(tilemap.CellToWorld(data.rayStartPoint), data.rayStartDir, BeamColor.White);
    }

    /// <summary>프리즘 등이 새 광선을 만들 때 호출한다. (같은 프레임의 DrawRay 루프에서 이어서 계산된다)</summary>
    public void NewRay(Vector2 startPoint, Vector2 startDir, BeamColor color, Collider2D ignoreCollider = null)
    {
        rays.Add(new RayLine(startPoint, startDir, color, ignoreCollider));
    }

    /// <summary>
    /// 광선 전체를 처음부터 다시 계산한다.
    /// 이전 계산에서 만들어진 분기 광선을 모두 버리고 시작 광선 하나만 남긴 뒤,
    /// 계산 도중 프리즘이 추가하는 광선까지 순서대로 처리한다.
    /// </summary>
    public void DrawRay()
    {
        foreach (Target target in targets)
        {
            target.isClear = false;
        }

        foreach (LineRenderer lineRenderer in lineRenderers)
        {
            if (lineRenderer != null) lineRenderer.positionCount = 0;
        }

        rays.Clear();
        rays.Add(primaryRay);

        // 루프 도중 프리즘이 rays에 광선을 추가하므로 Count를 매번 다시 평가한다
        for (int i = 0; i < rays.Count && i < MaxRayCount; i++)
        {
            rays[i].CalculateWay();
        }

        CheckClear();
    }

    private void CheckClear()
    {
        if (targets.Count == 0) return;

        foreach (Target target in targets)
        {
            if (!target.isClear) return;
        }

        FlowManager.Instance.Clear();
    }
}

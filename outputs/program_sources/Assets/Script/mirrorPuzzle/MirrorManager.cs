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

public enum BeamColor
{
    White,
    Red,
    Green,
    Blue
}

public class MirrorManager : MonoBehaviour
{
    private const int MaxRayCount = 20;

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private MirrorStageData[] stageData;
    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject boundaryPrefab;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject prismPrefab;
    [SerializeField, FormerlySerializedAs("puzzleB")] private Transform puzzleBoard;

    public int stage;
    public LineRenderer[] lineRenderers = new LineRenderer[3];
    public static MirrorManager Instance { get; private set; }

    private HitKind[,] map;
    private readonly List<RayLine> rays = new List<RayLine>();
    private readonly List<Target> targets = new List<Target>();

    private Vector2 initialRayStartPoint;
    private Vector2 initialRayDirection;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        stage = FlowManager.Instance.stage;

        if (!TryGetStageData(out MirrorStageData currentStageData))
        {
            return;
        }

        MakeMap(currentStageData);
        DrawRay();
    }

    private bool TryGetStageData(out MirrorStageData data)
    {
        data = null;
        if (stageData == null || stage < 0 || stage >= stageData.Length || stageData[stage] == null)
        {
            FlowManager.Instance.WriteLog($"Mirror stage data missing: {stage}");
            return false;
        }

        data = stageData[stage];
        return true;
    }

    private void MakeMap(MirrorStageData data)
    {
        int width = data.mapSize[0];
        int height = data.mapSize[1];
        int targetIndex = 0;

        map = new HitKind[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                HitKind hitKind = data.map[y * width + x];
                map[y, x] = hitKind;

                Vector3 worldPosition = tilemap.CellToWorld(new Vector3Int(x, -y));
                switch (hitKind)
                {
                    case HitKind.Mirror:
                        Instantiate(mirrorPrefab, worldPosition, Quaternion.identity, puzzleBoard);
                        break;

                    case HitKind.Boundary:
                        Instantiate(boundaryPrefab, worldPosition, Quaternion.identity, puzzleBoard);
                        break;

                    case HitKind.Target:
                        Target target = Instantiate(targetPrefab, worldPosition, Quaternion.identity, puzzleBoard)
                            .GetComponent<Target>();

                        BeamColor targetColor = targetIndex < data.targets.Length
                            ? data.targets[targetIndex]
                            : BeamColor.White;
                        target.Init(targetColor);

                        targets.Add(target);
                        targetIndex++;
                        break;

                    case HitKind.Prism:
                        Instantiate(prismPrefab, worldPosition, Quaternion.identity, puzzleBoard);
                        break;
                }
            }
        }

        initialRayStartPoint = tilemap.CellToWorld(data.rayStartPoint);
        initialRayDirection = data.rayStartDir.normalized;
    }

    public void AddRay(Vector2 startPoint, Vector2 startDirection, BeamColor color, Collider2D ignoreCollider = null)
    {
        rays.Add(new RayLine
        {
            startPoint = startPoint,
            startDir = startDirection.normalized,
            beamColor = color,
            ignoreCollider = ignoreCollider
        });
    }

    // Kept for older code references.
    public void newRay(Vector2 startP, Vector2 startD, BeamColor color, Collider2D ignoreCollider = null)
    {
        AddRay(startP, startD, color, ignoreCollider);
    }

    public void DrawRay()
    {
        ClearTargets();
        ClearLineRenderers();

        rays.Clear();
        AddRay(initialRayStartPoint, initialRayDirection, BeamColor.White);

        for (int i = 0; i < rays.Count && i < MaxRayCount; i++)
        {
            rays[i].CalculateWay();
        }

        CheckClear();
    }

    public LineRenderer GetLineRenderer(BeamColor color)
    {
        int index = color switch
        {
            BeamColor.Green => 1,
            BeamColor.Blue => 2,
            _ => 0
        };

        return index >= 0 && index < lineRenderers.Length ? lineRenderers[index] : null;
    }

    private void ClearTargets()
    {
        foreach (Target target in targets)
        {
            target.ResetClear();
        }
    }

    private void ClearLineRenderers()
    {
        foreach (LineRenderer lineRenderer in lineRenderers)
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

    private void CheckClear()
    {
        if (targets.Count == 0)
        {
            return;
        }

        foreach (Target target in targets)
        {
            if (!target.isClear)
            {
                return;
            }
        }

        FlowManager.Instance.Clear();
    }
}

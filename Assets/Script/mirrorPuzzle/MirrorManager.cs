using System.Collections.Generic;
using UnityEngine;
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

public class MirrorManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    public int stage;
    [SerializeField] private MirrorStageData[] stageData;
    public static MirrorManager Instance { get; private set; }

    public LineRenderer[] lineRenderers = new LineRenderer[3];

    List<bool> isClear;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        stage = FlowManager.Instance.stage;

        makeMap();
        DrawRay();
    }

    HitKind[,] map;
    List<RayLine> rays = new List<RayLine>() { };

    [SerializeField] GameObject mirrorPrefab;
    [SerializeField] GameObject boundaryPrefab;
    [SerializeField] GameObject targetPrefab;
    [SerializeField] GameObject prismPrefab;

    [SerializeField] private Transform puzzleB;
    int idx = 0;

    List<Target> targets = new List<Target>();

    void targetClear()
    {
        foreach(Target t in targets)
        {
            t.isClear = false;
        }
    }

    void makeMap()
    {
        map = new HitKind[stageData[stage].mapSize[1], stageData[stage].mapSize[0]];

        for (int y = 0; y < stageData[stage].mapSize[1]; y++)
        {
            for (int x = 0; x < stageData[stage].mapSize[0]; x++)
            {
                map[y, x] = stageData[stage].map[y * stageData[stage].mapSize[0] + x];

                switch (map[y, x])
                {
                    case HitKind.Mirror:
                        GameObject m = Instantiate(mirrorPrefab, puzzleB);
                        m.transform.position = tilemap.CellToWorld(new Vector3Int(x, -y));
                        break;

                    case HitKind.Boundary:
                        GameObject b = Instantiate(boundaryPrefab, puzzleB);
                        b.transform.position = tilemap.CellToWorld(new Vector3Int(x, -y));
                        break;

                    case HitKind.Target:
                        GameObject t = Instantiate(targetPrefab, puzzleB);
                        targets.Add(t.GetComponent<Target>());

                        t.transform.position = tilemap.CellToWorld(new Vector3Int(x, -y));
                        targets[^1].targetColor = stageData[stage].targets[idx++];
                        targets[^1].isClear = false;

                        t.SetActive(true);

                        break;

                    case HitKind.Prism:
                        GameObject p = Instantiate(prismPrefab, puzzleB);
                        p.transform.position = tilemap.CellToWorld(new Vector3Int(x, -y));
                        break;

                }
            }
        }

        newRay(tilemap.CellToWorld(stageData[stage].rayStartPoint), stageData[stage].rayStartDir, BeamColor.White);
    }

    public void newRay(Vector2 startP, Vector2 startD, BeamColor color, Collider2D ignoreCollider = null)
    {
        RayLine r = new RayLine();

        r.startPoint = startP;
        r.startDir = startD.normalized;
        r.beamColor = color;
        r.ignoreCollider = ignoreCollider;

        rays.Add(r);
    }

    void checkClear()
    {
        bool c = true;

        for (int i = 0; i < targets.Count; i++)
        {
            if (!targets[i].isClear) {
                c = false; 
                break;
            }
        }

        if (c) FlowManager.Instance.Clear();
    }

    public void DrawRay()
    {
        targetClear();

        for (int i = 0; i < rays.Count && i < 20; i++)
        {
            rays[i].calculateWay();
        }

        checkClear();
    }
}

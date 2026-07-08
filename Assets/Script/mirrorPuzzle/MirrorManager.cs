using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum HitKind
    {
        None,
        Mirror,
        Target,
        Boundary,
        Prism
    }

public class MirrorManager : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    public int stage;
    [SerializeField] private MirrorStageData[] stageData;
    public static MirrorManager Instance { get; private set; }

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

    [SerializeField] private Transform puzzleB;
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
                        t.transform.position = tilemap.CellToWorld(new Vector3Int(x, -y));
                        break;

                }
            }
        }

        RayLine r = new RayLine();
        r.startPoint = tilemap.CellToWorld(stageData[stage].rayStartPoint);
        r.startDir = stageData[stage].rayStartDir;

        rays.Add(r);
    }

    public void DrawRay()
    {
        foreach(var r in rays)
        {
            r.calculateWay();
        }
    }
}

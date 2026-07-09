using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum BlockKind
{
    L,      // └ : 두 방향이 꺾여 연결
    I,      // ─ : 양쪽 직선 연결
    Plus,   // + : 네 방향 모두 연결
    T       // ┤ : 세 방향 연결
}

/// <summary>
/// 파이프 퍼즐 매니저.
/// 맵을 2차원 배열(그리드)로 관리하고, 블록이 회전할 때마다
/// 시작 블록에서 스택 기반 DFS로 연결된 블록을 탐색해
/// 모든 도착 블록에 도달하면 클리어로 판정한다.
/// </summary>
public class PipePuzzleManager : MonoBehaviour
{
    public static PipePuzzleManager Instance { get; private set; }

    /// <summary>방향 인덱스별 (행, 열) 오프셋. 0:오른쪽, 1:아래, 2:왼쪽, 3:위 (시계방향 순서)</summary>
    public static readonly int[,] Dir = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

    /// <summary>블록 종류별 기본(회전 0) 통로 방향. 빈 슬롯은 -1.</summary>
    public static readonly int[,] ConnectedDir =
    {
        { 0, 3, -1, -1 },   // L    : 오른쪽, 위
        { 0, 2, -1, -1 },   // I    : 오른쪽, 왼쪽
        { 0, 1, 2, 3 },     // Plus : 네 방향
        { 1, 2, 3, -1 }     // T    : 아래, 왼쪽, 위
    };

    [FormerlySerializedAs("puzzleB")]
    [SerializeField] private Transform puzzleRoot;
    [SerializeField] private PipeStageData[] stageData;
    [FormerlySerializedAs("blockPrefabs")]
    [SerializeField] private GameObject blockPrefab;

    public int stage;

    private int rows;                          // 맵 세로 크기 (mapSize[0])
    private int cols;                          // 맵 가로 크기 (mapSize[1])
    private PipePuzzleBlock[,] blocks;
    private bool[,] visited;

    private (int y, int x) startCell;
    private (int y, int x)[] goalCells;

    private readonly StringBuilder logBuilder = new StringBuilder();

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
            FlowManager.Instance.WriteLog($"[Error] No Pipe stage data for stage {stage}");
            return;
        }

        BuildStage();
        CheckClear();
    }

    /// <summary>스테이지 데이터를 읽어 그리드와 블록 오브젝트를 구성한다.</summary>
    private void BuildStage()
    {
        PipeStageData data = stageData[stage];

        rows = data.mapSize[0];
        cols = data.mapSize[1];

        blocks = new PipePuzzleBlock[rows, cols];
        visited = new bool[rows, cols];

        startCell = (data.start[0], data.start[1]);

        goalCells = new (int y, int x)[data.end.Length / 2];
        for (int i = 0; i < goalCells.Length; i++)
        {
            goalCells[i] = (data.end[i * 2], data.end[i * 2 + 1]);
        }

        // 블록 종류별 스프라이트는 한 번만 로드해서 재사용
        Sprite[] blockSprites = new Sprite[4];
        for (int i = 0; i < blockSprites.Length; i++)
        {
            blockSprites[i] = Resources.Load<Sprite>($"pipePuzzle/image/block{i}");
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                BlockKind kind = data.map[y * cols + x];

                GameObject blockObj = Instantiate(blockPrefab, puzzleRoot);
                blockObj.transform.position = new Vector3(
                    (x - cols / 2 - 0.5f) * 1.5f,
                    (rows / 2 - y - 1.5f) * 1.5f);

                blocks[y, x] = blockObj.GetComponent<PipePuzzleBlock>();
                blocks[y, x].kind = kind;
                blockObj.GetComponent<Image>().sprite = blockSprites[(int)kind];
            }
        }

        // 시작/도착/고정 블록은 회전 불가
        blocks[startCell.y, startCell.x].isFixed = true;

        foreach ((int y, int x) in goalCells)
        {
            blocks[y, x].isFixed = true;
        }

        if (data.fixedBlocks != null)
        {
            for (int i = 0; i + 1 < data.fixedBlocks.Length; i += 2)
            {
                blocks[data.fixedBlocks[i], data.fixedBlocks[i + 1]].isFixed = true;
            }
        }
    }

    /// <summary>
    /// 시작 블록에서 DFS로 통로가 서로 마주보는 블록들을 탐색해 연결 상태를 갱신하고,
    /// 모든 도착 블록에 도달했는지 검사한다. 블록이 회전할 때마다 호출된다.
    /// </summary>
    public void CheckClear()
    {
        if (blocks == null) return;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                visited[y, x] = false;
            }
        }

        List<(int y, int x)> stack = new List<(int y, int x)> { startCell };
        visited[startCell.y, startCell.x] = true;

        while (stack.Count > 0)
        {
            WriteStackLog(stack);

            (int y, int x) cur = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            int[] curOpenDirs = blocks[cur.y, cur.x].GetOpenDirections();

            for (int i = 0; i < 4; i++)
            {
                int outDir = curOpenDirs[i];
                if (outDir == -1) continue;

                (int y, int x) next = (cur.y + Dir[outDir, 0], cur.x + Dir[outDir, 1]);

                // 맵 범위 밖이거나 이미 방문한 블록은 건너뛴다
                if (next.y < 0 || next.y >= rows || next.x < 0 || next.x >= cols) continue;
                if (visited[next.y, next.x]) continue;

                // 이웃 블록에 내가 나간 방향과 정반대((outDir + 2) % 4)의 통로가 열려 있어야 연결된다
                int inDir = (outDir + 2) % 4;
                foreach (int neighborDir in blocks[next.y, next.x].GetOpenDirections())
                {
                    if (neighborDir == inDir)
                    {
                        visited[next.y, next.x] = true;
                        stack.Add(next);
                        break;
                    }
                }
            }
        }

        UpdateCellVisuals();

        foreach ((int y, int x) in goalCells)
        {
            if (!visited[y, x]) return;
        }

        FlowManager.Instance.WriteLog("Clear");
        FlowManager.Instance.Clear();
    }

    /// <summary>연결된(방문한) 블록을 밝게, 나머지는 어둡게 표시한다.</summary>
    private void UpdateCellVisuals()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                blocks[y, x].SetRoadLit(visited[y, x]);
            }
        }
    }

    /// <summary>심사용 디버그: 현재 DFS 스택 내용을 화면 로그로 출력한다.</summary>
    private void WriteStackLog(List<(int y, int x)> stack)
    {
        logBuilder.Clear();
        logBuilder.Append("DFS stack: [");

        foreach ((int y, int x) in stack)
        {
            logBuilder.Append('(').Append(y).Append(',').Append(x).Append(") ");
        }

        logBuilder.Append(']');
        FlowManager.Instance.WriteLog(logBuilder.ToString());
    }
}

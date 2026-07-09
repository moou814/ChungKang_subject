using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum blockKind
{
    L,
    I,
    plus,
    T
}

public class pipePuzzle_Manager : MonoBehaviour
{
    public static readonly int[,] Directions = new int[,]
    {
        { 0, 1 },
        { 1, 0 },
        { 0, -1 },
        { -1, 0 }
    };

    public static readonly int[,] ConnectedDirections = new int[,]
    {
        { 0, 3, -1, -1 },
        { 0, 2, -1, -1 },
        { 0, 1, 2, 3 },
        { 1, 2, 3, -1 }
    };

    // Kept for older code references that used the misspelled field name.
    public static readonly int[,] connetedDir = ConnectedDirections;

    [SerializeField, FormerlySerializedAs("puzzleB")] private Transform puzzleBoard;
    [SerializeField] private StageData[] stageData;
    [SerializeField, FormerlySerializedAs("blockPrefabs")] private GameObject blockPrefab;

    public int stage;
    public pipePuzzle_Block[,] blocks;
    public static pipePuzzle_Manager Instance { get; private set; }

    private blockKind[,] map;
    private bool[,] visited;
    private int[] startBlock;
    private int[,] destinationBlocks;
    private int[,] fixedBlocks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        stage = FlowManager.Instance.stage;

        if (!TryGetStageData(out StageData currentStageData))
        {
            return;
        }

        MakeStage(currentStageData);
        SetUpBlocks(currentStageData);
        ClearCheck();
    }

    private bool TryGetStageData(out StageData data)
    {
        data = null;
        if (stageData == null || stage < 0 || stage >= stageData.Length || stageData[stage] == null)
        {
            FlowManager.Instance.WriteLog($"Pipe stage data missing: {stage}");
            return false;
        }

        data = stageData[stage];
        return true;
    }

    private void MakeStage(StageData data)
    {
        int rowCount = data.mapSize[0];
        int columnCount = data.mapSize[1];

        map = new blockKind[rowCount, columnCount];
        blocks = new pipePuzzle_Block[rowCount, columnCount];
        visited = new bool[rowCount, columnCount];

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                map[row, column] = data.map[row * columnCount + column];
            }
        }

        startBlock = data.start;
        destinationBlocks = ToPairs(data.end);
        fixedBlocks = ToPairs(data.fixedBlocks);
    }

    private void SetUpBlocks(StageData data)
    {
        int rowCount = data.mapSize[0];
        int columnCount = data.mapSize[1];

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                GameObject blockObject = Instantiate(blockPrefab, puzzleBoard);
                blockObject.transform.position = new Vector3(
                    (column - (columnCount / 2f) + 0.5f) * 1.5f,
                    ((rowCount / 2f) - row - 0.5f) * 1.5f
                );

                pipePuzzle_Block block = blockObject.GetComponent<pipePuzzle_Block>();
                block.kind = map[row, column];
                blockObject.GetComponent<Image>().sprite = Resources.Load<Sprite>($"pipePuzzle/image/block{(int)map[row, column]}");

                blocks[row, column] = block;
            }
        }

        SetFixed(startBlock[0], startBlock[1]);
        for (int i = 0; i < destinationBlocks.GetLength(0); i++)
        {
            SetFixed(destinationBlocks[i, 0], destinationBlocks[i, 1]);
        }

        for (int i = 0; i < fixedBlocks.GetLength(0); i++)
        {
            SetFixed(fixedBlocks[i, 0], fixedBlocks[i, 1]);
        }
    }

    private void SetFixed(int row, int column)
    {
        if (IsInside(row, column))
        {
            blocks[row, column].IsFixed = true;
        }
    }

    public void ClearCheck()
    {
        if (!TryGetStageData(out StageData data))
        {
            return;
        }

        ResetVisited(data.mapSize[0], data.mapSize[1]);

        List<int[]> stack = new List<int[]>();
        stack.Add(startBlock);
        visited[startBlock[0], startBlock[1]] = true;

        while (stack.Count > 0)
        {
            WriteLogStack(stack);

            int[] currentPosition = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            int[] currentDirections = blocks[currentPosition[0], currentPosition[1]].GetCanGo();
            for (int i = 0; i < currentDirections.Length; i++)
            {
                int currentDirection = currentDirections[i];
                if (currentDirection == -1)
                {
                    continue;
                }

                int nextRow = currentPosition[0] + Directions[currentDirection, 0];
                int nextColumn = currentPosition[1] + Directions[currentDirection, 1];

                if (!IsInside(nextRow, nextColumn) || visited[nextRow, nextColumn])
                {
                    continue;
                }

                if (CanConnectBack(currentDirection, blocks[nextRow, nextColumn].GetCanGo()))
                {
                    visited[nextRow, nextColumn] = true;
                    stack.Add(new int[] { nextRow, nextColumn });
                }
            }
        }

        UpdateCells(data.mapSize[0], data.mapSize[1]);

        if (AllDestinationsVisited())
        {
            FlowManager.Instance.WriteLog("Clear");
            FlowManager.Instance.Clear();
        }
    }

    // Kept for older code and prefab references.
    public void clearCheck()
    {
        ClearCheck();
    }

    private void ResetVisited(int rowCount, int columnCount)
    {
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                visited[row, column] = false;
            }
        }
    }

    private void UpdateCells(int rowCount, int columnCount)
    {
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                blocks[row, column].SetRoadState(visited[row, column]);
            }
        }
    }

    private bool AllDestinationsVisited()
    {
        for (int i = 0; i < destinationBlocks.GetLength(0); i++)
        {
            if (!visited[destinationBlocks[i, 0], destinationBlocks[i, 1]])
            {
                return false;
            }
        }

        return true;
    }

    private bool IsInside(int row, int column)
    {
        return row >= 0
            && row < blocks.GetLength(0)
            && column >= 0
            && column < blocks.GetLength(1);
    }

    private static bool CanConnectBack(int currentDirection, int[] nextDirections)
    {
        for (int i = 0; i < nextDirections.Length; i++)
        {
            int nextDirection = nextDirections[i];
            if (nextDirection == -1)
            {
                continue;
            }

            bool oppositeRow = Directions[currentDirection, 0] + Directions[nextDirection, 0] == 0;
            bool oppositeColumn = Directions[currentDirection, 1] + Directions[nextDirection, 1] == 0;
            if (oppositeRow && oppositeColumn)
            {
                return true;
            }
        }

        return false;
    }

    private static int[,] ToPairs(int[] flat)
    {
        if (flat == null || flat.Length == 0)
        {
            return new int[0, 2];
        }

        int[,] pairs = new int[flat.Length / 2, 2];
        for (int i = 0; i < pairs.GetLength(0); i++)
        {
            pairs[i, 0] = flat[i * 2];
            pairs[i, 1] = flat[i * 2 + 1];
        }

        return pairs;
    }

    private void WriteLogStack(List<int[]> stack)
    {
        StringBuilder builder = new StringBuilder("DFS stack: [");
        foreach (int[] position in stack)
        {
            builder.Append('[')
                .Append(position[0])
                .Append(", ")
                .Append(position[1])
                .Append("], ");
        }

        builder.Append(']');
        FlowManager.Instance.WriteLog(builder.ToString());
    }
}

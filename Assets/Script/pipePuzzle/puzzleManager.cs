using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public enum blockKind
{
    L,
    I,
    plus,
    T
};

public class pipePuzzle_Manager : MonoBehaviour
{
    static public int[,] dir = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }; // µ¿³²¼­ºÏ
    // 0:¤¤, 1:-, 2:+, 3:¤¿
    static public int[,] connetedDir = new int[,] {
            { 0, 3, -1, -1 },
            { 0, 2, -1, -1 },
            { 0, 1, 2, 3 },
            { 1, 2, 3, -1 }
        };

    [SerializeField] private Transform puzzleB;

    [SerializeField] private StageData[] stageData;
    public int stage;

    blockKind[,] map;

    [SerializeField] private GameObject blockPrefabs;

    int[] startB;
    int[,] desB;
    int[,] fixedB;

    public pipePuzzle_Block[,] blocks;
    public static pipePuzzle_Manager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        stage = FlowManager.Instance.stage;

        makeStage();

        setupBlock();

        clearCheck();
    }

    void makeStage()
    {
        map = new blockKind[stageData[stage].mapSize[0], stageData[stage].mapSize[1]];
        for (int y = 0; y < stageData[stage].mapSize[0]; y++)
        {
            for (int x = 0; x < stageData[stage].mapSize[1]; x++)
            {
                map[y, x] = stageData[stage].map[y * stageData[stage].mapSize[1] + x];
            }
        }

        blocks = new pipePuzzle_Block[stageData[stage].mapSize[0], stageData[stage].mapSize[1]];
        visited = new bool[stageData[stage].mapSize[0], stageData[stage].mapSize[1]];

        startB = stageData[stage].start;
        desB = new int[(int)(stageData[stage].end.Length / 2), 2];

        if (stageData[stage].fixedBlocks != null) 
            fixedB = new int[(int)(stageData[stage].fixedBlocks.Length / 2), 2];

        for (int i = 0; i < (int)(stageData[stage].end.Length / 2); i++)
        {
            desB[i, 0] = stageData[stage].end[i * 2];
            desB[i, 1] = stageData[stage].end[i * 2 + 1];
        }

        if (stageData[stage].fixedBlocks != null)
        {
            for (int i = 0; i < (int)(stageData[stage].fixedBlocks.Length / 2); i++)
            {
                fixedB[i, 0] = stageData[stage].fixedBlocks[i * 2];
                fixedB[i, 1] = stageData[stage].fixedBlocks[i * 2 + 1];
            }
        }

    }

    void setupBlock()
    {
        for (int col = 0; col < stageData[stage].mapSize[0]; col++) 
        {
            for (int row = 0; row < stageData[stage].mapSize[1]; row++)
            {
                GameObject b = Instantiate(blockPrefabs, puzzleB);
                b.transform.position = 
                    new Vector3((row - (stageData[stage].mapSize[1] / 2) - 0.5f) * 1.5f, 
                    (stageData[stage].mapSize[0] / 2 - col -  1.5f) * 1.5f);
                blocks[col, row] = b.GetComponent<pipePuzzle_Block>();

                b.GetComponent<Image>().sprite = Resources.Load<Sprite>($"pipePuzzle/image/block{(int)map[col, row]}");
                blocks[col, row].kind = map[col, row];
            }
        }

        blocks[startB[0], startB[1]].itFixed = true;
        for (int i = 0; i < desB.GetLength(0); i++) { blocks[desB[i, 0], desB[i, 1]].itFixed = true; }

        if (stageData[stage].fixedBlocks != null) 
            for (int i = 0; i < fixedB.GetLength(0); i++) { blocks[fixedB[i, 0], fixedB[i, 1]].itFixed = true; }
    }

    void cellUpdate(bool[,] cell)
    {
        for (int col = 0; col < stageData[stage].mapSize[0]; col++)
        {
            for (int row = 0; row < stageData[stage].mapSize[1]; row++)
            {
                blocks[col, row].onoffRoad(cell[col, row]);
            }
        }
    }

    void WriteLogStack()
    {
        string log = "DFS stack: [";
        foreach (int[] s in stack)
        {
            log += "[";
            foreach (int i in s)
            {
                log += i + ", ";
            }
            log += "], ";
        }
        log += "]";
        
        FlowManager.Instance.WriteLog(log);
    }

    bool[,] visited;
    List<int[]> stack;
    int[] curHasPass;
    int[] nextPos;
    public void clearCheck()
    { 
        stack = new List<int[]>();

        for (int i = 0; i < stageData[stage].mapSize[0]; i++)
        {
            for (int j = 0; j < stageData[stage].mapSize[1]; j++)
            {
                visited[i, j] = false;
            }
        }

        stack.Add(startB); 
        visited[startB[0], startB[1]] = true;

        while (stack.Count > 0) {
            WriteLogStack();

            int[] curPos = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            curHasPass = blocks[curPos[0], curPos[1]].GetCanGo();

            for (int i = 0; i < 4; i++) {
                if (curHasPass[i] == -1) { continue; }

                nextPos = new int[2] { curPos[0] + dir[curHasPass[i], 0], curPos[1] + dir[curHasPass[i], 1] };

                if (nextPos[0] >= 0 && nextPos[0] < stageData[stage].mapSize[0] 
                    && nextPos[1] >= 0 && nextPos[1] < stageData[stage].mapSize[1])
                {
                    if (!visited[nextPos[0], nextPos[1]])
                    {
                        foreach (var hasPass in blocks[nextPos[0], nextPos[1]].GetCanGo())
                        {
                            if (hasPass != -1 &&
                                0 == dir[curHasPass[i], 0] + dir[hasPass, 0] && 0 == dir[curHasPass[i], 1] + dir[hasPass, 1])
                            {
                                visited[nextPos[0], nextPos[1]] = true;
                                stack.Add(new int[] { nextPos[0], nextPos[1] });
                            }
                        }
                    }
                }
            }
        }

        cellUpdate(visited);

        bool f = true;
        for(int i = 0; i < desB.GetLength(0); i++)
        {
            if (!visited[desB[i, 0], desB[i, 1]]) { 
                f = false; 
                break;
            }
        }

        if (f) {
            FlowManager.Instance.WriteLog("Clear");
            FlowManager.Instance.Clear();
        }

        return;
    }
}

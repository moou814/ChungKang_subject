using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
    int stage;

    blockKind[,] map;

    [SerializeField] private GameObject blockPrefubs;

    int[] startB;
    int[] desB;

    public pipePuzzle_Block[,] blocks;

    public bool isClear;
    public static pipePuzzle_Manager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        isClear = false;

        stage = 1;

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
        desB = stageData[stage].end;
       
        setupBlock();

        clearCheck();
    }

    void setupBlock()
    {
        for (int col = 0; col < stageData[stage].mapSize[0]; col++) 
        {
            for (int row = 0; row < stageData[stage].mapSize[1]; row++)
            {
                GameObject b = Instantiate(blockPrefubs, puzzleB);
                b.transform.position = 
                    new Vector3((row - (map.GetLength(0) / 2) - 0.5f) * 1.5f, 
                    (map.GetLength(1) / 2 - col -  1.5f) * 1.5f);
                blocks[col, row] = b.GetComponent<pipePuzzle_Block>();

                b.GetComponent<Image>().sprite = Resources.Load<Sprite>($"pipePuzzle/image/block{(int)map[col, row]}");
                blocks[col, row].kind = map[col, row];
            }
        }

        blocks[startB[0], startB[1]].itFixed = true;
        blocks[desB[0], desB[1]].itFixed = true;
    }

    IEnumerator clear()
    {
        Debug.Log("Clear!");
        yield break;
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

        if (visited[desB[0], desB[1]]) {
            isClear = true;
            StartCoroutine(clear());
        }

        return;
    }
}

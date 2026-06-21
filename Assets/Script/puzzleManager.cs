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

        stage = 0;

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

        IsClear();
    }

    void setupBlock()
    {
        for (int col = 0; col < map.GetLength(0); col++) 
        {
            for (int row = 0; row < map.GetLength(1); row++)
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
    }

    IEnumerator clear()
    {
        Debug.Log("Clear!");
        yield break;
    }

    bool[,] visited;
    public void IsClear()
    {
        List<int[]> stack = new List<int[]>();
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                visited[i, j] = false;
            }
        }

        foreach (var pj in blocks) {
            pj.onoffRoad(false);
        }

        stack.Add(startB); 
        visited[startB[0], startB[1]] = true;
        while (stack.Count > 0) {
            int[] curPos = stack[^1];

            stack.RemoveAt(stack.Count - 1);
            blocks[curPos[0], curPos[1]].onoffRoad(true);

            int [] c = blocks[curPos[0], curPos[1]].GetCanGo();

            for (int i = 0; i < blocks.GetLength(0); i++) {
                if (c[i] != -1
                    && curPos[0] + dir[c[i], 0] >= 0 && curPos[0] + dir[c[i], 0] < blocks.GetLength(0) &&
                    curPos[1] + dir[c[i], 1] >= 0 && curPos[1] + dir[c[i], 1] < blocks.GetLength(1))
                {
                    if (!visited[curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1]])
                    {
                        foreach (var hasPass in blocks[curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1]].GetCanGo())
                        {
                            if (hasPass != -1 &&
                                0 == dir[c[i], 0] + dir[hasPass, 0] && 0 == dir[c[i], 1] + dir[hasPass, 1])
                            {
                                visited[curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1]] = true;
                                stack.Add(new int[] { curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1] });
                            }
                        }
                    }
                }
            }
        }

        if (visited[desB[0], desB[1]]) {
            isClear = true;
            StartCoroutine(clear());
        }

        return; // yield break;
    }
}

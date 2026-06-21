using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

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

    public Transform puzzleB;

    int stage;

    int[,] map;

    public GameObject blockPrefubs;

    int[] startB;
    int[] desB;

    public List<List<pipePuzzle_Block>> block = new List<List<pipePuzzle_Block>>() { };

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

        switch (stage)
        {
            case 1:
                map = new int[,] {
                    { 0, 1, 0, 2, 3, 1 },
                    { 3, 2, 1, 1, 0, 0 },
                    { 3, 3, 2, 0, 1, 2 },
                    { 2, 2, 3, 1, 0, 0 },
                };

                startB = new int[] { 0, 0 };
                desB = new int[] { 3,5 };
                break;
        }
       
        setupBlock();

        IsClear();
    }

    void setupBlock()
    {
        for (int col = 0; col < map.GetLength(0); col++) 
        {
            block.Add(new List<pipePuzzle_Block> { });
            for (int row = 0; row < map.GetLength(1); row++)
            {
                GameObject b = Instantiate(blockPrefubs, puzzleB);
                b.transform.position = 
                    new Vector3((row - (map.GetLength(0) / 2) - 0.5f) * 1.5f, 
                    (map.GetLength(1) / 2 - col -  1.5f) * 1.5f);
                block[col].Add(b.GetComponent<pipePuzzle_Block>());

                b.GetComponent<Image>().sprite = Resources.Load<Sprite>($"image/block{map[col, row]}");
                block[col][row].kind = map[col, row];
            }
        }
    }

    IEnumerator clear()
    {
        Debug.Log("Clear!");
        yield break;
    }

    public void IsClear()
    {
        List<int[]> stack = new List<int[]>();
        List<List<bool>> visited = new List<List<bool>> { }; 
        for (int i = 0; i < map.GetLength(0); i++)
        {
            visited.Add(new List<bool> { });
            for (int j = 0; j < map.GetLength(1); j++)
            {
                visited[i].Add(false);
            }
        }

        foreach (var pi in block) {
            foreach (var pj in pi) { 
                pj.onoffRoad(false);
            }
        }

        stack.Add(startB);
        while (stack.Count > 0) {
            int[] curPos = stack[^1];

            string log = "";
            foreach (var i in stack)
            {
                log += $"[{i[0]}, {i[1]}], ";
            }
            Debug.Log(log);

            visited[curPos[0]][curPos[1]] = true;
            stack.RemoveAt(stack.Count - 1);
            block[curPos[0]][curPos[1]].onoffRoad(true);

            int [] c = block[curPos[0]][curPos[1]].GetCanGo();

            for (int i = 0; i < block.Count; i++) {
                if (c[i] != -1
                    && curPos[0] + dir[c[i], 0] >= 0 && curPos[0] + dir[c[i], 0] < block.Count &&
                    curPos[1] + dir[c[i], 1] >= 0 && curPos[1] + dir[c[i], 1] < block[0].Count)
                {
                    if (!visited[curPos[0] + dir[c[i], 0]][curPos[1] + dir[c[i], 1]])
                    {
                        foreach (var a in block[curPos[0] + dir[c[i], 0]][curPos[1] + dir[c[i], 1]].GetCanGo())
                        {
                            if (a != -1 &&
                                0 == dir[c[i], 0] + dir[a, 0] && 0 == dir[c[i], 1] + dir[a, 1])
                            {
                                stack.Add(new int[] { curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1] });
                            }
                        }
                    }
                }
            }
        }

        if (visited[desB[0]][desB[1]]) {
            isClear = true;
            StartCoroutine(clear());
        }

        return; // yield break;
    }
}

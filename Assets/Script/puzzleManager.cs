using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class elevatorPuzzleManager : MonoBehaviour
{
    static public int[,] dir = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }; // µ¿³²¼­ºÏ
    // 0:¤¤, 1:-, 2:+, 3:¤¿
    static public int[,] connetedDir = new int[,] {
            { 0, 3, -1, -1 },
            { 0, 2, -1, -1 },
            { 0, 1, 2, 3 },
            { 1, 2, 3, -1 }
        };

    Transform puzzleB;

    int stage;

    int[,] map;

    public GameObject blockPrefubs;

    public int[] startB;
    public int[] desB;

    public List<List<elevatorPuzzlePiece>> pieces = new List<List<elevatorPuzzlePiece>>() { };

    public bool isClear;

    private void Awake()
    {
        puzzleB = GameObject.Find("pipePuzzle_block").transform;

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
                desB = new int[] { 6, 4 };
                break;
        }
       
        setupBlock();

        IsClear();
    }

    void setupBlock()
    {
        for (int i = 0; i < map.GetLength(1); i++) 
        {
            pieces.Add(new List<elevatorPuzzlePiece> { });
            for (int j = 0; j < map.Length; j++)
            {
                GameObject b = Instantiate(blockPrefubs, puzzleB);
                b.transform.position = 
                    new Vector3(((map.GetLength(1) / 2) + j - 1) * 1.5f, 
                    (map.Length / 2 + i - 1) * 1.5f);
                pieces[i].Add(b.GetComponent<elevatorPuzzlePiece>());
                b.GetComponent<Image>().sprite = Resources.Load<Sprite>($"image/block{map[j,i]}");
                pieces[i][j].kind = map[j, i];
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
        bool[,] visited = new bool[,] { };

        foreach (var pi in pieces) {
            foreach (var pj in pi) { 
                pj.onoffRoad(false);
            }
        }

        stack.Add(startB);
        while (stack.Count > 0) {
            int[] curPos = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            visited[curPos[0], curPos[1]] = true;
            pieces[curPos[0]][curPos[1]].onoffRoad(true);

            int [] c = pieces[curPos[0]][curPos[1]].GetCanGo();

            for (int i = 0; i < pieces.Count; i++) {
                if (c[i] != -1
                    && curPos[0] + dir[c[i], 0] >= 0 && curPos[0] + dir[c[i], 0] < pieces.Count &&
                    curPos[1] + dir[c[i], 1] >= 0 && curPos[1] + dir[c[i], 1] < pieces[0].Count)
                {
                    if (!visited[curPos[0] + dir[c[i], 0], curPos[1] + dir[c[i], 1]])
                    {
                        foreach (var a in pieces[curPos[0] + dir[c[i], 0]][curPos[1] + dir[c[i], 1]].GetCanGo())
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

        if (visited[desB[0], desB[1]]) {
            StartCoroutine(clear());
        }

        return; // yield break;
    }
}

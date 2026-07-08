using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    bool[] IsPuzzleClear = new bool[3] { false, false, false };
    bool[] IsStageClear = new bool[3] { false, false, false };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        stage = 1;
    }

    [SerializeField] private TMP_Text debugerText;
    int line = 0;
    public void WriteLog(string context)
    {
        if (line >= 12) {
            debugerText.text = "";
            line = 0;
        }
        line++;
        debugerText.text += "\n" + context;
    }

    public void GoPuzzle(int puzzle)
    {
        if (IsPuzzleClear[puzzle]) return;

        RequestScene(puzzle);
    }

    [SerializeField] GameObject selectPanel_stage;
    public void selectStage(int _stage)
    {
        stage = _stage;
        selectPanel_stage.SetActive(false);
        selectPanel_puzzle.SetActive(true);
    }

    int curSceneNum = 3;
    public int stage;
    static string[] Scenes = new string[]
    {
        "PipeScene",
        "BitMaskScene",
        "MirrorScene",
        "MainScene"
    };
    public void RequestScene(int newSceneNum)
    {
        clearPanel.SetActive(false);
        selectPanel_puzzle.SetActive(newSceneNum == 3? true : false);

        curSceneNum = newSceneNum;

        SceneManager.LoadScene(Scenes[newSceneNum], LoadSceneMode.Single);

    }

    [SerializeField] GameObject clearPanel;
    [SerializeField] GameObject selectPanel_puzzle;
    public void Clear()
    {
        IsPuzzleClear[curSceneNum] = true;
        clearPanel.SetActive(true);

        if (IsPuzzleClear[0] && IsPuzzleClear[1] && IsPuzzleClear[2]) 
            IsStageClear[stage] = true;
    }

    private void Update()
    {
        
    }


}

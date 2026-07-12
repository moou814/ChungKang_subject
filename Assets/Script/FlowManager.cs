using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// 게임 전체 흐름을 담당하는 전역 매니저.
/// 씬 전환, 스테이지/퍼즐 클리어 상태 관리, 화면 내 디버그 로그 출력을 맡는다.
/// </summary>
public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    private const int PuzzleCount = 3;      // 0:Pipe, 1:BitMask, 2:Mirror
    private const int MainSceneIndex = 3;   // sceneNames에서 MainScene의 인덱스
    private const int MaxStage = 3;         // 스테이지 번호는 1 ~ MaxStage

    private static readonly string[] sceneNames =
    {
        "PipeScene",
        "BitMaskScene",
        "MirrorScene",
        "MainScene"
    };

    // 현재 스테이지에서 각 퍼즐을 풀었는지 여부 (인덱스 = 퍼즐 번호)
    private readonly bool[] isPuzzleClear = new bool[PuzzleCount];
    // 스테이지 클리어 여부 (스테이지 번호가 1부터 시작하므로 크기는 MaxStage + 1)
    private readonly bool[] isStageClear = new bool[MaxStage + 1];

    /// <summary>현재 선택된 스테이지 번호 (1 ~ MaxStage). 각 퍼즐 매니저가 참조한다.</summary>
    public int stage = 1;

    private int curSceneNum = MainSceneIndex;

    [FormerlySerializedAs("debugerText")]
    [SerializeField] private TMP_Text debugText;
    [FormerlySerializedAs("selectPanel_stage")]
    [SerializeField] private GameObject stageSelectPanel;
    [FormerlySerializedAs("selectPanel_puzzle")]
    [SerializeField] private GameObject puzzleSelectPanel;
    [SerializeField] private GameObject clearPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region 디버그 로그 (심사용: 퍼즐 내부 데이터 실시간 출력)

    private const int MaxLogLines = 12;
    private int logLineCount;

    /// <summary>화면 내 디버그 텍스트 창에 한 줄을 출력한다. 줄 수가 차면 비우고 다시 쓴다.</summary>
    public void WriteLog(string context)
    {
        if (debugText == null) return;

        if (logLineCount >= MaxLogLines)
        {
            debugText.text = "";
            logLineCount = 0;
        }

        logLineCount++;
        debugText.text += "\n" + context;
    }

    /// <summary>심사용 치트: 현재 퍼즐을 강제 클리어 처리한다. (디버그 UI 버튼에 연결)</summary>
    [SerializeField] private GameObject skipButton;
    public void skip()
    {
        WriteLog($"stage{stage} {sceneNames[curSceneNum]} | skip");

        SoundManager.Instance.soundEffect(0);

        if (curSceneNum != 3)
            Clear();
    }

    #endregion

    #region 게임 흐름 제어

    /// <summary>스테이지 선택 버튼에서 호출. 퍼즐 클리어 상태를 초기화하고 퍼즐 선택 화면으로 넘어간다.</summary>
    public void SelectStage(int newStage)
    {
        if (newStage < 1 || newStage > MaxStage)
        {
            WriteLog($"[Error] Invalid stage: {newStage}");
            return;
        }

        stage = newStage;

        for (int i = 0; i < isPuzzleClear.Length; i++)
        {
            isPuzzleClear[i] = false;
        }

        stageSelectPanel.SetActive(false);
        puzzleSelectPanel.SetActive(true);

        SoundManager.Instance.soundEffect(0);

    }

    /// <summary>퍼즐 선택 버튼에서 호출. 이미 클리어한 퍼즐이면 무시한다.</summary>
    public void GoPuzzle(int puzzle)
    {
        if (puzzle < 0 || puzzle >= PuzzleCount) return;
        if (isPuzzleClear[puzzle]) return;

        RequestScene(puzzle);

        SoundManager.Instance.soundEffect(0);
    }

    /// <summary>씬 전환. 메인 씬으로 돌아올 때는 퍼즐 선택 패널을 다시 띄운다.</summary>
    public void RequestScene(int newSceneNum)
    {
        if (newSceneNum < 0 || newSceneNum >= sceneNames.Length) return;

        clearPanel.SetActive(false);
        
        if (!isStageClear[stage]) puzzleSelectPanel.SetActive(newSceneNum == MainSceneIndex);
        else stageSelectPanel.SetActive(newSceneNum == MainSceneIndex);

        skipButton.SetActive(newSceneNum == 3 ? false : true);

        curSceneNum = newSceneNum;
        SceneManager.LoadScene(sceneNames[newSceneNum], LoadSceneMode.Single);
    }

    /// <summary>퍼즐 매니저가 클리어 조건을 만족했을 때 호출. 세 퍼즐을 모두 풀면 스테이지 클리어.</summary>
    public void Clear()
    {
        // 메인 씬 등 퍼즐 씬이 아닌 곳에서 호출되면 무시 (배열 범위 방어)
        if (curSceneNum < 0 || curSceneNum >= PuzzleCount) return;

        isPuzzleClear[curSceneNum] = true;
        clearPanel.SetActive(true);

        if (isPuzzleClear[0] && isPuzzleClear[1] && isPuzzleClear[2])
        {
            isStageClear[stage] = true;
            WriteLog($"Stage {stage} All Clear!");
        }

        SoundManager.Instance.soundEffect(1);
    }

    #endregion
}

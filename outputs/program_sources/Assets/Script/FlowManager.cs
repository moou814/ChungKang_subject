using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class FlowManager : MonoBehaviour
{
    private const int PuzzleCount = 3;
    private const int MainSceneIndex = 3;
    private const int MaxDebugLines = 12;

    public static FlowManager Instance { get; private set; }

    private readonly bool[] isPuzzleClear = new bool[PuzzleCount];

    // Stage buttons pass 1, 2, 3, so index 0 is intentionally unused.
    private readonly bool[] isStageClear = new bool[PuzzleCount + 1];

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

    [SerializeField, FormerlySerializedAs("debugerText")] private TMP_Text debuggerText;
    private int debugLineCount;

    public void WriteLog(string context)
    {
        if (debuggerText == null)
        {
            Debug.Log(context);
            return;
        }

        if (debugLineCount >= MaxDebugLines)
        {
            debuggerText.text = "";
            debugLineCount = 0;
        }

        debugLineCount++;
        debuggerText.text += "\n" + context;
    }

    public void GoPuzzle(int puzzle)
    {
        if (!IsValidPuzzleIndex(puzzle))
        {
            WriteLog($"Invalid puzzle index: {puzzle}");
            return;
        }

        if (isPuzzleClear[puzzle]) return;

        RequestScene(puzzle);
    }

    [SerializeField, FormerlySerializedAs("selectPanel_stage")] private GameObject selectPanelStage;

    public void SelectStage(int selectedStage)
    {
        stage = Mathf.Clamp(selectedStage, 1, PuzzleCount);
        Array.Clear(isPuzzleClear, 0, isPuzzleClear.Length);

        SetActive(selectPanelStage, false);
        SetActive(selectPanelPuzzle, true);
        WriteLog($"Stage selected: {stage}");
    }

    // Kept for existing Unity Button events in MainScene.
    public void selectStage(int selectedStage)
    {
        SelectStage(selectedStage);
    }

    private int currentSceneIndex = MainSceneIndex;
    public int stage;

    private static readonly string[] SceneNames = new string[]
    {
        "PipeScene",
        "BitMaskScene",
        "MirrorScene",
        "MainScene"
    };

    public void RequestScene(int newSceneNum)
    {
        if (newSceneNum < 0 || newSceneNum >= SceneNames.Length)
        {
            WriteLog($"Invalid scene index: {newSceneNum}");
            return;
        }

        SetActive(clearPanel, false);
        SetActive(selectPanelPuzzle, newSceneNum == MainSceneIndex);

        currentSceneIndex = newSceneNum;

        SceneManager.LoadScene(SceneNames[newSceneNum], LoadSceneMode.Single);
    }

    [SerializeField] private GameObject clearPanel;
    [SerializeField, FormerlySerializedAs("selectPanel_puzzle")] private GameObject selectPanelPuzzle;

    public void Clear()
    {
        if (!IsValidPuzzleIndex(currentSceneIndex))
        {
            WriteLog($"Clear ignored. Current scene index is not a puzzle: {currentSceneIndex}");
            return;
        }

        isPuzzleClear[currentSceneIndex] = true;
        SetActive(clearPanel, true);

        if (isPuzzleClear[0] && isPuzzleClear[1] && isPuzzleClear[2])
        {
            isStageClear[stage] = true;
            WriteLog($"Stage {stage} Clear");
        }
    }

    private static bool IsValidPuzzleIndex(int index)
    {
        return index >= 0 && index < PuzzleCount;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}

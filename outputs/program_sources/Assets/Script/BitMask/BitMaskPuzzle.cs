using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum switchType
{
    XOR,
    OR,
    AND
}

public class BitMaskPuzzle : MonoBehaviour
{
    private const int LampCount = 5;

    public GameObject switchPrefab;
    public GameObject lightPrefab;

    [SerializeField, FormerlySerializedAs("puzzleB")] private Transform puzzleBoard;
    [SerializeField] private bitMaskData[] stageData;

    private uint currentState;
    private uint[] switchMasks = Array.Empty<uint>();
    private readonly Image[] lights = new Image[LampCount];

    public int stage;
    public bool isClear;
    public static BitMaskPuzzle Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetUp();
    }

    private void SetUp()
    {
        stage = FlowManager.Instance.stage;
        if (!TryGetStageData(out bitMaskData currentStageData))
        {
            return;
        }

        int switchCount = currentStageData.switchInfo.Length / LampCount;
        switchMasks = new uint[switchCount];

        for (int switchIndex = 0; switchIndex < switchCount; switchIndex++)
        {
            GameObject switchObject = Instantiate(switchPrefab, puzzleBoard);
            switchObject.transform.position = new Vector3((switchIndex - (switchCount / 2f) - 0.3f) * 2.4f, -2);

            BitMaskPuzzle_switch switchButton = switchObject.GetComponent<BitMaskPuzzle_switch>();
            switchButton.switchNum = switchIndex;
            switchButton.type = currentStageData.switchType[switchIndex];

            for (int lampIndex = 0; lampIndex < LampCount; lampIndex++)
            {
                if (currentStageData.switchInfo[lampIndex + switchIndex * LampCount])
                {
                    switchMasks[switchIndex] |= 1u << lampIndex;
                }
            }
        }

        for (int lampIndex = 0; lampIndex < LampCount; lampIndex++)
        {
            GameObject lampObject = Instantiate(lightPrefab, puzzleBoard);
            lampObject.transform.position = new Vector3((lampIndex - (LampCount / 2f) + 0.5f) * 2.4f, 2);
            lights[lampIndex] = lampObject.GetComponent<Image>();
        }
    }

    private bool TryGetStageData(out bitMaskData data)
    {
        data = null;
        if (stageData == null || stage < 0 || stage >= stageData.Length || stageData[stage] == null)
        {
            FlowManager.Instance.WriteLog($"BitMask stage data missing: {stage}");
            return false;
        }

        data = stageData[stage];
        return true;
    }

    public void InteractSwitch(int index, switchType type)
    {
        if (index < 0 || index >= switchMasks.Length)
        {
            FlowManager.Instance.WriteLog($"Invalid switch index: {index}");
            return;
        }

        uint mask = switchMasks[index];
        switch (type)
        {
            case switchType.XOR:
                currentState ^= mask;
                FlowManager.Instance.WriteLog($"switch | XOR | {Convert.ToString(mask, 2).PadLeft(LampCount, '0')}");
                break;

            case switchType.OR:
                currentState |= mask;
                FlowManager.Instance.WriteLog($"switch | OR | {Convert.ToString(mask, 2).PadLeft(LampCount, '0')}");
                break;

            case switchType.AND:
                currentState &= mask;
                FlowManager.Instance.WriteLog($"switch | AND | {Convert.ToString(mask, 2).PadLeft(LampCount, '0')}");
                break;
        }

        UpdateLamps();
    }

    // Kept for older references.
    public void interSwitch(int index, switchType type)
    {
        InteractSwitch(index, type);
    }

    private void UpdateLamps()
    {
        bool cleared = true;
        for (int lampIndex = 0; lampIndex < lights.Length; lampIndex++)
        {
            bool isOn = (currentState & (1u << lampIndex)) != 0;
            lights[lampIndex].color = isOn ? Color.yellow : Color.black;

            if (!isOn)
            {
                cleared = false;
            }
        }

        if (cleared && !isClear)
        {
            isClear = true;
            FlowManager.Instance.WriteLog("Clear");
            FlowManager.Instance.Clear();
        }
    }
}

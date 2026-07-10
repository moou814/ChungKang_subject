using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum SwitchType
{
    XOR,
    OR,
    AND
}

/// <summary>
/// 비트마스크 퍼즐 매니저.
/// 램프 5개의 점등 상태를 uint의 하위 5비트로 표현하고,
/// 각 스위치가 가진 비트마스크를 XOR / OR / AND 연산으로 적용해
/// 모든 비트가 1(모든 램프 점등)이 되면 클리어로 판정한다.
/// </summary>
public class BitMaskPuzzle : MonoBehaviour
{
    public static BitMaskPuzzle Instance { get; private set; }

    private const int LightCount = 5;                          // 램프(비트) 개수
    private const uint ClearMask = (1u << LightCount) - 1;     // 0b11111 = 모든 램프 점등

    public GameObject switchPrefab;
    public GameObject lightPrefab;

    [FormerlySerializedAs("puzzleB")]
    [SerializeField] private Transform puzzleRoot;
    [SerializeField] private bitMaskData[] stageData;

    public int stage;

    private uint curState;                                     // 현재 램프 상태 (비트마스크)
    private uint[] switchMasks;                                // 스위치별 조작 비트마스크
    private readonly Image[] lights = new Image[LightCount];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildStage();
    }

    private void BuildStage()
    {
        stage = FlowManager.Instance.stage;

        if (stage < 0 || stage >= stageData.Length || stageData[stage] == null)
        {
            FlowManager.Instance.WriteLog($"[Error] No BitMask stage data for stage {stage}");
            return;
        }

        bitMaskData data = stageData[stage];
        int switchCount = data.switchInfo.Length / LightCount;

        switchMasks = new uint[switchCount];

        for (int j = 0; j < switchCount; j++)
        {
            GameObject switchObj = Instantiate(switchPrefab, puzzleRoot);
            switchObj.transform.position = new Vector3((j - switchCount / 2 - 0.8f) * 2.4f, -2);

            BitMaskSwitch bitSwitch = switchObj.GetComponent<BitMaskSwitch>();
            bitSwitch.switchNum = j;
            bitSwitch.type = data.switchType[j];

            // switchInfo는 스위치당 LightCount개의 bool을 이어 붙인 배열.
            // true인 자리의 비트를 세워 스위치의 비트마스크를 만든다.
            for (int i = 0; i < LightCount; i++)
            {
                if (data.switchInfo[i + j * LightCount])
                {
                    switchMasks[j] |= 1u << i;
                }
            }
        }

        for (int i = 0; i < LightCount; i++)
        {
            GameObject lightObj = Instantiate(lightPrefab, puzzleRoot);
            lightObj.transform.position = new Vector3((i - switchCount / 2 - 0.5f) * 2.4f, 2);
            lights[i] = lightObj.GetComponent<Image>();
        }
    }

    /// <summary>스위치를 눌렀을 때 해당 스위치의 마스크를 현재 상태에 비트 연산으로 적용한다.</summary>
    public void ApplySwitch(int idx, SwitchType type)
    {
        if (switchMasks == null || idx < 0 || idx >= switchMasks.Length) return;

        string maskBits = Convert.ToString(switchMasks[idx], 2).PadLeft(LightCount, '0');

        switch (type)
        {
            case SwitchType.XOR:
                curState ^= switchMasks[idx];
                break;

            case SwitchType.OR:
                curState |= switchMasks[idx];
                break;

            case SwitchType.AND:
                curState &= switchMasks[idx];
                break;
        }

        FlowManager.Instance.WriteLog(
            $"switch | {type} | mask: {maskBits} | state: {Convert.ToString(curState, 2).PadLeft(LightCount, '0')}");

        UpdateLights();
    }

    /// <summary>현재 상태의 각 비트를 램프 색으로 반영하고, 모든 비트가 켜졌으면 클리어 처리한다.</summary>
    private void UpdateLights()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            bool isOn = (curState & (1u << i)) != 0;
            lights[i].color = isOn ? Color.yellow : Color.black;
        }

        if (curState == ClearMask)
        {
            FlowManager.Instance.WriteLog("Clear");
            FlowManager.Instance.Clear();
        }
    }
}

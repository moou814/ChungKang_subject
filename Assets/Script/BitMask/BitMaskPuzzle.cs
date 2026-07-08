using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI.Table;

public enum switchType
{
    XOR,
    OR,
    AND
};

public class BitMaskPuzzle : MonoBehaviour
{
    public GameObject switchPrefab;
    public GameObject lightPrefab;

    [SerializeField] private Transform puzzleB;

    uint curState;
    uint[] switchs = new uint[4];

    Image[] lights = new Image[5];

    [SerializeField] private bitMaskData[] stageData;
    public int stage;
    public bool isClear = false;
    public static BitMaskPuzzle Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        setUp();
    }

    void setUp()
    {
        stage = FlowManager.Instance.stage;

        // stage make
        for (int j = 0; j < (int)(stageData[stage].switchInfo.Length / 5); j++) {
            GameObject s = Instantiate(switchPrefab, puzzleB);

            s.transform.position = new Vector3((j - (stageData[stage].switchInfo.Length / 5 / 2) - 0.8f) * 2.4f, -2);
            s.GetComponent<BitMaskPuzzle_switch>().switchNum = j;
            s.GetComponent<BitMaskPuzzle_switch>().type = stageData[stage].switchType[j];

            switchs[j] = 0;

            for (int i = 0; i < 5; i++)
            {
                if (stageData[stage].switchInfo[i + j * 5]) {
                    switchs[j] |= (1u << i); }
            }
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject l = Instantiate(lightPrefab, puzzleB);

            l.transform.position = new Vector3((i - (stageData[stage].switchInfo.Length / 5 / 2) - 0.5f) * 2.4f, 2);
            lights[i] = l.GetComponent<Image>();
        }

    }

    public void interSwitch(int idx, switchType tp)
    {
        switch (tp)
        {
            case switchType.XOR: 
                curState ^= switchs[idx];
                FlowManager.Instance.WriteLog($"swich | XOR | {Convert.ToString(switchs[idx], 2).PadLeft(4, '0')}");
                break;

            case switchType.OR:
                curState |= switchs[idx]; 
                FlowManager.Instance.WriteLog($"swich | OR | {Convert.ToString(switchs[idx], 2).PadLeft(4, '0')}");
                break;

            case switchType.AND: 
                curState &= switchs[idx]; 
                FlowManager.Instance.WriteLog($"swich | AND | {Convert.ToString(switchs[idx], 2).PadLeft(4, '0')}");
                break;
        }

        lampUpdate();
    }

    bool on;
    void lampUpdate()
    {
        bool f = true;

        for (int i = 0; i < lights.Length; i++)
        {
            on = (curState & (1 << i)) != 0;

            lights[i].color = on ? Color.black : Color.yellow;
            if (on) f = false;
        }

        if (f)
        {
            FlowManager.Instance.WriteLog("Clear");
            FlowManager.Instance.Clear();
        }
    }

}

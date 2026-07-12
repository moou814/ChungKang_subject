using UnityEngine;

/// <summary>비트마스크 퍼즐의 스위치 하나. 버튼 클릭 시 매니저에게 자신의 번호와 연산 종류를 전달한다.</summary>
public class BitMaskSwitch : MonoBehaviour
{
    public int switchNum;
    public SwitchType type;

    /// <summary>UI Button onClick에 연결된다.</summary>
    public void Press()
    {
        SoundManager.Instance.soundEffect(0);

        BitMaskPuzzle.Instance.ApplySwitch(switchNum, type);
    }
}

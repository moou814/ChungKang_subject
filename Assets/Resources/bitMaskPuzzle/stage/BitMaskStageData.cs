using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 비트마스크 퍼즐 스테이지 데이터.
/// switchInfo는 스위치당 5개(램프 수)의 bool을 이어 붙인 1차원 배열로,
/// true인 자리가 해당 스위치가 조작하는 비트를 의미한다.
/// </summary>
[CreateAssetMenu(fileName = "BitMaskStageData", menuName = "Scriptable Objects/BitMaskStageData")]
public class BitMaskStageData : ScriptableObject
{
    public int stage;

    [FormerlySerializedAs("switchType")]
    public SwitchType[] switchTypes;   // 스위치별 연산 종류 (XOR / OR / AND)
    public bool[] switchInfo;          // 스위치별 조작 비트 (스위치 수 x 5)
}

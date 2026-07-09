using UnityEngine;

/// <summary>
/// 파이프 퍼즐 스테이지 데이터.
/// map은 mapSize[0](행) x mapSize[1](열) 그리드를 1차원으로 펼친 배열이다.
/// start / end / fixedBlocks는 (행, 열) 쌍을 이어 붙인 배열이다.
/// </summary>
[CreateAssetMenu(fileName = "PipeStageData", menuName = "Scriptable Objects/PipeStageData")]
public class PipeStageData : ScriptableObject
{
    public int stage;
    public BlockKind[] map;
    public int[] mapSize;      // [0] = 행(세로), [1] = 열(가로)
    public int[] start;        // 시작 블록 (행, 열)
    public int[] end;          // 도착 블록들 (행, 열) 쌍의 나열
    public int[] fixedBlocks;  // 회전 불가 블록들 (행, 열) 쌍의 나열
}

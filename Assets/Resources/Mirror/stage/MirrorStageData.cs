using UnityEngine;

/// <summary>
/// 거울 퍼즐 스테이지 데이터.
/// map은 mapSize[0](가로) x mapSize[1](세로) 그리드를 1차원으로 펼친 배열이다.
/// </summary>
[CreateAssetMenu(fileName = "MirrorStageData", menuName = "Scriptable Objects/MirrorStageData")]
public class MirrorStageData : ScriptableObject
{
    public int stage;
    public HitKind[] map;          // 셀별 오브젝트 종류 (None/Mirror/Target/Boundary/Prism)
    public BeamColor[] targets;    // 타겟들의 요구 색상 (맵에 등장하는 순서대로)
    public int[] mapSize;          // [0] = 가로, [1] = 세로
    public Vector3Int rayStartPoint;
    public Vector2 rayStartDir;
}

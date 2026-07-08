using UnityEngine;
using static Unity.Collections.AllocatorManager;

[CreateAssetMenu(fileName = "MirrorStageData", menuName = "Scriptable Objects/MirrorStageData")]

public class MirrorStageData : ScriptableObject
{
    public int stage;
    public HitKind[] map;
    public int[] mapSize;
    public Vector3Int rayStartPoint;
    public Vector2 rayStartDir;
}

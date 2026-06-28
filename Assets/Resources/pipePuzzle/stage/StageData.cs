using UnityEngine;
using static Unity.Collections.AllocatorManager;

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]

public class StageData : ScriptableObject
{
    public int stage;
    public blockKind[] map;
    public int[] mapSize; 
    public int[] start;
    public int[] end;
    public int[] fixedBlocks;
}

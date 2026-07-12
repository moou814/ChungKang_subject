using UnityEngine;

[CreateAssetMenu(fileName = "PipeStageData", menuName = "Scriptable Objects/StageData")]
public class PipeStageData : ScriptableObject
{
    public int stage;
    public BlockKind[] map;
    public int[] mapSize; 
    public int[] start;
    public int[] end;
    public int[] fixedBlocks;
}

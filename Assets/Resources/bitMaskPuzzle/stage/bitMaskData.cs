using UnityEngine;

[CreateAssetMenu(fileName = "bitMaskData", menuName = "Scriptable Objects/bitMaskData")]
public class bitMaskData : ScriptableObject
{
    public int stage;
    public SwitchType[] switchType;
    public bool[] switchInfo;
}

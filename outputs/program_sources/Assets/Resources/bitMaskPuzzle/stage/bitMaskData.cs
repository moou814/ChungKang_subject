using UnityEngine;

[CreateAssetMenu(fileName = "bitMaskData", menuName = "Scriptable Objects/bitMaskData")]
public class bitMaskData : ScriptableObject
{
    public int stage;
    public switchType[] switchType;
    public bool[] switchInfo;
}

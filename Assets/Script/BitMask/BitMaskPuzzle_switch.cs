using UnityEngine;

public class BitMaskPuzzle_switch : MonoBehaviour
{
    public int switchNum;
    public switchType type;

    public void onoff()
    {
        BitMaskPuzzle.Instance.interSwitch(switchNum, type);
    }
}

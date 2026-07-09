using UnityEngine;

public class BitMaskPuzzle_switch : MonoBehaviour
{
    public int switchNum;
    public switchType type;

    public void OnOff()
    {
        BitMaskPuzzle.Instance.InteractSwitch(switchNum, type);
    }

    // Kept for the existing Button event on Switch.prefab.
    public void onoff()
    {
        OnOff();
    }
}

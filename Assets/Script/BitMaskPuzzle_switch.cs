using UnityEngine;

public class BitMaskPuzzle_switch : MonoBehaviour
{
    public int switchNum;
    [SerializeField] private int type;
    
    public void onoff()
    {
        BitMaskPuzzle.Instance.interSwitch(switchNum, type);
    }
}

using UnityEngine;

public class BitMaskPuzzle_switch : MonoBehaviour
{
    [SerializeField]
    private int switchNum;
    
    void onoff()
    {
        BitMaskPuzzle.Instance.interSwitch(switchNum);
    }
}

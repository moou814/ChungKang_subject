using UnityEngine;

public class BitMaskPuzzle : MonoBehaviour
{
    int curState;
    int[] switchs;

    
    void Start()
    {
        
    }

    public void interSwitch(int idx)
    {
        curState ^= switchs[idx];

        lampUpdate();
    }

    void lampUpdate()
    {
        for (int i = 0; i < lightCount; i++)
        {
            bool on = (curState & (1 << i)) != 0;

            lights[i].SetActive(on);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

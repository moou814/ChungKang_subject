using UnityEngine;
using UnityEngine.UI;

public class BitMaskPuzzle : MonoBehaviour
{
    public GameObject switchPrefub;
    public GameObject lightPrefub;

    int curState;
    int[] switchs;

    Image [] lights;
    public static BitMaskPuzzle Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // stage make
        
    }

    public void interSwitch(int idx, int tp)
    {
        switch (tp)
        {
            case 0: // xor
                curState ^= switchs[idx]; break;

            case 1: // or
                curState |= switchs[idx]; break;

            case 2: // and
                curState &= switchs[idx]; break;
        }

        lampUpdate();
    }

    void lampUpdate()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            bool on = (curState & (1 << i)) != 0;

            lights[i].color = on ? Color.red : Color.green;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;

public class BitMaskPuzzle : MonoBehaviour
{
    int curState;
    int[] switchs;

    GameObject[] lights;
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

    public void interSwitch(int idx)
    {
        curState ^= switchs[idx];

        lampUpdate();
    }

    void lampUpdate()
    {
        for (int i = 0; i < lights.Length; i++)
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

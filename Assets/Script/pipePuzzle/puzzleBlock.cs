using UnityEngine;
using UnityEngine.UI;

public class pipePuzzle_Block : MonoBehaviour
{
    public int angle;
    public blockKind kind; // 0:¤¤, 1:-, 2:+, 3:¤¿

    private Image img;

    public bool itFixed;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void turn()
    {
        if (itFixed) return;

        angle = (angle + 1) % 4;
        transform.Rotate(new Vector3(0, 0, -90));

        pipePuzzle_Manager.Instance.clearCheck();
    }

    public void onoffRoad(bool isOn)
    {
        img.color = isOn ? Color.white : Color.gray;
    }
    int[] r;
    public int[] GetCanGo()
    {
        r = new int[] { -1, -1, -1, -1 };

        for (int i = 0; i < 4; i++)
        {
            if (pipePuzzle_Manager.connetedDir[(int)kind, i] != -1)
            {
                r[i] = (pipePuzzle_Manager.connetedDir[(int)kind, i] + angle) % 4;
            }
        }
        return r;
    }
}

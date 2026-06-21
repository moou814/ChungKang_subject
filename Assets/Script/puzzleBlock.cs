using UnityEngine;
using UnityEngine.UI;

public class elevatorPuzzlePiece : MonoBehaviour
{
    public elevatorPuzzleManager pManager;

    public int angle;
    public int kind; // 0:¤¤, 1:-, 2:+, 3:¤¿

    public void turn()
    {
        angle = (angle + 1) % 4;
        transform.Rotate(new Vector3(0, 0, -90));

        pManager.IsClear();
    }

    public void onoffRoad(bool isOn)
    {
        GetComponent<Image>().color = isOn ? Color.white : Color.gray;
    }

    public int[] GetCanGo()
    {
        int[] r = new int[] { -1, -1, -1,-1 };

        for (int i = 0; i < 4; i++)
        {
            if (elevatorPuzzleManager.connetedDir[kind, i] != -1)
            {
                r[i] = (elevatorPuzzleManager.connetedDir[kind, i] + angle) % 4;
            }
        }
        return r;
    }
}

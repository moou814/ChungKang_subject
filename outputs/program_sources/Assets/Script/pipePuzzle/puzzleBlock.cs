using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class pipePuzzle_Block : MonoBehaviour
{
    public int angle;
    public blockKind kind;

    [FormerlySerializedAs("itFixed")] public bool IsFixed;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Turn()
    {
        if (IsFixed)
        {
            return;
        }

        angle = (angle + 1) % 4;
        transform.Rotate(new Vector3(0, 0, -90));

        pipePuzzle_Manager.Instance.ClearCheck();
    }

    // Kept for the existing Button event on Image.prefab.
    public void turn()
    {
        Turn();
    }

    public void SetRoadState(bool isConnected)
    {
        image.color = isConnected ? Color.white : Color.gray;
    }

    // Kept for older code references.
    public void onoffRoad(bool isOn)
    {
        SetRoadState(isOn);
    }

    public int[] GetCanGo()
    {
        int[] result = new int[] { -1, -1, -1, -1 };

        for (int i = 0; i < result.Length; i++)
        {
            int baseDirection = pipePuzzle_Manager.ConnectedDirections[(int)kind, i];
            if (baseDirection != -1)
            {
                result[i] = (baseDirection + angle) % 4;
            }
        }

        return result;
    }
}

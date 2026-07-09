using UnityEngine;
using UnityEngine.UI;

/// <summary>거울 퍼즐의 타겟. 광선이 도달하면 isClear가 켜진다.</summary>
public class Target : MonoBehaviour
{
    public BeamColor targetColor;
    public bool isClear;

    private void Awake()
    {
        Color color;

        switch (targetColor)
        {
            case BeamColor.Red: color = Color.red; break;
            case BeamColor.Green: color = Color.green; break;
            case BeamColor.Blue: color = Color.blue; break;
            default: color = Color.greenYellow; break; // White 타겟 표시색
        }

        if (TryGetComponent(out Image image))
        {
            image.color = color;
        }
    }
}

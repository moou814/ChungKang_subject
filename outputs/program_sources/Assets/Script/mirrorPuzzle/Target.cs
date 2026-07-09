using UnityEngine;
using UnityEngine.UI;

public class Target : MonoBehaviour
{
    public BeamColor targetColor;
    public bool isClear;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        ApplyColor();
    }

    public void Init(BeamColor color)
    {
        targetColor = color;
        isClear = false;

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        ApplyColor();
    }

    public void ResetClear()
    {
        isClear = false;
    }

    public bool TryClear(BeamColor incomingColor)
    {
        if (targetColor != BeamColor.White && incomingColor != targetColor)
        {
            return false;
        }

        isClear = true;
        return true;
    }

    private void ApplyColor()
    {
        if (image == null)
        {
            return;
        }

        image.color = targetColor switch
        {
            BeamColor.Red => Color.red,
            BeamColor.Green => Color.green,
            BeamColor.Blue => Color.blue,
            _ => Color.white
        };
    }
}

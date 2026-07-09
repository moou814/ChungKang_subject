using UnityEngine;
using UnityEngine.UI;

public class Target : MonoBehaviour
{
    public BeamColor targetColor;

    public bool isClear;

    private void Awake()
    {
        Color clo = Color.white;

        switch (targetColor)
        {
            case BeamColor.White: clo = Color.greenYellow; break;
            case BeamColor.Red: clo = Color.red; break;
            case BeamColor.Green: clo = Color.green; break;
            case BeamColor.Blue: clo = Color.blue; break;
        }

        GetComponent<Image>().color = clo;
    }
}

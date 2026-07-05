using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;

public class Mirror
{
    private readonly string label;
    private readonly float length;
    private readonly Color baseColor;
    private readonly LineRenderer surfaceLine;
    private readonly TextMesh caption;
    private readonly MirrorManager game;

    public Mirror(string id, string label, Vector2Int cell, float surfaceAngleDegrees, float length, Color baseColor, LineRenderer surfaceLine, TextMesh caption, MirrorManager game)
    {
        Id = id;
        this.label = label;
        Cell = cell;
        SurfaceAngleDegrees = NormalizeAngle(surfaceAngleDegrees);
        this.length = length;
        this.baseColor = baseColor;
        this.surfaceLine = surfaceLine;
        this.caption = caption;
        this.game = game;
    }

    public string Id { get; }
    public Vector2Int Cell { get; private set; }
    public float SurfaceAngleDegrees { get; private set; }
    public Vector2 Center => game.CellToWorld(Cell);
    public Vector2 SurfaceDirection => MirrorManager.RotateVector(Vector2.right, SurfaceAngleDegrees).normalized;
    public Vector2 Normal => MirrorManager.RotateVector(Vector2.up, SurfaceAngleDegrees).normalized;
    public Vector2 EndpointA => Center - SurfaceDirection * (length * 0.5f);
    public Vector2 EndpointB => Center + SurfaceDirection * (length * 0.5f);

    /// <summary>
    /// 거울 표면 각도를 설정합니다.
    /// 0~360도 범위로 정규화해서 표시와 계산이 안정적으로 유지되게 합니다.
    /// </summary>
    public void SetSurfaceAngle(float degrees)
    {
        SurfaceAngleDegrees = NormalizeAngle(degrees);
        Refresh(false);
    }

    /// <summary>
    /// 90도 단위 회전이 필요할 때 사용할 수 있는 보조 함수입니다.
    /// 현재 주 조작은 Q/W 연속 회전이지만, 테스트나 확장용으로 남겨둡니다.
    /// </summary>
    public void RotateClockwise()
    {
        SetSurfaceAngle(SurfaceAngleDegrees + 90f);
    }

    /// <summary>
    /// deltaDegrees만큼 연속 회전합니다.
    /// Q/W 입력은 매 프레임 `회전 방향 * 회전 속도 * Time.deltaTime`을 이 함수로 전달합니다.
    /// </summary>
    public void RotateBy(float deltaDegrees)
    {
        SetSurfaceAngle(SurfaceAngleDegrees + deltaDegrees);
    }

    /// <summary>
    /// 마우스 선택 판정을 위해, 임의의 점과 거울 선분 사이의 거리를 반환합니다.
    /// </summary>
    public float DistanceToSurface(Vector2 point)
    {
        return MirrorManager.DistancePointToSegment(point, EndpointA, EndpointB);
    }

    /// <summary>
    /// 거울의 선분 위치, 선택 색상, 라벨 텍스트를 현재 데이터에 맞게 다시 그립니다.
    /// </summary>
    public void Refresh(bool selected)
    {
        if (surfaceLine != null)
        {
            surfaceLine.SetPosition(0, new Vector3(EndpointA.x, EndpointA.y, -0.05f));
            surfaceLine.SetPosition(1, new Vector3(EndpointB.x, EndpointB.y, -0.05f));
            surfaceLine.startWidth = selected ? 0.18f : 0.13f;
            surfaceLine.endWidth = selected ? 0.18f : 0.13f;

            Color color = selected ? Color.white : baseColor;
            surfaceLine.startColor = color;
            surfaceLine.endColor = color;
        }

        if (caption != null)
        {
            caption.text = label + "\n(" + Cell.x + "," + Cell.y + ")" + ("\n" + Mathf.RoundToInt(SurfaceAngleDegrees) + " deg");
            caption.transform.position = new Vector3((Center + new Vector2(0f, -0.5f)).x, (Center + new Vector2(0f, -0.5f)).y, -0.2f);
            caption.color = selected ? Color.white : baseColor;
        }
    }

    /// <summary>
    /// 각도를 0 이상 360 미만으로 정리합니다.
    /// 예: 370도는 10도, -30도는 330도가 됩니다.
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
        {
            angle += 360f;
        }

        return angle;
    }
}

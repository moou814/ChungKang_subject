using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 거울 퍼즐의 거울 하나. 마우스를 가까이 대고 좌/우 클릭을 누르고 있으면 회전한다.
/// 광선이 부딪히면 입사 벡터와 법선 벡터로 반사 방향을 계산해 준다.
/// </summary>
public class Mirror : MonoBehaviour
{
    private const float InteractRadius = 1.5f;   // 마우스로 조작 가능한 거리

    [SerializeField] private float rotateSpeed;  // 초당 회전 각도 (deg/s)

    private float angle;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        angle = transform.eulerAngles.z;
    }

    private void Update()
    {
        if (mainCam == null || Mouse.current == null) return;

        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (Vector2.Distance(mouseWorld, transform.position) > InteractRadius) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Turn(clockwise: false);
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Turn(clockwise: true);
        }
    }

    /// <summary>델타타임 기반 회전(프레임 속도와 무관)한 뒤, 광선 경로를 다시 계산하게 한다.</summary>
    private void Turn(bool clockwise)
    {
        float delta = rotateSpeed * Time.deltaTime;
        angle = Mathf.Repeat(angle + (clockwise ? delta : -delta), 360f);

        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 회전 직후의 콜라이더 위치로 즉시 레이캐스트할 수 있도록 물리 트랜스폼을 동기화
        Physics2D.SyncTransforms();

        MirrorManager.Instance.DrawRay();
    }

    /// <summary>
    /// 반사 벡터 공식 r = d - 2(d·n)n 을 그대로 구현한다.
    /// d: 입사 방향(단위 벡터), n: 거울의 법선(transform.up).
    /// </summary>
    public Vector2 CalculateReflection(Vector2 incidentDir)
    {
        Vector2 d = incidentDir.normalized;
        Vector2 n = transform.up.normalized;

        float dot = d.x * n.x + d.y * n.y;   // 내적 d·n
        Vector2 reflected = d - 2f * dot * n;

        return reflected.normalized;
    }
}

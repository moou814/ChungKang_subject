using UnityEngine;
using UnityEngine.InputSystem;

public class Mirror : MonoBehaviour
{
    private float angle;
    [SerializeField] private float rotateSpeed;

    private void Update()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (Vector2.Distance(mouseWorld, (Vector2)transform.position) < 1.5f)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                RotateMirror(false);
            }

            if (Mouse.current.rightButton.isPressed)
            {
                RotateMirror(true);
            }
        }
    }

    private void RotateMirror(bool clockwise)
    {
        if (clockwise)
        {
            angle = (angle + rotateSpeed * Time.deltaTime) % 360f;
        }
        else
        {
            angle = (angle - rotateSpeed * Time.deltaTime);
            angle = angle < 0 ? 360f + angle : angle;
        }

        transform.rotation = Quaternion.Euler(0, 0, angle);

        Physics2D.SyncTransforms();

        MirrorManager.Instance.DrawRay();
    }

    public Vector2 CalculateReflectDirection(Vector2 direction)
    {
        Vector2 dir = direction.normalized;
        Vector2 normal = transform.up.normalized;

        float dot = dir.x * normal.x + dir.y * normal.y;

        Vector2 reflect = dir - 2f * dot * normal;

        return reflect.normalized;
    }

    // Kept for older code references.
    public Vector2 calculateReflexDgree(Vector2 direction)
    {
        return CalculateReflectDirection(direction);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;
using UnityEditor.Experimental.GraphView;

public class Mirror : MonoBehaviour
{
    private float angle;
    [SerializeField] float rotateSpeed;

    void Update()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint((Mouse.current.position.ReadValue()));

        if (Vector2.Distance(mouseWorld, (Vector2)transform.position) < 1.5f)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                Trun(false);
            }

            if (Mouse.current.rightButton.isPressed)
            {
                Trun(true);
            }
        }
    }

    private void Trun(bool clockwise)
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

    public Vector2 calculateReflexDgree(Vector2 direction)
    {
        Vector2 dir = direction.normalized;
        Vector2 normal = transform.up.normalized;

        float dot = dir.x * normal.x + dir.y * normal.y;

        Vector2 reflect = dir - 2f * dot * normal;

        return reflect.normalized;
    }
}

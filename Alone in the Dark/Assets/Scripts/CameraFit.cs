using UnityEngine;

/// <summary>
/// Keeps a minimum horizontal view width no matter the aspect ratio by pulling the camera back.
/// The play field is the z = 0 plane. The camera is a child of the player and originally sat
/// 10 world units behind it, which on a portrait phone shows only about 3 units to each side.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public float minHalfWidth = 6.5f;
    public float minDistance = 10f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float needed = minHalfWidth / (cam.aspect * Mathf.Tan(halfFov));
        float distance = Mathf.Max(minDistance, needed);

        var anchor = transform.parent != null ? transform.parent.position : Vector3.zero;
        transform.position = new Vector3(anchor.x, anchor.y, -distance);
    }
}

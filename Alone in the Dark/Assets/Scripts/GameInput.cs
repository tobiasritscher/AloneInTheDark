using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One place for all input. Keyboard/gamepad axes in the editor and on desktop,
/// drag-anywhere virtual stick on touch screens, mouse drag as a stand-in in the editor.
/// </summary>
public static class GameInput
{
    const float DeadZone = 0.08f;

    static Vector2 dragOrigin;
    static bool dragging;

    /// <summary>Steering vector, magnitude 0..1.</summary>
    public static Vector2 Move()
    {
        var keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keys.sqrMagnitude > 0.01f)
        {
            return Vector2.ClampMagnitude(keys, 1f);
        }

        bool down;
        Vector2 pointer;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            down = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            pointer = t.position;
            if (t.phase == TouchPhase.Began)
            {
                dragOrigin = pointer;
                dragging = true;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            down = true;
            pointer = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = pointer;
                dragging = true;
            }
        }
        else
        {
            dragging = false;
            return Vector2.zero;
        }

        if (!down)
        {
            dragging = false;
            return Vector2.zero;
        }

        if (!dragging)
        {
            // Finger was already down when play started (the tap that launched the level).
            dragOrigin = pointer;
            dragging = true;
        }

        float radius = Screen.height * 0.10f;
        var v = (pointer - dragOrigin) / radius;
        float mag = v.magnitude;
        if (mag < DeadZone)
        {
            return Vector2.zero;
        }

        if (mag > 1f)
        {
            // Floating stick: the origin follows the finger so reversing direction is immediate.
            v /= mag;
            dragOrigin = pointer - v * radius;
        }

        return v;
    }

    /// <summary>True on the frame a finger or the mouse goes down somewhere that is not a UI element.</summary>
    public static bool TapDown()
    {
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase != TouchPhase.Began)
            {
                return false;
            }

            return !OverUi(t.fingerId);
        }

        if (Input.GetMouseButtonDown(0))
        {
            return !OverUi(-1);
        }

        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    }

    /// <summary>Android back button, or Escape on desktop.</summary>
    public static bool Back()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    /// <summary>Call when leaving the playing state so a held finger does not carry over.</summary>
    public static void ResetDrag()
    {
        dragging = false;
    }

    static bool OverUi(int pointerId)
    {
        var es = EventSystem.current;
        if (es == null)
        {
            return false;
        }

        return es.IsPointerOverGameObject(pointerId) || es.IsPointerOverGameObject(-1);
    }
}

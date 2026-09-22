using UnityEngine;

/// <summary>Slowly spins a pickup so it reads as "collectable".</summary>
public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}

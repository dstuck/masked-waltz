using UnityEngine;

public sealed class DanceCircleRotator : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = 20f;

    private void Update()
    {
        // 2D project: rotate around Z axis.
        transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime, Space.Self);
    }
}


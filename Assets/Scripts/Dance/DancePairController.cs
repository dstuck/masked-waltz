using UnityEngine;

public sealed class DancePairController : MonoBehaviour
{
    private Quaternion _uprightWorldRotation;

    private void Awake()
    {
        _uprightWorldRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // Keep the pair upright (non-rotating) while orbiting via parent transform.
        transform.rotation = _uprightWorldRotation;
    }

    public void ApplyWorldSpaceDelta(Vector2 worldDelta)
    {
        transform.position += new Vector3(worldDelta.x, worldDelta.y, 0f);
    }
}


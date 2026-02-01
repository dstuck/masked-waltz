using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerMarker : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField] private Sprite _markerSprite;

    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _size = 0.25f;
    [SerializeField] private int _sortingOrderOffset = 10;

    [Header("Feedback")]
    [SerializeField] private float _flickerDurationSeconds = 0.35f;
    [SerializeField] private float _flickerHz = 18f;

    [Tooltip("Optional. If set, the marker copies sorting settings from this renderer.")]
    [SerializeField] private SpriteRenderer _targetRenderer;

    private SpriteRenderer _markerRenderer;
    private Coroutine _flickerRoutine;

    private void Reset()
    {
        _targetRenderer = GetComponentInParent<SpriteRenderer>();
    }

    private void Awake()
    {
        transform.localPosition = _localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(_size, _size, 1f);

        _markerRenderer = GetComponent<SpriteRenderer>();

        if (_markerSprite != null)
        {
            _markerRenderer.sprite = _markerSprite;
            _markerRenderer.color = Color.white;
        }

        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInParent<SpriteRenderer>();
        }

        if (_targetRenderer != null)
        {
            _markerRenderer.sortingLayerID = _targetRenderer.sortingLayerID;
            _markerRenderer.sortingOrder = _targetRenderer.sortingOrder + _sortingOrderOffset;
        }
    }

    public void Flicker()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (_markerRenderer == null)
        {
            _markerRenderer = GetComponent<SpriteRenderer>();
        }

        if (_markerRenderer == null)
        {
            return;
        }

        if (_flickerRoutine != null)
        {
            StopCoroutine(_flickerRoutine);
        }

        _flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        float duration = Mathf.Max(0.01f, _flickerDurationSeconds);
        float hz = Mathf.Max(1f, _flickerHz);

        bool initialEnabled = _markerRenderer.enabled;
        Color initialColor = _markerRenderer.color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            // Toggle roughly at the requested frequency.
            bool on = (Mathf.FloorToInt(t * hz) % 2) == 0;
            _markerRenderer.enabled = on;

            yield return null;
        }

        _markerRenderer.enabled = initialEnabled;
        _markerRenderer.color = initialColor;
        _flickerRoutine = null;
    }
}


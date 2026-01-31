using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerMarker : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField] private Sprite _markerSprite;

    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _size = 0.25f;
    [SerializeField] private int _sortingOrderOffset = 10;

    [Tooltip("Optional. If set, the marker copies sorting settings from this renderer.")]
    [SerializeField] private SpriteRenderer _targetRenderer;

    private SpriteRenderer _markerRenderer;

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
}


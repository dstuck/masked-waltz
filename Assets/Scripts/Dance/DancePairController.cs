using UnityEngine;

public sealed class DancePairController : MonoBehaviour
{
    [Header("Dancers (optional; auto-found if not set)")]
    [SerializeField] private SpriteRenderer _leadRenderer;
    [SerializeField] private SpriteRenderer _followerRenderer;

    [Header("In-pair motion")]
    [SerializeField] private float _maxSeparationOffsetX = 0.6f;
    [SerializeField] private float _inPairLerpSpeed = 18f;

    private Quaternion _uprightWorldRotation;
    private Vector3 _homeLocalPosition;
    private Vector3 _leadHomeLocalPosition;
    private Vector3 _followerHomeLocalPosition;
    private bool _cachedDancerHomePositions;

    // 0 = home positions, >0 = more apart, <0 = more together.
    private float _separationOffsetX;

    private void Awake()
    {
        _uprightWorldRotation = transform.rotation;
        _homeLocalPosition = transform.localPosition;

        AutoFindDancersIfNeeded();
        CacheDancerHomePositionsIfNeeded();
    }

    private void LateUpdate()
    {
        // Keep the pair upright (non-rotating) while orbiting via parent transform.
        transform.rotation = _uprightWorldRotation;

        UpdateInPairMotion();
    }

    public void ApplyWorldSpaceDelta(Vector2 worldDelta)
    {
        transform.position += new Vector3(worldDelta.x, worldDelta.y, 0f);
    }

    public void ResetToHome()
    {
        transform.localPosition = _homeLocalPosition;
    }

    public void ResetDancersToHome(bool snap = true)
    {
        AutoFindDancersIfNeeded();

        _separationOffsetX = 0f;

        if (_leadRenderer != null)
        {
            if (snap)
            {
                _leadRenderer.transform.localPosition = _leadHomeLocalPosition;
            }
        }

        if (_followerRenderer != null)
        {
            if (snap)
            {
                _followerRenderer.transform.localPosition = _followerHomeLocalPosition;
            }
        }
    }

    /// <summary>
    /// Moves dancers within the pair: positive signedDistance = Apart, negative = Together.
    /// </summary>
    public void ApplyInPairSignedStep(float signedDistance)
    {
        AutoFindDancersIfNeeded();

        if (_leadRenderer == null || _followerRenderer == null)
        {
            return;
        }

        // Positive signedDistance = Apart (increase separation),
        // Negative signedDistance = Together (move closer than home).
        _separationOffsetX = Mathf.Clamp(
            _separationOffsetX + signedDistance,
            -Mathf.Max(0f, _maxSeparationOffsetX),
            Mathf.Max(0f, _maxSeparationOffsetX)
        );
    }

    public SpriteRenderer LeadRenderer
    {
        get
        {
            AutoFindDancersIfNeeded();
            return _leadRenderer;
        }
    }

    public SpriteRenderer FollowerRenderer
    {
        get
        {
            AutoFindDancersIfNeeded();
            return _followerRenderer;
        }
    }

    public SpriteRenderer GetRenderer(bool lead)
    {
        return lead ? LeadRenderer : FollowerRenderer;
    }

    private void AutoFindDancersIfNeeded()
    {
        if (_leadRenderer != null && _followerRenderer != null)
        {
            return;
        }

        // Prefab convention: children are named DancerLead / DancerFollower.
        if (_leadRenderer == null)
        {
            Transform lead = transform.Find("DancerLead");
            if (lead != null)
            {
                _leadRenderer = lead.GetComponent<SpriteRenderer>();
            }
        }

        if (_followerRenderer == null)
        {
            Transform follower = transform.Find("DancerFollower");
            if (follower != null)
            {
                _followerRenderer = follower.GetComponent<SpriteRenderer>();
            }
        }

        // Fallback: if names changed, grab first two SpriteRenderers in children.
        if (_leadRenderer == null || _followerRenderer == null)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            if (renderers != null && renderers.Length >= 2)
            {
                if (_leadRenderer == null)
                {
                    _leadRenderer = renderers[0];
                }

                if (_followerRenderer == null)
                {
                    _followerRenderer = renderers[1];
                }
            }
        }
        CacheDancerHomePositionsIfNeeded();
    }

    private void CacheDancerHomePositionsIfNeeded()
    {
        if (_cachedDancerHomePositions)
        {
            return;
        }

        if (_leadRenderer == null || _followerRenderer == null)
        {
            return;
        }

        _leadHomeLocalPosition = _leadRenderer.transform.localPosition;
        _followerHomeLocalPosition = _followerRenderer.transform.localPosition;
        _cachedDancerHomePositions = true;
    }

    private void UpdateInPairMotion()
    {
        if (_leadRenderer == null || _followerRenderer == null || !_cachedDancerHomePositions)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, _inPairLerpSpeed) * Time.deltaTime);

        Vector3 leadTarget = _leadHomeLocalPosition;
        Vector3 followerTarget = _followerHomeLocalPosition;

        leadTarget.x = _leadHomeLocalPosition.x - _separationOffsetX;
        followerTarget.x = _followerHomeLocalPosition.x + _separationOffsetX;

        _leadRenderer.transform.localPosition = Vector3.Lerp(_leadRenderer.transform.localPosition, leadTarget, t);
        _followerRenderer.transform.localPosition = Vector3.Lerp(_followerRenderer.transform.localPosition, followerTarget, t);
    }
}


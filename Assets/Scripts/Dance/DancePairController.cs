using UnityEngine;

public sealed class DancePairController : MonoBehaviour
{
    [Header("Dancers (optional; auto-found if not set)")]
    [SerializeField] private SpriteRenderer _leadRenderer;
    [SerializeField] private SpriteRenderer _followerRenderer;

    private Quaternion _uprightWorldRotation;
    private Vector3 _homeLocalPosition;

    private void Awake()
    {
        _uprightWorldRotation = transform.rotation;
        _homeLocalPosition = transform.localPosition;

        AutoFindDancersIfNeeded();
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

    public void ResetToHome()
    {
        transform.localPosition = _homeLocalPosition;
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
    }
}


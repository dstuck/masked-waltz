using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BeatPulseUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("Optional. If set, this is the legacy single target.")]
    [SerializeField] private Graphic _graphic;

    [Tooltip("Optional. If set, all of these will be tinted together.")]
    [SerializeField] private Graphic[] _tintTargets;

    [Header("Pulse")]
    [SerializeField] private float _maxAlpha = 0.25f;
    [SerializeField] private float _durationSeconds = 0.12f;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine _pulseRoutine;

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _graphic = GetComponent<Graphic>();
        _tintTargets = GetDefaultTintTargets();
    }

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_tintTargets == null || _tintTargets.Length == 0)
        {
            _tintTargets = GetDefaultTintTargets();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
    }

    public void SetMaxAlpha(float maxAlpha)
    {
        _maxAlpha = Mathf.Clamp01(maxAlpha);
    }

    public void SetDurationSeconds(float seconds)
    {
        _durationSeconds = Mathf.Max(0.0001f, seconds);
    }

    public void SetColor(Color c)
    {
        if (_tintTargets == null || _tintTargets.Length == 0)
        {
            // Legacy fallback.
            if (_graphic == null)
            {
                _graphic = GetComponent<Graphic>();
            }

            if (_graphic == null)
            {
                return;
            }

            SetGraphicColor(_graphic, c);
            return;
        }

        for (int i = 0; i < _tintTargets.Length; i++)
        {
            Graphic g = _tintTargets[i];
            if (g == null)
            {
                continue;
            }

            SetGraphicColor(g, c);
        }
    }

    private static void SetGraphicColor(Graphic g, Color c)
    {
        // Keep alpha driven by CanvasGroup; only update RGB.
        Color current = g.color;
        current.r = c.r;
        current.g = c.g;
        current.b = c.b;
        current.a = 1f;
        g.color = current;
    }

    private Graphic[] GetDefaultTintTargets()
    {
        // Tint all Images/RawImages under this pulse object by default.
        // (Excludes TMP_Text so we don't accidentally tint text.)
        Graphic[] all = GetComponentsInChildren<Graphic>(includeInactive: true);
        if (all == null || all.Length == 0)
        {
            return System.Array.Empty<Graphic>();
        }

        var list = new System.Collections.Generic.List<Graphic>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            Graphic g = all[i];
            if (g == null)
            {
                continue;
            }

            if (g is Image || g is RawImage)
            {
                list.Add(g);
            }
        }

        return list.ToArray();
    }

    public void Pulse()
    {
        if (!isActiveAndEnabled || _canvasGroup == null)
        {
            return;
        }

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
        }

        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float duration = Mathf.Max(0.0001f, _durationSeconds);
        float maxAlpha = Mathf.Clamp01(_maxAlpha);

        float t = 0f;
        while (t < duration)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float normalized = Mathf.Clamp01(t / duration);
            float curveValue = _fadeCurve != null ? _fadeCurve.Evaluate(normalized) : 0f;
            _canvasGroup.alpha = Mathf.Clamp01(curveValue) * maxAlpha;

            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _pulseRoutine = null;
    }
}

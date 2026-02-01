using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BeatPulseUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Graphic _graphic;

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
    }

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_graphic == null)
        {
            _graphic = GetComponent<Graphic>();
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

    public void SetColor(Color c)
    {
        if (_graphic == null)
        {
            return;
        }

        // Keep alpha driven by CanvasGroup; only update RGB.
        Color current = _graphic.color;
        current.r = c.r;
        current.g = c.g;
        current.b = c.b;
        current.a = 1f;
        _graphic.color = current;
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

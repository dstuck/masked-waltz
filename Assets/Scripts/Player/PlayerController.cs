using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private DancePairController _controlledPair;
    [SerializeField] private float _moveSpeedUnitsPerSecond = 2f;

    [Header("Movement")]
    [SerializeField] private float _moveDirectionDeadzone = 0.2f;

    [Header("In-Pair Motion (Apart/Together)")]
    [SerializeField] private float _inPairStepDistanceUnits = 0.15f;

    [Header("NPC In-Pair Auto Step (ATT)")]
    [SerializeField] private bool _enableNpcInPairAutoStep = true;
    [SerializeField] private float _npcInPairStepDistanceUnits = 0.12f;

    [Header("Partner Swap (AAA)")]
    [SerializeField] private float _partnerSwapMaxDistance = 4f;
    [SerializeField, Range(5f, 90f)] private float _partnerSwapConeHalfAngleDegrees = 35f;

    [Header("Rhythm")]
    [SerializeField] private BeatClock _beatClock;
    [SerializeField] private PlayerMarker _playerMarker;

    private InputSystem_Actions _actions;

    private enum BeatBucketInput
    {
        None = 0,
        Apart = 1,
        Together = 2,
        Invalid = 3
    }

    private sealed class MeasureBuffer
    {
        public readonly BeatBucketInput[] Inputs = new BeatBucketInput[3];
    }

    private static readonly HashSet<string> _validPatterns =
        new HashSet<string>(StringComparer.Ordinal) { "ATT", "TAA", "AAA", "TTT" };

    // Keyed by measureStartBeatIndex (downbeat beatIndex).
    private readonly Dictionary<int, MeasureBuffer> _buffersByMeasureStart = new Dictionary<int, MeasureBuffer>();

    private int _currentWaltzStep = 1;

    // If set, movement input is ignored while CurrentBeatIndex < _movementLockoutUntilBeatIndex.
    private int _movementLockoutUntilBeatIndex = int.MinValue;
    private float _movementLockoutUntilTime = float.NegativeInfinity;

    private Vector2 _lastNonZeroMoveDirection = Vector2.zero;
    private bool _markerOnLead = true;

    private void OnEnable()
    {
        _actions = new InputSystem_Actions();
        _actions.Player.Enable();

        _actions.Player.Apart.performed += OnApartPerformed;
        _actions.Player.Together.performed += OnTogetherPerformed;

        if (_beatClock != null)
        {
            _beatClock.BeatTick += OnBeatTick;
            _beatClock.MeasureStartTick += OnMeasureStartTick;
        }

        // Ensure marker is attached to the currently-controlled pair on enable.
        AttachMarkerToControlledPair();
    }

    private void OnDisable()
    {
        if (_actions != null)
        {
            _actions.Player.Apart.performed -= OnApartPerformed;
            _actions.Player.Together.performed -= OnTogetherPerformed;
            _actions.Player.Disable();
            _actions.Dispose();
            _actions = null;
        }

        if (_beatClock != null)
        {
            _beatClock.BeatTick -= OnBeatTick;
            _beatClock.MeasureStartTick -= OnMeasureStartTick;
        }
    }

    private void Update()
    {
        if (_actions == null || _controlledPair == null)
        {
            return;
        }

        bool movementLocked = IsMovementLocked();
        Vector2 rawMove = _actions.Player.Move.ReadValue<Vector2>();
        UpdateLastNonZeroMoveDirection(rawMove);

        Vector2 move = rawMove;
        if (movementLocked)
        {
            move = Vector2.zero;
        }

        Vector2 delta = move * (_moveSpeedUnitsPerSecond * Time.deltaTime);
        _controlledPair.ApplyWorldSpaceDelta(delta);
    }

    private void OnApartPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (RecordBucketInput(BeatBucketInput.Apart, out int beatIndex))
        {
            TryApplyInPairStep(BeatBucketInput.Apart, beatIndex);
        }
    }

    private void OnTogetherPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (RecordBucketInput(BeatBucketInput.Together, out int beatIndex))
        {
            TryApplyInPairStep(BeatBucketInput.Together, beatIndex);
        }
    }

    private bool RecordBucketInput(BeatBucketInput input, out int beatIndex)
    {
        beatIndex = -1;

        if (_beatClock == null || !_beatClock.IsReady)
        {
            return false;
        }

        beatIndex = _beatClock.GetNearestBeatIndex();
        if (beatIndex < 0)
        {
            return false;
        }

        int measureStartBeatIndex = beatIndex - (beatIndex % 3);

        if (!_buffersByMeasureStart.TryGetValue(measureStartBeatIndex, out MeasureBuffer buffer))
        {
            buffer = new MeasureBuffer();
            _buffersByMeasureStart[measureStartBeatIndex] = buffer;
        }

        int slot = beatIndex - measureStartBeatIndex; // 0..2
        if (slot < 0 || slot > 2)
        {
            return false;
        }

        BeatBucketInput existing = buffer.Inputs[slot];

        if (existing == BeatBucketInput.None)
        {
            buffer.Inputs[slot] = input;
            return true;
        }
        else
        {
            // Multiple presses in the same bucket mark that beat invalid.
            buffer.Inputs[slot] = BeatBucketInput.Invalid;
            return false;
        }
    }

    private void OnBeatTick(int beatIndex)
    {
        if (beatIndex < 0)
        {
            return;
        }

        _currentWaltzStep = (beatIndex % 3) + 1;

        // Non-player pairs should auto-step ATT each measure.
        // BeatClock invokes BeatTick before MeasureStartTick on downbeats, and we reset on MeasureStartTick,
        // so we apply the downbeat 'A' step in MeasureStartTick and only do beats 2 & 3 here.
        if (_enableNpcInPairAutoStep && _currentWaltzStep != 1)
        {
            ApplyNpcInPairAutoStep(beatInMeasure: _currentWaltzStep);
        }
    }

    private void OnMeasureStartTick(int downbeatBeatIndex)
    {
        if (downbeatBeatIndex < 0)
        {
            return;
        }

        int previousMeasureStart = downbeatBeatIndex - 3;
        if (previousMeasureStart < 0)
        {
            return;
        }

        bool isValid = ValidateMeasure(previousMeasureStart, out string pattern);
        if (!isValid)
        {
            _playerMarker?.Flicker();
            // Invalid sequences reset the dancers within the pair immediately (no world teleport).
            _controlledPair?.ResetDancersToHome(snap: true);

            // Stumble: lose movement control for the entire next measure (the measure that just started).
            // downbeatBeatIndex is the start of the current (next) measure.
            _movementLockoutUntilBeatIndex = downbeatBeatIndex + 3;
            _movementLockoutUntilTime = Time.time + GetMeasureDurationSecondsFallback();
        }
        else
        {
            // Apply measure-boundary actions based on the just-finished measure pattern.
            if (pattern == "AAA")
            {
                TrySwapControlledPairInMoveDirection();
            }
            else if (pattern == "TTT")
            {
                ToggleLeaderFollower();
            }
        }

        // Always reset in-pair motion at measure boundaries so it cannot drift between measures.
        _controlledPair?.ResetDancersToHome(snap: false);

        if (_enableNpcInPairAutoStep)
        {
            // Measure start is the downbeat. Apply the 'A' step for NPC pairs after resets so it sticks.
            ApplyNpcInPairAutoStep(beatInMeasure: 1);
        }

        // Drop the previous measure so we only keep recent buffers.
        _buffersByMeasureStart.Remove(previousMeasureStart);
    }

    private bool ValidateMeasure(int measureStartBeatIndex, out string pattern)
    {
        pattern = string.Empty;

        if (!_buffersByMeasureStart.TryGetValue(measureStartBeatIndex, out MeasureBuffer buffer) || buffer == null)
        {
            // No inputs recorded at all -> invalid.
            pattern = "___";
            return false;
        }

        char[] chars = new char[3];
        for (int i = 0; i < 3; i++)
        {
            chars[i] = buffer.Inputs[i] switch
            {
                BeatBucketInput.Apart => 'A',
                BeatBucketInput.Together => 'T',
                BeatBucketInput.None => '_',
                _ => 'X' // Invalid
            };
        }

        pattern = new string(chars);

        // Any missing or invalid beats fail validation.
        if (pattern.Contains('_') || pattern.Contains('X'))
        {
            return false;
        }

        return _validPatterns.Contains(pattern);
    }

    private void UpdateLastNonZeroMoveDirection(Vector2 rawMove)
    {
        float deadzone = Mathf.Clamp01(_moveDirectionDeadzone);
        if (rawMove.sqrMagnitude < (deadzone * deadzone))
        {
            return;
        }

        _lastNonZeroMoveDirection = rawMove.normalized;
    }

    private void TrySwapControlledPairInMoveDirection()
    {
        if (_controlledPair == null)
        {
            return;
        }

        // Directional requirement: only swap if we have a recent non-zero move direction.
        if (_lastNonZeroMoveDirection == Vector2.zero)
        {
            return;
        }

        DancePairController target = FindBestPairInDirection(_controlledPair, _lastNonZeroMoveDirection);
        if (target == null)
        {
            return;
        }

        // Reset old pair to home so it doesn't keep its player offset.
        _controlledPair.ResetToHome();
        _controlledPair.ResetDancersToHome();

        _controlledPair = target;
        AttachMarkerToControlledPair();
    }

    private void TryApplyInPairStep(BeatBucketInput input, int beatIndex)
    {
        if (_controlledPair == null)
        {
            return;
        }

        // Stumble means you can't act (including in-pair motion), but we still record inputs for UI/validation.
        if (IsMovementLocked())
        {
            return;
        }

        if (input != BeatBucketInput.Apart && input != BeatBucketInput.Together)
        {
            return;
        }

        int beatInMeasure = (beatIndex % 3) + 1; // 1..3
        // Downbeat is larger; offbeats are half as big.
        float stepScale = beatInMeasure == 1 ? 2f : 1f;

        float signed = input == BeatBucketInput.Apart ? 1f : -1f;
        float step = Mathf.Max(0f, _inPairStepDistanceUnits) * stepScale * signed;

        _controlledPair.ApplyInPairSignedStep(step);
    }

    private void ApplyNpcInPairAutoStep(int beatInMeasure)
    {
        if (_beatClock == null || !_beatClock.IsReady)
        {
            return;
        }

        float stepBase = Mathf.Max(0f, _npcInPairStepDistanceUnits);
        if (stepBase <= 0f)
        {
            return;
        }

        float signedStep = beatInMeasure == 1 ? stepBase : -stepBase; // ATT

        // Downbeat is stronger (2x), matching player in-pair logic.
        if (beatInMeasure == 1)
        {
            signedStep *= 2f;
        }

        DancePairController[] pairs = FindObjectsByType<DancePairController>(FindObjectsSortMode.None);
        if (pairs == null || pairs.Length == 0)
        {
            return;
        }

        for (int i = 0; i < pairs.Length; i++)
        {
            DancePairController p = pairs[i];
            if (p == null || p == _controlledPair)
            {
                continue;
            }

            p.ApplyInPairSignedStep(signedStep);
        }
    }

    private DancePairController FindBestPairInDirection(DancePairController current, Vector2 dir)
    {
        float maxDist = Mathf.Max(0.01f, _partnerSwapMaxDistance);
        float coneHalfAngle = Mathf.Clamp(_partnerSwapConeHalfAngleDegrees, 0.01f, 179f);
        float minDot = Mathf.Cos(coneHalfAngle * Mathf.Deg2Rad);

        Vector2 origin = current != null ? (Vector2)current.transform.position : Vector2.zero;
        Vector2 direction = dir.normalized;

        DancePairController[] pairs = FindObjectsByType<DancePairController>(FindObjectsSortMode.None);
        if (pairs == null || pairs.Length == 0)
        {
            return null;
        }

        DancePairController best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < pairs.Length; i++)
        {
            DancePairController p = pairs[i];
            if (p == null || p == current)
            {
                continue;
            }

            Vector2 to = (Vector2)p.transform.position - origin;
            float dist = to.magnitude;
            if (dist <= 0.0001f || dist > maxDist)
            {
                continue;
            }

            float dot = Vector2.Dot(direction, to / dist);
            if (dot < minDot)
            {
                continue;
            }

            // Prefer closer and better aligned.
            float score = dot / (0.001f + dist);
            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        return best;
    }

    private void ToggleLeaderFollower()
    {
        _markerOnLead = !_markerOnLead;
        AttachMarkerToControlledPair();
    }

    private void AttachMarkerToControlledPair()
    {
        if (_playerMarker == null || _controlledPair == null)
        {
            return;
        }

        SpriteRenderer targetRenderer = _controlledPair.GetRenderer(_markerOnLead);
        if (targetRenderer == null)
        {
            return;
        }

        _playerMarker.AttachTo(targetRenderer);
    }

    public void SetControlledPair(DancePairController newPair, bool resetOffset)
    {
        _controlledPair = newPair;
    }

    public int CurrentWaltzStep => _currentWaltzStep;

    public void GetCurrentMeasureBeatSymbols(out char beat1, out char beat2, out char beat3)
    {
        if (_beatClock == null || !_beatClock.IsReady)
        {
            beat1 = '_';
            beat2 = '_';
            beat3 = '_';
            return;
        }

        int beatIndex = _beatClock.CurrentBeatIndex;
        if (beatIndex < 0)
        {
            beat1 = '_';
            beat2 = '_';
            beat3 = '_';
            return;
        }

        int measureStartBeatIndex = beatIndex - (beatIndex % 3);

        if (!_buffersByMeasureStart.TryGetValue(measureStartBeatIndex, out MeasureBuffer buffer) || buffer == null)
        {
            beat1 = '_';
            beat2 = '_';
            beat3 = '_';
            return;
        }

        beat1 = ToSymbol(buffer.Inputs[0]);
        beat2 = ToSymbol(buffer.Inputs[1]);
        beat3 = ToSymbol(buffer.Inputs[2]);
    }

    private static char ToSymbol(BeatBucketInput v)
    {
        return v switch
        {
            BeatBucketInput.Apart => 'A',
            BeatBucketInput.Together => 'T',
            BeatBucketInput.None => '_',
            _ => 'X'
        };
    }

    private bool IsMovementLocked()
    {
        if (_movementLockoutUntilBeatIndex == int.MinValue)
        {
            return false;
        }

        // If the beat clock isn't usable, don't soft-lock movement forever.
        if (_beatClock == null || !_beatClock.IsReady)
        {
            ClearMovementLockout();
            return false;
        }

        int beatIndex = _beatClock.CurrentBeatIndex;
        bool beatWindowExpired = beatIndex >= 0 && beatIndex >= _movementLockoutUntilBeatIndex;
        bool timeWindowExpired = !float.IsFinite(_movementLockoutUntilTime) || Time.time >= _movementLockoutUntilTime;

        // Clear as soon as either window has definitely elapsed.
        if (beatWindowExpired || timeWindowExpired)
        {
            ClearMovementLockout();
            return false;
        }

        // If we can't compute beat progress yet (e.g., negative beat index from offset),
        // fall back to the time window.
        return true;
    }

    private void ClearMovementLockout()
    {
        _movementLockoutUntilBeatIndex = int.MinValue;
        _movementLockoutUntilTime = float.NegativeInfinity;
    }

    private float GetMeasureDurationSecondsFallback()
    {
        // One measure = beatsPerMeasure beats.
        // If BPM isn't configured, pick a short fallback so we never lock forever.
        float bpm = _beatClock != null ? _beatClock.Bpm : 0f;
        float safeBpm = Mathf.Max(30f, bpm); // clamp to a sane minimum
        float secondsPerBeat = 60f / safeBpm;
        int beatsPerMeasure = _beatClock != null ? _beatClock.BeatsPerMeasure : 3;
        return secondsPerBeat * Mathf.Max(1, beatsPerMeasure);
    }
}


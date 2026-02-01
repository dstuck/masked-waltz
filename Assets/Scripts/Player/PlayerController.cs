using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private DancePairController _controlledPair;
    [SerializeField] private float _moveSpeedUnitsPerSecond = 2f;

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

        Vector2 move = _actions.Player.Move.ReadValue<Vector2>();
        Vector2 delta = move * (_moveSpeedUnitsPerSecond * Time.deltaTime);
        _controlledPair.ApplyWorldSpaceDelta(delta);
    }

    private void OnApartPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        RecordBucketInput(BeatBucketInput.Apart);
    }

    private void OnTogetherPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        RecordBucketInput(BeatBucketInput.Together);
    }

    private void RecordBucketInput(BeatBucketInput input)
    {
        if (_beatClock == null || !_beatClock.IsReady)
        {
            return;
        }

        int beatIndex = _beatClock.GetNearestBeatIndex();
        if (beatIndex < 0)
        {
            return;
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
            return;
        }

        BeatBucketInput existing = buffer.Inputs[slot];

        if (existing == BeatBucketInput.None)
        {
            buffer.Inputs[slot] = input;
        }
        else
        {
            // Multiple presses in the same bucket mark that beat invalid.
            buffer.Inputs[slot] = BeatBucketInput.Invalid;
        }
    }

    private void OnBeatTick(int beatIndex)
    {
        if (beatIndex < 0)
        {
            return;
        }

        _currentWaltzStep = (beatIndex % 3) + 1;
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
}


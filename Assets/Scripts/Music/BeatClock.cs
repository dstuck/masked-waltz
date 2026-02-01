using System;
using UnityEngine;

public sealed class BeatClock : MonoBehaviour
{
    [SerializeField] private int _beatsPerMeasure = 3;
    [SerializeField] private BeatManager _beatManager;
    [Tooltip("Shifts the beat grid without changing BPM.\nUse this to align beat 1 (downbeat) with your music.\nExample: if Frame1 behaves like beat 2, set this to -1.")]
    [SerializeField] private int _beatIndexOffset = 0;

    /// <summary>Invoked when a new half-beat boundary is crossed (index is monotonically increasing).</summary>
    public event Action<int> HalfBeatTick;

    /// <summary>Invoked when a new beat boundary is crossed (index is monotonically increasing).</summary>
    public event Action<int> BeatTick;

    /// <summary>Invoked on downbeat / measure start (beatIndex % beatsPerMeasure == 0).</summary>
    public event Action<int> MeasureStartTick;

    private int _lastHalfBeatIndex = -1;

    public float Bpm => _beatManager != null ? _beatManager.Bpm : 0f;
    public int BeatsPerMeasure => Mathf.Max(1, _beatsPerMeasure);

    public AudioSource AudioSource => _beatManager != null ? _beatManager.AudioSource : null;

    public bool IsReady => AudioSource != null && AudioSource.clip != null;

    public int CurrentHalfBeatIndex => IsReady ? GetHalfBeatIndexFromSamples(AudioSource.timeSamples) : 0;
    public int CurrentBeatIndex => ApplyBeatIndexOffset(CurrentHalfBeatIndex / 2);
    public int CurrentBeatInMeasure => CurrentBeatIndex < 0 ? 1 : (PositiveMod(CurrentBeatIndex, BeatsPerMeasure) + 1);
    public bool IsDownBeat => CurrentBeatIndex >= 0 && PositiveMod(CurrentBeatIndex, BeatsPerMeasure) == 0;

    private void Reset()
    {
        _beatManager = GetComponent<BeatManager>();
    }

    private void OnEnable()
    {
        SyncToAudioTime();
    }

    /// <summary>
    /// Snap internal counters to the current audio time (prevents catch-up bursts on enable).
    /// </summary>
    public void SyncToAudioTime()
    {
        _lastHalfBeatIndex = IsReady ? GetHalfBeatIndexFromSamples(AudioSource.timeSamples) : -1;
    }

    private void Update()
    {
        if (!IsReady)
        {
            return;
        }

        int currentHalfBeatIndex = GetHalfBeatIndexFromSamples(AudioSource.timeSamples);

        // If audio time jumped backwards (restart/seek), resync.
        if (_lastHalfBeatIndex > currentHalfBeatIndex)
        {
            _lastHalfBeatIndex = currentHalfBeatIndex;
            return;
        }

        while (_lastHalfBeatIndex < currentHalfBeatIndex)
        {
            _lastHalfBeatIndex++;
            HalfBeatTick?.Invoke(_lastHalfBeatIndex);

            if ((_lastHalfBeatIndex % 2) == 0)
            {
                int beatIndex = ApplyBeatIndexOffset(_lastHalfBeatIndex / 2);
                if (beatIndex >= 0)
                {
                    BeatTick?.Invoke(beatIndex);

                    if (PositiveMod(beatIndex, BeatsPerMeasure) == 0)
                    {
                        MeasureStartTick?.Invoke(beatIndex);
                    }
                }
            }
        }
    }

    public int GetNearestBeatIndex()
    {
        return IsReady ? GetNearestBeatIndex(AudioSource.timeSamples) : 0;
    }

    public int GetNearestBeatIndex(int timeSamples)
    {
        // Bucket from half-beat to half-beat around each beat (midpoints are boundaries).
        int halfBeatIndex = GetHalfBeatIndexFromSamples(timeSamples);
        int bucketBeatIndex = Mathf.Max(0, (halfBeatIndex + 1) / 2);
        return ApplyBeatIndexOffset(bucketBeatIndex);
    }

    private int GetHalfBeatIndexFromSamples(int timeSamples)
    {
        float halfBeatPosition = GetHalfBeatPositionFromSamples(timeSamples);
        return Mathf.Max(0, Mathf.FloorToInt(halfBeatPosition));
    }

    private float GetHalfBeatPositionFromSamples(int timeSamples)
    {
        // halfBeats = seconds / (secondsPerBeat/2) = seconds * 2 / secondsPerBeat
        float seconds = GetSecondsFromSamples(timeSamples);
        float secondsPerBeat = GetSecondsPerBeat();
        if (secondsPerBeat <= 0f)
        {
            return 0f;
        }

        return (seconds * 2f) / secondsPerBeat;
    }

    private float GetBeatPositionFromSamples(int timeSamples)
    {
        float seconds = GetSecondsFromSamples(timeSamples);
        float secondsPerBeat = GetSecondsPerBeat();
        if (secondsPerBeat <= 0f)
        {
            return 0f;
        }

        return seconds / secondsPerBeat;
    }

    private float GetSecondsFromSamples(int timeSamples)
    {
        AudioSource audioSource = AudioSource;
        int frequency = audioSource != null && audioSource.clip != null ? audioSource.clip.frequency : 0;
        if (frequency <= 0)
        {
            return 0f;
        }

        return timeSamples / (float)frequency;
    }

    private float GetSecondsPerBeat()
    {
        float safeBpm = Mathf.Max(0.0001f, Bpm);
        return 60f / safeBpm;
    }

    private int ApplyBeatIndexOffset(int beatIndex)
    {
        return beatIndex + _beatIndexOffset;
    }

    private static int PositiveMod(int value, int modulus)
    {
        if (modulus <= 0)
        {
            return 0;
        }

        int m = value % modulus;
        return m < 0 ? m + modulus : m;
    }
}


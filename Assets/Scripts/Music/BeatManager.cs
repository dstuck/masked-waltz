using UnityEngine;
using UnityEngine.Events;

public sealed class BeatManager : MonoBehaviour
{
    [SerializeField] private float _bpm = 75f;
    [SerializeField] private AudioSource _audioSource;
    [Header("Events")]
    [SerializeField] private Intervals[] _intervals;

    public float Bpm => _bpm;
    public AudioSource AudioSource => _audioSource;

    private void Update()
    {
        if (_audioSource == null || _audioSource.clip == null)
        {
            return;
        }

        foreach (Intervals interval in _intervals)
        {
            float sampledTime =
                (_audioSource.timeSamples / (_audioSource.clip.frequency * interval.GetIntervalLength(_bpm)));
            interval.CheckForNewInterval(sampledTime);
        }
    }
}

[System.Serializable]
public sealed class Intervals
{
    [SerializeField] private float _steps = 1f;
    [SerializeField] private UnityEvent _trigger;
    private int _lastInterval;

    public float GetIntervalLength(float bpm)
    {
        return 60f / (bpm * _steps);
    }

    public void CheckForNewInterval(float interval)
    {
        if (Mathf.FloorToInt(interval) != _lastInterval)
        {
            _lastInterval = Mathf.FloorToInt(interval);
            _trigger?.Invoke();
        }
    }
}

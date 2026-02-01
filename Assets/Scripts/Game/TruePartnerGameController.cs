using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TruePartnerGameController : MonoBehaviour
{
    [Header("References (assign in scene)")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private BeatClock _beatClock;
    [SerializeField] private BeatPulseUI _beatPulse;

    [Header("Pulse Mapping (distance -> alpha/color)")]
    [Tooltip("If <= 0, we use the initial distance to the target pair.")]
    [SerializeField] private float _maxDistance = 0f;
    [SerializeField] private float _farPulseMaxAlpha = 0.05f;
    [SerializeField] private float _nearPulseMaxAlpha = 0.25f;
    [SerializeField] private float _farPulseDurationSeconds = 0.10f;
    [SerializeField] private float _nearPulseDurationSeconds = 0.28f;
    [SerializeField] private Color _farPulseColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color _nearPulseColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Target Selection")]
    [SerializeField, Min(1)] private int _pickFromFarthestN = 3;

    [Header("Win Flow")]
    [SerializeField] private TMP_Text _winText;
    [SerializeField] private float _winDelaySeconds = 3f;
    [SerializeField] private float _restartDelaySeconds = 3f;

    [Header("Win Particles (optional)")]
    [SerializeField] private ParticleSystem _winParticlesPrefab;

    [Header("Debug Target Marker")]
    [SerializeField] private bool _showTargetMarker = false;
    [SerializeField] private Sprite _targetMarkerSprite;
    [SerializeField] private Vector3 _targetMarkerLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float _targetMarkerScale = 0.25f;
    [SerializeField] private Color _targetMarkerColor = new Color(1f, 0.2f, 0.8f, 0.9f);

    private DancePairController _targetPair;
    private SpriteRenderer _targetDancerRenderer;
    private GameObject _targetMarkerInstance;
    private bool _hasWon;
    private Coroutine _winRoutine;

    private void Awake()
    {
        if (_winText != null)
        {
            _winText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        SelectTarget();
        SetupMaxDistanceIfNeeded();
        SetupDebugMarker();
    }

    private void Update()
    {
        if (!isActiveAndEnabled || _player == null || _targetPair == null)
        {
            return;
        }

        UpdatePulseFromDistance();
        CheckWin();
    }

    private bool ValidateReferences()
    {
        if (_player == null)
        {
            Debug.LogError($"{nameof(TruePartnerGameController)}: Assign _player (PlayerController) in the inspector.", this);
            return false;
        }

        if (_beatPulse == null)
        {
            Debug.LogError($"{nameof(TruePartnerGameController)}: Assign _beatPulse (BeatPulseUI) in the inspector.", this);
            return false;
        }

        // _beatClock is optional; kept for future consistency (measure timing etc).
        return true;
    }

    private void SelectTarget()
    {
        DancePairController playerPair = _player.ControlledPair;
        if (playerPair == null)
        {
            return;
        }

        DancePairController[] pairs = FindObjectsByType<DancePairController>(FindObjectsSortMode.None);
        if (pairs == null || pairs.Length == 0)
        {
            return;
        }

        var candidates = new System.Collections.Generic.List<(DancePairController pair, float dist)>();
        Vector2 origin = playerPair.transform.position;

        for (int i = 0; i < pairs.Length; i++)
        {
            DancePairController p = pairs[i];
            if (p == null || p == playerPair)
            {
                continue;
            }

            float dist = Vector2.Distance(origin, p.transform.position);
            candidates.Add((p, dist));
        }

        if (candidates.Count == 0)
        {
            return;
        }

        // Sort by distance descending and pick randomly among the farthest N.
        candidates.Sort((a, b) => b.dist.CompareTo(a.dist));
        int topN = Mathf.Clamp(_pickFromFarthestN, 1, candidates.Count);
        int pickIndex = Random.Range(0, topN);

        _targetPair = candidates[pickIndex].pair;

        bool pickLead = Random.value < 0.5f;
        _targetDancerRenderer = _targetPair != null ? _targetPair.GetRenderer(pickLead) : null;
    }

    private void UpdatePulseFromDistance()
    {
        if (_beatPulse == null || _player == null || _targetPair == null)
        {
            return;
        }

        float dist = Vector2.Distance(_player.ControlledPair.transform.position, _targetPair.transform.position);
        float maxDist = Mathf.Max(0.01f, _maxDistance);

        // Linear with distance: t=0 at maxDist, t=1 at dist=0.
        float t = Mathf.InverseLerp(maxDist, 0f, dist);

        _beatPulse.SetMaxAlpha(Mathf.Lerp(_farPulseMaxAlpha, _nearPulseMaxAlpha, t));
        _beatPulse.SetColor(Color.Lerp(_farPulseColor, _nearPulseColor, t));
        _beatPulse.SetDurationSeconds(Mathf.Lerp(_farPulseDurationSeconds, _nearPulseDurationSeconds, t));
    }

    private void SetupMaxDistanceIfNeeded()
    {
        if (_maxDistance > 0f || _player == null || _targetPair == null || _player.ControlledPair == null)
        {
            return;
        }

        float initial = Vector2.Distance(_player.ControlledPair.transform.position, _targetPair.transform.position);
        _maxDistance = Mathf.Max(0.01f, initial);
    }

    private void CheckWin()
    {
        if (_hasWon || _targetPair == null)
        {
            return;
        }

        if (_player.ControlledPair != _targetPair)
        {
            return;
        }

        _hasWon = true;
        _player.SetInputEnabled(false);

        if (_winParticlesPrefab != null)
        {
            ParticleSystem ps = Instantiate(_winParticlesPrefab, _player.ControlledPair.transform);
            ps.transform.localPosition = Vector3.zero;
        }

        if (_winRoutine != null)
        {
            StopCoroutine(_winRoutine);
        }

        _winRoutine = StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        float winDelay = Mathf.Max(0f, _winDelaySeconds);
        float restartDelay = Mathf.Max(0f, _restartDelaySeconds);

        yield return new WaitForSeconds(winDelay);

        if (_winText != null)
        {
            _winText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(restartDelay);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetupDebugMarker()
    {
        if (!_showTargetMarker || _targetDancerRenderer == null || _targetMarkerSprite == null)
        {
            return;
        }

        _targetMarkerInstance = new GameObject("TruePartnerMarker");
        _targetMarkerInstance.transform.SetParent(_targetDancerRenderer.transform, worldPositionStays: false);
        _targetMarkerInstance.transform.localPosition = _targetMarkerLocalOffset;
        _targetMarkerInstance.transform.localRotation = Quaternion.identity;
        _targetMarkerInstance.transform.localScale = Vector3.one * Mathf.Max(0.001f, _targetMarkerScale);

        SpriteRenderer sr = _targetMarkerInstance.AddComponent<SpriteRenderer>();
        sr.sprite = _targetMarkerSprite;
        sr.color = _targetMarkerColor;
        sr.sortingLayerID = _targetDancerRenderer.sortingLayerID;
        sr.sortingOrder = _targetDancerRenderer.sortingOrder + 50;
    }
}


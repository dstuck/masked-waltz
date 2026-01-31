using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private DancePairController _controlledPair;
    [SerializeField] private float _moveSpeedUnitsPerSecond = 2f;

    private InputSystem_Actions _actions;

    private void OnEnable()
    {
        _actions = new InputSystem_Actions();
        _actions.Player.Enable();
    }

    private void OnDisable()
    {
        if (_actions != null)
        {
            _actions.Player.Disable();
            _actions.Dispose();
            _actions = null;
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

    public void SetControlledPair(DancePairController newPair, bool resetOffset)
    {
        _controlledPair = newPair;
    }
}


using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [SerializeField] private InputActionReference[] _inputs;

    public event Action<int> OnButtonPressed;

    private Action<InputAction.CallbackContext>[] _handlers;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        _handlers = new Action<InputAction.CallbackContext>[_inputs.Length];

        for (int i = 0; i < _inputs.Length; i++)
        {
            if (_inputs[i] == null) continue;

            int index = i;
            _handlers[i] = _ => OnButtonPressed?.Invoke(index);

            InputAction action = _inputs[i].action;
            action.performed += _handlers[i];
            action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_handlers == null) return;

        for (int i = 0; i < _inputs.Length; i++)
        {
            if (_inputs[i] == null || _handlers[i] == null) continue;

            InputAction action = _inputs[i].action;
            action.performed -= _handlers[i];
            action.Disable();
        }

        _handlers = null;
    }
}
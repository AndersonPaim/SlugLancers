using System;
using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private float _tickDuration = 1f; // duration in seconds

    public Action OnTick;
    public Action OnCombatStarted;
    public Action OnCombatStopped;
    
    private bool _inCombat;
    private Coroutine _tickRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartCombat();
    }

    private void OnDisable()
    {
        StopCombat();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartCombat()
    {
        if (_inCombat) return;
        
        _inCombat = true;
        _tickRoutine = StartCoroutine(TickLoop());
        OnCombatStarted?.Invoke();
    }

    public void StopCombat()
    {
        if (!_inCombat && _tickRoutine == null) return;

        _inCombat = false;

        if (_tickRoutine != null)
        {
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }

        OnCombatStopped?.Invoke();
    }

    private IEnumerator TickLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(_tickDuration);

        while (_inCombat)
        {
            yield return wait;

            if (!_inCombat) yield break;

            OnTick?.Invoke();
        }
    }
}
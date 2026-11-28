using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private float _tickDuration = 1f; // duração do tick em segundos
    [SerializeField] private List<EnemyProperties> _enemies = new();
    [SerializeField] private List<HeroProperties> _heroes = new();
    [SerializeField] private HeroCombatant _heroPrefab;
    [SerializeField] private Transform _heroesPosition;
    [SerializeField] private EnemyCombatant _enemyPrefab;
    [SerializeField] private Transform _enemiesPosition;
    
    public Action OnTick;
    public Action OnCombatStarted;
    public Action OnCombatStopped;

    [HideInInspector] public Combatant SelectedCombatant;

    public bool InCombat => _inCombat;
    public IReadOnlyList<Combatant> Heroes => _heroesCombatants;
    public IReadOnlyList<Combatant> Enemies => _enemyCombatants;

    private readonly List<Combatant> _heroesCombatants = new();
    private readonly List<Combatant> _enemyCombatants = new();
    private readonly List<Combatant> _targets = new();

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

        int buttonOffset = 0;

        foreach (HeroProperties hero in _heroes)
        {
            HeroCombatant combatant = Instantiate(_heroPrefab, _heroesPosition);
            combatant.Initialize(hero, buttonOffset);
            _heroesCombatants.Add(combatant);

            buttonOffset += hero.actions.Count;
        }
        
        foreach (EnemyProperties enemy in _enemies)
        {
            EnemyCombatant combatant = Instantiate(_enemyPrefab, _enemiesPosition);
            combatant.Initialize(enemy, buttonOffset);
            _enemyCombatants.Add(combatant);
        }

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
    
    /*private void HandleCombatantDied(Combatant dead)
    {
        if (!_inCombat) return;
        if (!AnyAlive(_enemyCombatants));
        else if (!AnyAlive(_heroes));
    }*/

    private static bool AnyAlive(List<Combatant> list)
    {
        foreach (Combatant combatant in list)
        {
            if (combatant.IsAlive) return true;
        }

        return false;
    }


    public bool TryExecuteAction(ActionProperties action, Combatant source, List<Combatant> targets)
    {
        if (!_inCombat) return false;

        if (SelectedCombatant != null && source.GetComponent<HeroCombatant>())
        {
            if (action.damageTypes.Contains(DamageTypes.Healing) && SelectedCombatant.GetComponent<EnemyCombatant>())
                return false; //cant heal enemies
            if (!action.damageTypes.Contains(DamageTypes.Healing) && SelectedCombatant.GetComponent<HeroCombatant>())
                return false; //cant damage allies
        }

        Execute(action, source, targets);
        return true;
    }
    
    public void Execute(ActionProperties action, Combatant source, List<Combatant> targets)
    {
        foreach (Combatant target in targets)
        {
            if (!target.IsAlive) continue;

            if (action.damageTypes.Contains(DamageTypes.Healing))
            {
                target.Heal(action.damage);
            }
            else
            {
                target.TakeDamage(action.damage, action.damageTypes);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum EnemyCycleState { Idle, Windup, Action }

public class EnemyCombatant : Combatant
{
    [SerializeField] private TextMeshProUGUI _hpText;
    
    private int _tickOffset = 1; //TODO OFFSET MECHANIC

    public EnemyProperties EnemyProperties { get; private set; }
    public EnemyCycleState State { get; private set; }
    public int TicksLeft => _ticksLeft;
    public ActionCyclesProperties CurrentCycle => _cycles.Count > 0 ? _cycles[_cycleIndex] : null;

    private readonly List<ActionCyclesProperties> _cycles = new();
    private int _cycleIndex;
    private int _ticksLeft;

    public void Initialize(EnemyProperties enemy, int index)
    {
        base.Initialize(enemy, index);
        EnemyProperties = enemy;

        _cycles.Clear();
        AddCycle(enemy.cycle1Id);
        AddCycle(enemy.cycle2Id);
        AddCycle(enemy.cycle3Id);

        _cycleIndex = 0;

        SetState(EnemyCycleState.Idle, _cycles[0].idleTicks + _tickOffset);

        UpdateUI();
    }

    private void AddCycle(ActionCyclesProperties cycle)
    {
        if (cycle != null) _cycles.Add(cycle);
    }

    protected override void HandleTick()
    {
        if (!IsAlive || _cycles.Count == 0) return;

        _ticksLeft--;
        
        if (_ticksLeft <= 0)
        {
            Advance();
        }
    }

    private void Advance()
    {
        ActionCyclesProperties cycle = _cycles[_cycleIndex];

        switch (State)
        {
            case EnemyCycleState.Idle:
                SetState(EnemyCycleState.Windup, cycle.windupTicks);
                break;

            case EnemyCycleState.Windup:
                SetState(EnemyCycleState.Action, GetActionTicks(cycle));
                PerformAction(cycle);
                break;

            case EnemyCycleState.Action:
                _cycleIndex = (_cycleIndex + 1) % _cycles.Count; //restart cycle
                SetState(EnemyCycleState.Idle, _cycles[_cycleIndex].idleTicks + _tickOffset);
                break;
        }
    }

    private void SetState(EnemyCycleState state, int ticks)
    {
        State = state;
        _ticksLeft = ticks;
    }

    private static int GetActionTicks(ActionCyclesProperties cycle)
    {
        return cycle.actionId != null ? cycle.actionId.actionTicks : 0;
    }

    private void PerformAction(ActionCyclesProperties cycle)
    {
        if (cycle.actionId == null) return;


        List<Combatant> targets = new(); //TODO PRIORITY SYSTEM TO PICK TARGETS
        targets.Add(CombatManager.Instance.Heroes[UnityEngine.Random.Range(0, CombatManager.Instance.Heroes.Count)]);
        
        if (CombatManager.Instance.TryExecuteAction(cycle.actionId, this, targets))
        {
            Debug.Log("ENEMY ACTION");
        }
    }

    public override void TakeDamage(float amount, List<DamageTypes> damageTypes)
    {
        base.TakeDamage(amount, damageTypes);
        UpdateUI();
    }

    public override void Heal(float amount)
    {
        base.Heal(amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        _hpText.text = CurrentHp.ToString();
    }
}
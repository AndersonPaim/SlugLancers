using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HeroCombatant : Combatant, IDisposable
{
    [SerializeField] private ActionSlot _actionPrefab;
    [SerializeField] private Transform _actionsPosition;

    public HeroProperties Hero { get; }
    public Combatant SelectedTarget { get; set; }
    public IReadOnlyList<ActionSlot> Slots => _slots;

    [SerializeField] private TextMeshProUGUI _hpText;

    private List<ActionSlot> _slots = new();
    private int _inputValue;

    public void Initialize(HeroProperties hero, int input)
    {
        base.Initialize(hero, input);
        _inputValue = input;

        foreach (ActionProperties action in hero.actions)
        {
            ActionSlot slot = Instantiate(_actionPrefab, _actionsPosition);
            slot.Initialize(action);
            _slots.Add(slot);
        }

        CombatManager.Instance.OnTick += HandleTick;
        InputManager.Instance.OnButtonPressed += HandleButtonPressed;
        UpdateUI();
    }

    public void Dispose()
    {
        CombatManager.Instance.OnTick -= HandleTick;
        InputManager.Instance.OnButtonPressed -= HandleButtonPressed;
    }

    private void HandleTick()
    {
        foreach (ActionSlot slot in _slots) slot.Tick();
    }

    private void HandleButtonPressed(int button)
    {
        Debug.Log($"Button pressed: {button}");
        int index = button - _inputValue;
        if (index < 0 || index >= _slots.Count) return;
        if (!IsAlive) return;

        ActionSlot slot = _slots[index];
        if (!slot.IsReady) return;
        
        if (CombatManager.Instance.TryExecuteAction(slot.Action, this, SelectedTarget))
        {
            Debug.Log($"START COOLDOWN: " + slot.Action.cooldown);
            slot.StartCooldown();
        }
    }

    public override void TakeDamage(float amount, DamageTypes type)
    {
        base.TakeDamage(amount, type);
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
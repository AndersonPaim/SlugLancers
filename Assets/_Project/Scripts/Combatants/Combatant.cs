using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public abstract class Combatant : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _selectedBG;

    protected CombatantProperties Properties;

    public int CurrentHp { get; private set; }
    public int MaxHp => Properties.hp;
    public bool IsAlive => CurrentHp > 0f;

    public event Action<Combatant, float> OnHpChanged;
    public event Action<Combatant> OnDied;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        CombatManager.Instance.SelectedCombatant = this;
        _selectedBG.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CombatManager.Instance.SelectedCombatant == this)
        {
            CombatManager.Instance.SelectedCombatant = null;
        }
        
        _selectedBG.enabled = false;
    }


    protected void Initialize(CombatantProperties properties, int input)
    {
        Properties = properties;
        CurrentHp = properties.hp;
        CombatManager.Instance.OnTick += HandleTick;
    }

    private void OnDestroy()
    {
        CombatManager.Instance.OnTick -= HandleTick;
    }

    protected abstract void HandleTick();

    public virtual void TakeDamage(float amount, List<DamageTypes> damageTypes)
    {
        if (!IsAlive) return;

        float modifier = Properties.GetDamageModifier(damageTypes);
        int final = Mathf.Clamp(Mathf.RoundToInt(amount * modifier), 0, CurrentHp);

        CurrentHp -= final;
        OnHpChanged?.Invoke(this, -final);

        if (!IsAlive) OnDied?.Invoke(this);
    }

    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;

        int healed = Mathf.Clamp(Mathf.RoundToInt(amount), 0, MaxHp - CurrentHp);
        if (healed <= 0) return;

        CurrentHp += healed;
        OnHpChanged?.Invoke(this, healed);
    }
}
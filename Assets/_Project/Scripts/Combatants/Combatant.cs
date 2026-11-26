using System;
using UnityEngine;


public abstract class Combatant : MonoBehaviour
{
    protected CombatantProperties Properties;

    public float CurrentHp { get; private set; }
    public float MaxHp => Properties.hp;
    public bool IsAlive => CurrentHp > 0f;

    public event Action<Combatant, float> OnHpChanged;
    public event Action<Combatant> OnDied;

    protected void Initialize(CombatantProperties properties, int input)
    {
        Properties = properties;
        CurrentHp = properties.hp;
    }

    public virtual void TakeDamage(float amount, DamageTypes type)
    {
        if (!IsAlive) return;

        float multiplier = 1f - Properties.GetResistance(type) / 100f;
        float final = Mathf.Min(CurrentHp, Mathf.Max(0f, amount * multiplier));

        CurrentHp -= final;
        OnHpChanged?.Invoke(this, -final);

        if (!IsAlive) OnDied?.Invoke(this);
    }

    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;

        float healed = Mathf.Clamp(amount, 0f, MaxHp - CurrentHp);
        if (healed <= 0f) return;

        CurrentHp += healed;
        OnHpChanged?.Invoke(this, healed);
    }
}
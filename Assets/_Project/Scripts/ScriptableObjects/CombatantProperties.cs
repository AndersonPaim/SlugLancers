using System.Collections.Generic;
using UnityEngine;

public abstract class CombatantProperties : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Status")]
    public int hp;

    [Header("Resistences")]
    public int resSlashing;
    public int resPiercing;
    public int resCrushing;
    public int resAcidic;
    public int resSaline;
    public int resDivine;
    public int resArcane;
    public int resDebuff;
    public int resPoisoned;
    public int resBleeding;
    public int resBurning;

    public int GetResistance(DamageTypes type) => type switch
    {
        DamageTypes.Slashing => resSlashing,
        DamageTypes.Piercing => resPiercing,
        DamageTypes.Crushing => resCrushing,
        DamageTypes.Acidic   => resAcidic,
        DamageTypes.Saline   => resSaline,
        DamageTypes.Divine   => resDivine,
        DamageTypes.Arcane   => resArcane,
        _ => 0
    };
    
    public float GetTotalDTR(IReadOnlyList<DamageTypes> damageTypes)
    {
        float remaining = 1f;

        foreach (DamageTypes type in damageTypes)
        {
            float resistance = Mathf.Clamp(GetResistance(type), -100, 100) / 100f;
            remaining *= 1f - resistance;
        }

        return (1f - remaining) * 100f;
    }

    public float GetDamageModifier(IReadOnlyList<DamageTypes> damageTypes)
    {
        return 1f - GetTotalDTR(damageTypes) / 100f;
    }
}
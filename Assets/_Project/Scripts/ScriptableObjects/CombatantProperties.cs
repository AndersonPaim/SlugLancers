using UnityEngine;

public abstract class CombatantProperties : ScriptableObject
{
    public string id;
    public string displayName;

    [Header("Status")]
    public float hp;

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
}
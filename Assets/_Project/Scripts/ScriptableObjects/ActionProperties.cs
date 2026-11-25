using UnityEngine;

public enum CombatTags
{
    Deflective, Dazing, Defensive, Breaching, Multihit, Serrated
}

public enum DamageTypes
{
    Slashing, Piercing, Crushing, Acidic, Saline, Divine, Arcane 
}

[CreateAssetMenu(menuName = "ScriptableObjects/Actions/ActionProperties", fileName = "ActionProperties")]
public class ActionProperties : ScriptableObject
{
    public string id;
    public CombatTags combatTags;
    public DamageTypes damageTypes;
    public float damage;
    public float cooldown;
    public string displayName;
}
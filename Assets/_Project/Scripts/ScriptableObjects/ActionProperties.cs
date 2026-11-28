using System.Collections.Generic;
using UnityEngine;

public enum CombatTags
{
    Deflective, Dazing, Defensive, Breaching, Multihit, Serrated
}

public enum DamageTypes
{
    Slashing, Piercing, Crushing, Acidic, Saline, Divine, Arcane, Healing
}

[CreateAssetMenu(menuName = "ScriptableObjects/Actions/ActionProperties", fileName = "ActionProperties")]
public class ActionProperties : ScriptableObject
{
    public string id;
    public CombatTags combatTags;
    public List<DamageTypes> damageTypes = new();
    public float damage;
    public float cooldown;
    public string displayName;
}
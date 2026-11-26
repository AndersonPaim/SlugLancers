using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Player/HeroProperties", fileName = "HeroProperties")]
public class HeroProperties : CombatantProperties
{
    public List<ActionProperties> actions = new();
}
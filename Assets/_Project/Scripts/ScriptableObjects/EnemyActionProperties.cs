using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Actions/EnemyActionProperties", fileName = "EnemyActionProperties")]
public class EnemyActionProperties : ActionProperties
{
    public int actionTicks;
    public int critChance;
}
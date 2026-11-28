using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Actions/ActionCycles", fileName = "ActionCycles")]
public class ActionCyclesProperties : ScriptableObject
{
    public string id;
    public int idleTicks;
    public int windupTicks;
    public EnemyActionProperties actionId;
}
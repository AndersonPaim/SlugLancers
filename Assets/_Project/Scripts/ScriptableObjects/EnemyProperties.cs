using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy/EnemyProperties", fileName = "EnemyProperties")]
public class EnemyProperties : CombatantProperties
{
    public int phase;

    [Header("Cycles")]
    public ActionCyclesProperties cycle1Id;
    public ActionCyclesProperties cycle2Id;
    public ActionCyclesProperties cycle3Id;
}
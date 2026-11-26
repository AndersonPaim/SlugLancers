using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy/EnemyProperties", fileName = "EnemyProperties")]
public class EnemyProperties : CombatantProperties
{
    public int phase;

    [Header("Cycles")]
    public string cycle1Id;
    public string cycle2Id;
    public string cycle3Id;
}
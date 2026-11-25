using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Enemy/EnemyProperties", fileName = "EnemyProperties")]
public class EnemyProperties : ScriptableObject
{
    public string id;
    public string displayName;
    public int phase;

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

    [Header("Cycles")]
    public string cycle1Id;
    public string cycle2Id;
    public string cycle3Id;
}
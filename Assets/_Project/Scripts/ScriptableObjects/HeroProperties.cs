using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Player/HeroProperties", fileName = "HeroProperties")]
public class HeroProperties : ScriptableObject
{
    public string id;
    public string displayName;
    public List<ActionProperties> actions = new();

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
}
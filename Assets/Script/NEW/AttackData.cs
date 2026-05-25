using UnityEngine;

[System.Serializable]
public struct AttackData
{
    public GameObject attacker;
    public Faction attackerFaction;
    public float damage;
    public Vector3 position;
    public AttackType type;
    public int strength;
}
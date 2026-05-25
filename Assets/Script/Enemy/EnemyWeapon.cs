using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [HideInInspector]
    public float attackDamage;
    public float weaponDamage;

    [HideInInspector]
    public AttackType type = AttackType.normal;

    [HideInInspector]
    public int strength = 1;

    Enemy owner;

    void Start()
    {
        owner = GetComponentInParent<Enemy>();
    }

    void OnTriggerEnter(Collider col)
    {
        if (!col.CompareTag("Player") || attackDamage <= 0)
            return;

        PlayerController player = col.GetComponent<PlayerController>();
        if (!player)
            player = col.GetComponentInParent<PlayerController>();

        if (player)
        {
            AttackData attack = new AttackData();
            attack.attacker = owner ? owner.gameObject : gameObject;
            attack.attackerFaction = Faction.Enemy;
            attack.damage = attackDamage + weaponDamage;
            attack.position = transform.position;
            attack.type = type;
            attack.strength = strength;

            player.TakeDamage(attack);
        }
    }
}

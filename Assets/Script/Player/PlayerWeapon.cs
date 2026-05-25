using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [HideInInspector]
    public float attackDamage;
    public float weaponDamage;

    [HideInInspector]
    public AttackType type;
    [HideInInspector]
    public int strength;

    PlayerController player;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (!player)
            player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Treasure") && attackDamage > 0)
            col.SendMessage("Hit");

        if (!col.CompareTag("Enemy") || attackDamage <= 0)
            return;

        Enemy enemy = col.GetComponent<Enemy>();
        if (!enemy)
            enemy = col.GetComponentInParent<Enemy>();

        if (enemy)
        {
            AttackData attack = new AttackData();
            attack.attacker = player ? player.gameObject : gameObject;
            attack.attackerFaction = Faction.Player;
            attack.damage = attackDamage + weaponDamage;
            attack.position = player ? player.transform.position : transform.position;
            attack.type = type;
            attack.strength = strength;

            enemy.TakeDamage(attack);
        }
    }
}

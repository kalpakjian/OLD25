using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    PlayerController player;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (player == null && transform.root != null)
            player = transform.root.GetComponentInChildren<PlayerController>(true);

        Debug.Log($"[PlayerWeapon] owner = {(player ? player.name : "NULL")}");
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Treasure") && attackDamage > 0)
        {
            col.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
            return;
        }

        Debug.Log($"[PlayerWeapon] hit {col.name}, tag={col.tag}, atk={attackDamage}");

        if (attackDamage <= 0 || player == null)
            return;

        // 先找新版 CombatActor
        CombatActor targetActor = FindCombatActor(col);

        if (targetActor != null && targetActor.faction != player.faction)
        {
            AttackData attack = new AttackData
            {
                attacker = player.gameObject,
                attackerFaction = player.faction,
                damage = attackDamage + weaponDamage,
                position = player.transform.position,
                type = type,
                strength = strength
            };
            Debug.Log($"[PlayerWeapon] deal {attack.damage} to {targetActor.name} (CombatActor)");
            targetActor.TakeDamage(attack);
            return;
        }

        Debug.Log($"[PlayerWeapon] no valid target found on {col.name}");
    }

    CombatActor FindCombatActor(Collider col)
    {
        CombatActor t = col.GetComponent<CombatActor>();
        if (t != null) return t;
        t = col.GetComponentInParent<CombatActor>();
        if (t != null) return t;
        if (col.attachedRigidbody != null)
        {
            t = col.attachedRigidbody.GetComponentInParent<CombatActor>();
            if (t != null) return t;
        }
        if (col.transform.root != null)
            t = col.transform.root.GetComponentInChildren<CombatActor>(true);
        return t;
    }
}

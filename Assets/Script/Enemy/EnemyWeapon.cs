using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    // 新版 EnemyBase
    EnemyBase ownerBase;

    GameObject OwnerGO => ownerBase != null ? ownerBase.gameObject : null;
    Transform OwnerTransform => ownerBase != null ? ownerBase.transform : null;

    Faction OwnerFaction => ownerBase != null ? ownerBase.faction : Faction.Enemy;

    bool HasOwner => ownerBase != null;

    void Start()
    {
        // 找新版 EnemyBase
        ownerBase = GetComponentInParent<EnemyBase>();
        if (ownerBase == null && transform.root != null)
            ownerBase = transform.root.GetComponentInChildren<EnemyBase>(true);

        Debug.Log($"[EnemyWeapon] ownerBase={(ownerBase ? ownerBase.name : "NULL")}");
    }

    void OnTriggerEnter(Collider col)
    {
        Debug.Log($"[EnemyWeapon] hit {col.name}, tag={col.tag}, atk={attackDamage}");

        if (attackDamage <= 0 || !HasOwner)
            return;

        // 先找新版 CombatActor（PlayerController : CombatActor）
        CombatActor targetActor = FindCombatActor(col);

        if (targetActor != null && targetActor.faction != OwnerFaction)
        {
            AttackData attack = new AttackData
            {
                attacker = OwnerGO,
                attackerFaction = OwnerFaction,
                damage = attackDamage + weaponDamage,
                position = OwnerTransform != null ? OwnerTransform.position : transform.position,
                type = type,
                strength = strength
            };
            Debug.Log($"[EnemyWeapon] deal {attack.damage} to {targetActor.name} (CombatActor)");
            targetActor.TakeDamage(attack);
            return;
        }

        Debug.Log($"[EnemyWeapon] no valid target found on {col.name}");
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

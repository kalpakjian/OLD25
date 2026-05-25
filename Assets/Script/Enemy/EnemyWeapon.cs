using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    // 新版 EnemyBase
    EnemyBase ownerBase;
    // 舊版 Enemy fallback
    Enemy ownerLegacy;

    GameObject OwnerGO => ownerBase != null ? ownerBase.gameObject :
                          (ownerLegacy != null ? ownerLegacy.gameObject : null);

    Transform OwnerTransform => ownerBase != null ? ownerBase.transform :
                                (ownerLegacy != null ? ownerLegacy.transform : null);

    Faction OwnerFaction => ownerBase != null ? ownerBase.faction : Faction.Enemy;

    bool HasOwner => ownerBase != null || ownerLegacy != null;

    void Start()
    {
        // 找新版 EnemyBase
        ownerBase = GetComponentInParent<EnemyBase>();
        if (ownerBase == null && transform.root != null)
            ownerBase = transform.root.GetComponentInChildren<EnemyBase>(true);

        // 找舊版 Enemy fallback
        if (ownerBase == null)
        {
            ownerLegacy = GetComponentInParent<Enemy>();
            if (ownerLegacy == null && transform.root != null)
                ownerLegacy = transform.root.GetComponentInChildren<Enemy>(true);
        }

        Debug.Log($"[EnemyWeapon] ownerBase={(ownerBase ? ownerBase.name : "NULL")}, " +
                  $"ownerLegacy={(ownerLegacy ? ownerLegacy.name : "NULL")}");
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

        // fallback：如果 PlayerController 沒繼承 CombatActor 的舊場景
        PlayerController playerLegacy = FindPlayerController(col);
        if (playerLegacy != null && targetActor == null)
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
            Debug.Log($"[EnemyWeapon] deal {attack.damage} to {playerLegacy.name} (Legacy PlayerController)");
            playerLegacy.TakeDamage(attack);
        }
        else if (targetActor == null)
        {
            Debug.Log($"[EnemyWeapon] no valid target found on {col.name}");
        }
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

    PlayerController FindPlayerController(Collider col)
    {
        PlayerController p = col.GetComponent<PlayerController>();
        if (p != null) return p;
        p = col.GetComponentInParent<PlayerController>();
        if (p != null) return p;
        if (col.attachedRigidbody != null)
        {
            p = col.attachedRigidbody.GetComponentInParent<PlayerController>();
            if (p != null) return p;
        }
        if (col.transform.root != null)
            p = col.transform.root.GetComponentInChildren<PlayerController>(true);
        return p;
    }
}

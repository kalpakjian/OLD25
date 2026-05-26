using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 共用武器碰撞邏輯。
/// 玩家武器和敵人武器都直接使用此 Component，不再需要子類別。
/// 支援：
/// 1. 同一 phase 內同一目標只命中一次
/// 2. 同一 attack cycle 內可有多個 phase 傷害
/// 3. Treasure 與 CombatActor 都做 phase 去重
/// </summary>
public class WeaponHitbox : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    /// <summary>是否可以打到寶箱（Player 武器需開啟）</summary>
    [SerializeField] protected bool canHitTreasure = false;

    /// <summary>擁有者（PlayerController 或 EnemyBase 皆可，兩者都是 CombatActor）</summary>
    protected CombatActor owner;

    // 同一 phase 內已命中的 CombatActor
    private readonly HashSet<CombatActor> hitTargetsInPhase = new HashSet<CombatActor>();

    // 同一 phase 內已命中的 Treasure Collider
    private readonly HashSet<Collider> hitTreasuresInPhase = new HashSet<Collider>();

    private bool wasAttacking = false;
    private float currentPhaseDamage = -1f;

    protected virtual void Start()
    {
        owner = GetComponentInParent<CombatActor>();
        if (owner == null && transform.root != null)
            owner = transform.root.GetComponentInChildren<CombatActor>(true);
    }

    protected virtual void Update()
    {
        bool isAttacking = attackDamage > 0f;

        if (!isAttacking)
        {
            if (wasAttacking)
            {
                hitTargetsInPhase.Clear();
                hitTreasuresInPhase.Clear();
                currentPhaseDamage = -1f;
            }

            wasAttacking = false;
            return;
        }

        // 攻擊剛開始，或 phase damage 改變時，視為新 phase
        if (!wasAttacking || !Mathf.Approximately(currentPhaseDamage, attackDamage))
        {
            currentPhaseDamage = attackDamage;
            hitTargetsInPhase.Clear();
            hitTreasuresInPhase.Clear();
        }

        wasAttacking = true;
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        TryHit(col);
    }

    protected virtual void OnTriggerStay(Collider col)
    {
        TryHit(col);
    }

    private void TryHit(Collider col)
    {
        if (attackDamage <= 0f || owner == null)
            return;

        if (col == null)
            return;

        if (col.transform.root == owner.transform.root)
            return;

        // Treasure
        if (canHitTreasure && col.CompareTag("Treasure"))
        {
            if (!hitTreasuresInPhase.Add(col))
                return;

            col.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
            return;
        }

        // CombatActor
        CombatActor targetActor = FindCombatActor(col);

        if (targetActor != null && targetActor.faction != owner.faction)
        {
            if (!hitTargetsInPhase.Add(targetActor))
                return;

            AttackData attack = new AttackData
            {
                attacker = owner.gameObject,
                attackerFaction = owner.faction,
                damage = attackDamage + weaponDamage,
                position = owner.transform.position,
                type = type,
                strength = strength
            };

            targetActor.TakeDamage(attack);
        }
    }

    protected CombatActor FindCombatActor(Collider col)
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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 共用武器碰撞邏輯。
/// 玩家武器和敵人武器都直接使用此 Component，不再需要子類別。
/// </summary>
public class WeaponHitbox : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    /// <summary>是否可以打到寶箱（Player 武器需開啟）</summary>
    [SerializeField] protected bool canHitTreasure = false;

    [Header("Debug")]
    [SerializeField] private bool logHit = true;
    [SerializeField] private bool logInvalidHit = false;
    [SerializeField] private bool logRepeatBlocked = false;

    /// <summary>擁有者（PlayerController 或 EnemyBase 皆可，兩者都是 CombatActor）</summary>
    protected CombatActor owner;

    // 同一攻擊窗口內已命中的目標
    private readonly HashSet<CombatActor> hitTargets = new HashSet<CombatActor>();
    private bool wasAttacking = false;

    protected virtual void Start()
    {
        owner = GetComponentInParent<CombatActor>();
        if (owner == null && transform.root != null)
            owner = transform.root.GetComponentInChildren<CombatActor>(true);

        Debug.Log($"[WeaponHitbox] {name} owner = {(owner ? owner.name : "NULL")} ({owner?.GetType().Name})");
    }

    protected virtual void Update()
    {
        bool isAttacking = attackDamage > 0f;

        // 攻擊窗口剛開始
        if (isAttacking && !wasAttacking)
        {
            hitTargets.Clear();
        }
        // 攻擊窗口剛結束
        else if (!isAttacking && wasAttacking)
        {
            hitTargets.Clear();
        }

        wasAttacking = isAttacking;
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        if (attackDamage <= 0 || owner == null)
            return;

        if (col.transform.root == owner.transform.root)
            return;

        if (canHitTreasure && col.CompareTag("Treasure"))
        {
            if (logHit)
                Debug.Log($"[WeaponHitbox] {name} hit treasure {col.name}, atk={attackDamage}");

            col.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
            return;
        }

        CombatActor targetActor = FindCombatActor(col);

        if (targetActor != null && targetActor.faction != owner.faction)
        {
            // 同一攻擊窗口內，已打過就不再重複扣血
            if (hitTargets.Contains(targetActor))
            {
                if (logRepeatBlocked)
                    Debug.Log($"[WeaponHitbox] repeat blocked on {targetActor.name}");
                return;
            }

            hitTargets.Add(targetActor);

            AttackData attack = new AttackData
            {
                attacker = owner.gameObject,
                attackerFaction = owner.faction,
                damage = attackDamage + weaponDamage,
                position = owner.transform.position,
                type = type,
                strength = strength
            };

            if (logHit)
                Debug.Log($"[WeaponHitbox] {name} hit {col.name}, tag={col.tag}, deal {attack.damage} to {targetActor.name}");

            targetActor.TakeDamage(attack);
            return;
        }

        if (logInvalidHit)
            Debug.Log($"[WeaponHitbox] no valid target found on {col.name}, tag={col.tag}");
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
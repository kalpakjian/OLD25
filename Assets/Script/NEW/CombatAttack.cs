using UnityEngine;

/// <summary>
/// 共用攻擊 StateMachineBehaviour。
/// 取代原本的 PlayerAttack 和 EnemyAttack，掛在 Animator State 上即可。
/// 無論是 PlayerController 或 EnemyBase，都能透過 CombatActor 取得 power。
/// </summary>
public class CombatAttack : StateMachineBehaviour
{
    public float damage;
    public AttackType type = AttackType.normal;
    public int strength = 1;
    public float start = 0f;
    public float end = 1f;

    CombatActor owner;
    WeaponHitbox weapon;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 找擁有者（PlayerController / EnemyBase 都是 CombatActor）
        owner = animator.GetComponent<CombatActor>();
        if (owner == null) owner = animator.GetComponentInParent<CombatActor>();
        if (owner == null && animator.transform.root != null)
            owner = animator.transform.root.GetComponentInChildren<CombatActor>(true);

        // 找武器（直接搜尋 WeaponHitbox，玩家和敵人都適用）
        weapon = animator.GetComponentInChildren<WeaponHitbox>(true);
        if (weapon == null && animator.transform.root != null)
            weapon = animator.transform.root.GetComponentInChildren<WeaponHitbox>(true);

        if (weapon != null)
        {
            weapon.type = type;
            weapon.strength = strength;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (owner == null || weapon == null) return;

        if (stateInfo.normalizedTime > start && stateInfo.normalizedTime < end)
            weapon.attackDamage = damage * owner.power;
        else
            weapon.attackDamage = 0f;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon != null)
            weapon.attackDamage = 0f;
    }
}

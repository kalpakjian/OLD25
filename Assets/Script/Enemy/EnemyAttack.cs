using UnityEngine;

public class EnemyAttack : StateMachineBehaviour
{
    public float damage;
    public AttackType type = AttackType.normal;
    public int strength = 1;
    public float start = 0f;
    public float end = 1f;

    // 新版 EnemyBase
    EnemyBase enemyBase;
    // 舊版 Enemy fallback
    Enemy enemyLegacy;

    EnemyWeapon weapon;

    float OwnerPower => enemyBase != null ? enemyBase.power :
                        (enemyLegacy != null ? enemyLegacy.power : 1f);

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 找新版 EnemyBase
        enemyBase = animator.GetComponent<EnemyBase>();
        if (enemyBase == null) enemyBase = animator.GetComponentInParent<EnemyBase>();
        if (enemyBase == null && animator.transform.root != null)
            enemyBase = animator.transform.root.GetComponentInChildren<EnemyBase>(true);

        // 找舊版 Enemy fallback
        if (enemyBase == null)
        {
            enemyLegacy = animator.GetComponent<Enemy>();
            if (enemyLegacy == null) enemyLegacy = animator.GetComponentInParent<Enemy>();
            if (enemyLegacy == null && animator.transform.root != null)
                enemyLegacy = animator.transform.root.GetComponentInChildren<Enemy>(true);
        }

        // 找 EnemyWeapon
        weapon = animator.GetComponentInChildren<EnemyWeapon>(true);
        if (weapon == null && animator.transform.root != null)
            weapon = animator.transform.root.GetComponentInChildren<EnemyWeapon>(true);

        Debug.Log($"[EnemyAttack] enter, enemyBase={(enemyBase ? enemyBase.name : "NULL")}, " +
                  $"enemyLegacy={(enemyLegacy ? enemyLegacy.name : "NULL")}, " +
                  $"weapon={(weapon ? weapon.name : "NULL")}");

        if (weapon != null)
        {
            weapon.type = type;
            weapon.strength = strength;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon == null) return;
        if (enemyBase == null && enemyLegacy == null) return;

        if (stateInfo.normalizedTime > start && stateInfo.normalizedTime < end)
            weapon.attackDamage = damage * OwnerPower;
        else
            weapon.attackDamage = 0f;

        Debug.Log($"[EnemyAttack] time={stateInfo.normalizedTime:F2}, atk={weapon.attackDamage}");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon != null)
            weapon.attackDamage = 0f;
    }
}

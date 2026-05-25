using UnityEngine;

public class EnemyAttack : StateMachineBehaviour
{
    public float damage;
    public AttackType type = AttackType.normal;
    public int strength = 1;
    public float start = 0;
    public float end = 1;

    Enemy enemy;
    EnemyWeapon weapon;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemy = animator.GetComponent<Enemy>();
        weapon = animator.GetComponentInChildren<EnemyWeapon>();

        if (weapon != null)
        {
            weapon.type = type;
            weapon.strength = strength;
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime > start && stateInfo.normalizedTime < end)
            weapon.attackDamage = damage * enemy.power;
        else
            weapon.attackDamage = 0;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        weapon.attackDamage = 0;
    }
}

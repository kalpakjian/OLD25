using UnityEngine;

public class PlayerAttack : StateMachineBehaviour
{
    public float damage;
    public AttackType type = AttackType.normal;
    public int strength = 1;
    public float start = 0f;
    public float end = 1f;

    PlayerController player;
    PlayerWeapon weapon;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = animator.GetComponent<PlayerController>();
        if (player == null) player = animator.GetComponentInParent<PlayerController>();
        if (player == null && animator.transform.root != null)
            player = animator.transform.root.GetComponentInChildren<PlayerController>(true);

        weapon = animator.GetComponentInChildren<PlayerWeapon>(true);
        if (weapon == null && animator.transform.root != null)
            weapon = animator.transform.root.GetComponentInChildren<PlayerWeapon>(true);

        Debug.Log($"[PlayerAttack] enter, player={(player ? player.name : "NULL")}, weapon={(weapon ? weapon.name : "NULL")}");

        if (weapon != null)
        {
            weapon.type = type;
            weapon.strength = strength;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (player == null || weapon == null) return;

        if (stateInfo.normalizedTime > start && stateInfo.normalizedTime < end)
            weapon.attackDamage = damage * player.power;
        else
            weapon.attackDamage = 0f;

        Debug.Log($"[PlayerAttack] time={stateInfo.normalizedTime:F2}, atk={weapon.attackDamage}");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (weapon != null)
            weapon.attackDamage = 0f;
    }
}

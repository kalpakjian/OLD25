using UnityEngine;

public class EnemyRotate : StateMachineBehaviour
{
    EnemyBase enemyBase;
    Transform player;

    bool HasOwner => enemyBase != null;

    Transform OwnerTransform => enemyBase != null ? enemyBase.transform : null;

    bool AllowRotate
    {
        get
        {
            if (enemyBase != null) return enemyBase.allowRotate;
            return true;
        }
        set
        {
            if (enemyBase != null) enemyBase.allowRotate = value;
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemyBase = animator.GetComponent<EnemyBase>();
        if (enemyBase == null) enemyBase = animator.GetComponentInParent<EnemyBase>();
        if (enemyBase == null && animator.transform.root != null)
            enemyBase = animator.transform.root.GetComponentInChildren<EnemyBase>(true);

        GameObject playerObj = GameObject.FindWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;

        Debug.Log($"[EnemyRotate] enter, enemyBase={(enemyBase ? enemyBase.name : "NULL")}, player={(player ? player.name : "NULL")}");

        if (!HasOwner) return;
        AllowRotate = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!HasOwner || OwnerTransform == null || player == null)
            return;

        Vector3 lookPos = player.position - OwnerTransform.position;
        lookPos.y = 0f;

        if (lookPos.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(lookPos);
            OwnerTransform.rotation = Quaternion.Slerp(OwnerTransform.rotation, rot, Time.deltaTime * 10f);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!HasOwner) return;
        AllowRotate = true;
    }
}

using UnityEngine;

public class EnemyRotate : StateMachineBehaviour
{
    EnemyBase enemyBase;
    Enemy enemyLegacy;
    Transform player;

    bool HasOwner => enemyBase != null || enemyLegacy != null;

    Transform OwnerTransform =>
        enemyBase != null ? enemyBase.transform :
        enemyLegacy != null ? enemyLegacy.transform :
        null;

    bool AllowRotate
    {
        get
        {
            if (enemyBase != null) return enemyBase.allowRotate;
            if (enemyLegacy != null) return enemyLegacy.AllowRotate;
            return true;
        }
        set
        {
            if (enemyBase != null) enemyBase.allowRotate = value;
            if (enemyLegacy != null) enemyLegacy.AllowRotate = value;
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enemyBase = animator.GetComponent<EnemyBase>();
        if (enemyBase == null) enemyBase = animator.GetComponentInParent<EnemyBase>();
        if (enemyBase == null && animator.transform.root != null)
            enemyBase = animator.transform.root.GetComponentInChildren<EnemyBase>(true);

        if (enemyBase == null)
        {
            enemyLegacy = animator.GetComponent<Enemy>();
            if (enemyLegacy == null) enemyLegacy = animator.GetComponentInParent<Enemy>();
            if (enemyLegacy == null && animator.transform.root != null)
                enemyLegacy = animator.transform.root.GetComponentInChildren<Enemy>(true);
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;

        Debug.Log($"[EnemyRotate] enter, enemyBase={(enemyBase ? enemyBase.name : "NULL")}, enemyLegacy={(enemyLegacy ? enemyLegacy.name : "NULL")}, player={(player ? player.name : "NULL")}");

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
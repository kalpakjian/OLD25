using UnityEngine;

/// <summary>
/// 合併 EnemyRotate 與 RotatableMotion 的通用旋轉 StateMachineBehaviour。
/// 自動偵測掛載對象（PlayerController / EnemyBase），並依 Inspector 參數組合行為。
///
/// 常用設定參考：
///   玩家攻擊狀態：allowRotateOnEnter=false, faceTargetOnEnter=true,  slerpToTarget=false, restoreRotateOnExit=true
///   玩家閃避狀態：allowRotateOnEnter=false, changeDirection=true,    slerpToTarget=false, restoreRotateOnExit=true
///   玩家移動狀態：allowRotateOnEnter=true,  restoreRotateOnExit=false
///   敵人攻擊狀態：allowRotateOnEnter=false, slerpToTarget=true,       restoreRotateOnExit=true
/// </summary>
public class CombatRotate : StateMachineBehaviour
{
    [Header("Enter")]
    [Tooltip("進入狀態時，角色自身旋轉邏輯（allowRotate）是否開啟")]
    public bool allowRotateOnEnter = false;

    [Tooltip("進入狀態時，立即面向目標（玩家→最近存活敵人；敵人→玩家）")]
    public bool faceTargetOnEnter = false;

    [Tooltip("尋找目標的最大距離（0 = 不限制）")]
    public float maxRange = 0f;

    [Tooltip("進入狀態時呼叫 PlayerController.RotateChar()，依觸控拖曳方向旋轉（Roll 動畫用，僅玩家有效）")]
    public bool changeDirection = false;

    [Header("Update")]
    [Tooltip("每幀以 Slerp 方式持續朝向目標旋轉")]
    public bool slerpToTarget = false;

    [Tooltip("Slerp 旋轉速度")]
    public float slerpSpeed = 10f;

    [Header("Exit")]
    [Tooltip("離開狀態時，自動恢復 allowRotate = true")]
    public bool restoreRotateOnExit = true;

    // ── 內部快取 ──────────────────────────────────────────────────────────────
    CombatActor owner;
    Transform ownerTransform;
    Transform target;
    bool isPlayer;
    // ─────────────────────────────────────────────────────────────────────────

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResolveOwner(animator);
        ResolveTarget();

        if (owner != null)
            owner.allowRotate = allowRotateOnEnter;

        if (faceTargetOnEnter && ownerTransform != null && target != null)
        {
            LookAtTarget(ownerTransform, target.position, instantly: true);
        }
        else if (changeDirection && isPlayer)
        {
            var pc = ownerTransform != null ? ownerTransform.GetComponent<PlayerController>() : null;
            if (pc != null) pc.RotateChar();
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!slerpToTarget || ownerTransform == null || target == null)
            return;

        LookAtTarget(ownerTransform, target.position, instantly: false);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (restoreRotateOnExit && owner != null)
            owner.allowRotate = true;
    }

    // ── 輔助方法 ──────────────────────────────────────────────────────────────

    /// <summary>從 Animator 找出掛載的 CombatActor（PlayerController 或 EnemyBase）</summary>
    void ResolveOwner(Animator animator)
    {
        owner = null;
        ownerTransform = null;
        isPlayer = false;

        // 優先偵測 PlayerController
        var player = animator.GetComponent<PlayerController>();
        if (player != null)
        {
            owner = player;
            ownerTransform = player.transform;
            isPlayer = true;
            return;
        }

        // 其次偵測 EnemyBase（相容 Animator 不在根物件的情況）
        EnemyBase enemy = animator.GetComponent<EnemyBase>();
        if (enemy == null) enemy = animator.GetComponentInParent<EnemyBase>();
        if (enemy == null && animator.transform.root != null)
            enemy = animator.transform.root.GetComponentInChildren<EnemyBase>(true);

        if (enemy != null)
        {
            owner = enemy;
            ownerTransform = enemy.transform;
            isPlayer = false;
        }
    }

    /// <summary>依角色身份決定目標</summary>
    void ResolveTarget()
    {
        target = null;
        if (ownerTransform == null) return;

        if (isPlayer)
        {
            // 玩家 → 最近的存活敵人（可選範圍限制）
            target = FindNearestEnemy(ownerTransform, maxRange);
        }
        else
        {
            // 敵人 → 玩家
            GameObject playerObj = GameObject.FindWithTag("Player");
            target = playerObj != null ? playerObj.transform : null;
        }
    }

    void LookAtTarget(Transform self, UnityEngine.Vector3 targetPos, bool instantly)
    {
        UnityEngine.Vector3 dir = targetPos - self.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        UnityEngine.Quaternion rot = UnityEngine.Quaternion.LookRotation(dir);
        if (instantly)
            self.rotation = rot;
        else
            self.rotation = UnityEngine.Quaternion.Slerp(self.rotation, rot, UnityEngine.Time.deltaTime * slerpSpeed);
    }

    /// <summary>找出距 origin 最近的存活敵人。range ≤ 0 代表不限距離。</summary>
    static Transform FindNearestEnemy(Transform origin, float range = 0f)
    {
#if UNITY_2023_1_OR_NEWER
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
#else
        EnemyBase[] enemies = Object.FindObjectsOfType<EnemyBase>();
#endif
        Transform nearest = null;
        float minDist = float.MaxValue;
        bool useRange = range > 0f;

        foreach (var e in enemies)
        {
            if (e.IsDead) continue;
            float dist = UnityEngine.Vector3.Distance(origin.position, e.transform.position);
            if (useRange && dist > range) continue;
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e.transform;
            }
        }
        return nearest;
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 一鍵將所有 Animator Controller 中的 PlayerAttack / EnemyAttack
/// StateMachineBehaviour 替換為 CombatAttack，並保留原有參數值。
/// 執行路徑：Tools / Animator / Migrate Attack Behaviours
/// </summary>
public static class MigrateAttackBehaviours
{
    [MenuItem("Tools/Animator/Migrate Attack Behaviours (PlayerAttack+EnemyAttack → CombatAttack)")]
    public static void Run()
    {
        // 確認使用者已備份
        bool ok = EditorUtility.DisplayDialog(
            "Migrate Attack Behaviours",
            "此操作會把所有 .controller 檔案裡的\n" +
            "  PlayerAttack  →  CombatAttack\n" +
            "  EnemyAttack   →  CombatAttack\n\n" +
            "原始參數（damage / type / strength / start / end）會自動複製。\n\n" +
            "建議先 git commit 備份，確認繼續嗎？",
            "繼續", "取消");

        if (!ok) return;

        // 找出所有 .controller 檔案
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
        int totalReplaced = 0;
        int totalControllers = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            int replaced = MigrateController(controller);
            if (replaced > 0)
            {
                totalReplaced += replaced;
                totalControllers++;
                EditorUtility.SetDirty(controller);
                Debug.Log($"[MigrateAttackBehaviours] {path}：替換了 {replaced} 個 Behaviour");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "完成",
            $"共更新 {totalControllers} 個 Controller，替換 {totalReplaced} 個 Behaviour。\n\n" +
            "確認無誤後，可手動刪除\n" +
            "  Assets/Script/Player/PlayerAttack.cs\n" +
            "  Assets/Script/Enemy/EnemyAttack.cs",
            "OK");

        Debug.Log($"[MigrateAttackBehaviours] 完成。共替換 {totalReplaced} 個 Behaviour，影響 {totalControllers} 個 Controller。");
    }

    // ──────────────────────────────────────────────
    // 內部方法
    // ──────────────────────────────────────────────

    static int MigrateController(AnimatorController controller)
    {
        int count = 0;
        foreach (var layer in controller.layers)
            count += MigrateStateMachine(controller, layer.stateMachine);
        return count;
    }

    static int MigrateStateMachine(AnimatorController controller, AnimatorStateMachine sm)
    {
        int count = 0;

        // 處理此層的所有 State
        foreach (var childState in sm.states)
            count += MigrateState(controller, childState.state);

        // 遞迴處理子 StateMachine
        foreach (var childSM in sm.stateMachines)
            count += MigrateStateMachine(controller, childSM.stateMachine);

        return count;
    }

    static int MigrateState(AnimatorController controller, AnimatorState state)
    {
        int count = 0;
        var behaviours = state.behaviours;

        // 收集需要替換的舊 behaviour 及其參數
        var toReplace = new List<CombatAttack>();

        foreach (var b in behaviours)
        {
            // PlayerAttack 和 EnemyAttack 都繼承 CombatAttack，直接 cast 即可讀取參數
            if (b is CombatAttack ca && b.GetType() != typeof(CombatAttack))
            {
                toReplace.Add(ca);
            }
        }

        foreach (var old in toReplace)
        {
            // 先備份參數
            float  damage   = old.damage;
            var    type     = old.type;
            int    strength = old.strength;
            float  start    = old.start;
            float  end      = old.end;

            // 刪除舊 behaviour
            // Unity 的 StateMachine Behaviour 需透過 AnimatorController API 移除
            // 直接操作 state.behaviours array（移除特定實例）
            RemoveBehaviour(state, old);

            // 加入新的 CombatAttack（基底類別）
            var newBehaviour = state.AddStateMachineBehaviour(typeof(CombatAttack)) as CombatAttack;
            if (newBehaviour != null)
            {
                newBehaviour.damage   = damage;
                newBehaviour.type     = type;
                newBehaviour.strength = strength;
                newBehaviour.start    = start;
                newBehaviour.end      = end;
                EditorUtility.SetDirty(newBehaviour);
            }

            count++;
            Debug.Log($"  [{state.name}] {old.GetType().Name} → CombatAttack  " +
                      $"(damage={damage}, type={type}, strength={strength}, start={start:F2}, end={end:F2})");
        }

        return count;
    }

    /// <summary>
    /// 從 AnimatorState 移除指定的 StateMachineBehaviour。
    /// 直接透過 state.behaviours setter 重建陣列，再銷毀 sub-asset。
    /// </summary>
    static void RemoveBehaviour(AnimatorState state, StateMachineBehaviour target)
    {
        // 複製現有陣列，移除目標後重新賦值
        var list = new List<StateMachineBehaviour>(state.behaviours);
        list.Remove(target);
        state.behaviours = list.ToArray();

        // 將 sub-asset 本身從 .controller 中銷毀
        Object.DestroyImmediate(target, true);
    }
}

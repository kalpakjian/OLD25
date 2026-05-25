using System.Text;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorControllerExporter
{
    [MenuItem("Tools/Animator/Export Selected Controller To Text")]
    public static void ExportSelectedController()
    {
        var controller = Selection.activeObject as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("請先在 Project 視窗選一個 Animator Controller (.controller)。");
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("========================================");
        sb.AppendLine("Animator Controller Export");
        sb.AppendLine("========================================");
        sb.AppendLine($"Name: {controller.name}");
        sb.AppendLine($"Path: {AssetDatabase.GetAssetPath(controller)}");
        sb.AppendLine();

        AppendParameters(sb, controller);

        for (int i = 0; i < controller.layers.Length; i++)
        {
            var layer = controller.layers[i];
            sb.AppendLine("========================================");
            sb.AppendLine($"Layer {i}: {layer.name}");
            sb.AppendLine("========================================");
            AppendStateMachine(sb, layer.stateMachine, 0);
            sb.AppendLine();
        }

        string text = sb.ToString();
        Debug.Log(text);

        string controllerPath = AssetDatabase.GetAssetPath(controller);
        string folder = Path.GetDirectoryName(controllerPath);
        string fileName = controller.name + "_animator_dump.txt";
        string outputPath = Path.Combine(folder ?? "Assets", fileName);

        File.WriteAllText(outputPath, text, Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"Animator 匯出完成：{outputPath}");
    }

    static void AppendParameters(StringBuilder sb, AnimatorController controller)
    {
        sb.AppendLine("Parameters");
        sb.AppendLine("----------------------------------------");

        if (controller.parameters == null || controller.parameters.Length == 0)
        {
            sb.AppendLine("(none)");
            sb.AppendLine();
            return;
        }

        foreach (var p in controller.parameters)
        {
            string defaultValue = p.type switch
            {
                AnimatorControllerParameterType.Bool => p.defaultBool.ToString(),
                AnimatorControllerParameterType.Float => p.defaultFloat.ToString("0.###"),
                AnimatorControllerParameterType.Int => p.defaultInt.ToString(),
                AnimatorControllerParameterType.Trigger => "(trigger)",
                _ => ""
            };

            sb.AppendLine($"- {p.name} : {p.type}  default={defaultValue}");
        }

        sb.AppendLine();
    }

    static void AppendStateMachine(StringBuilder sb, AnimatorStateMachine sm, int indent)
    {
        string pad = new string(' ', indent * 2);

        sb.AppendLine($"{pad}StateMachine: {sm.name}");

        if (sm.defaultState != null)
            sb.AppendLine($"{pad}  DefaultState: {sm.defaultState.name}");

        if (sm.anyStateTransitions != null && sm.anyStateTransitions.Length > 0)
        {
            sb.AppendLine($"{pad}  Any State Transitions:");
            foreach (var t in sm.anyStateTransitions)
            {
                AppendTransition(sb, t, $"{pad}    ", "AnyState");
            }
        }

        if (sm.entryTransitions != null && sm.entryTransitions.Length > 0)
        {
            sb.AppendLine($"{pad}  Entry Transitions:");
            foreach (var t in sm.entryTransitions)
            {
                AppendTransition(sb, t, $"{pad}    ", "Entry");
            }
        }

        if (sm.states != null && sm.states.Length > 0)
        {
            sb.AppendLine($"{pad}  States:");
            foreach (var child in sm.states)
            {
                var state = child.state;
                sb.AppendLine($"{pad}    - {state.name}");

                if (state.motion != null)
                    sb.AppendLine($"{pad}      Motion: {state.motion.name}");

                sb.AppendLine($"{pad}      Speed: {state.speed}");
                sb.AppendLine($"{pad}      WriteDefaultValues: {state.writeDefaultValues}");

                if (state.behaviours != null && state.behaviours.Length > 0)
                {
                    sb.AppendLine($"{pad}      Behaviours:");
                    foreach (var b in state.behaviours)
                    {
                        if (b != null)
                            sb.AppendLine($"{pad}        - {b.GetType().Name}");
                    }
                }

                if (state.transitions != null && state.transitions.Length > 0)
                {
                    sb.AppendLine($"{pad}      Transitions:");
                    foreach (var t in state.transitions)
                    {
                        AppendTransition(sb, t, $"{pad}        ", state.name);
                    }
                }
            }
        }

        if (sm.stateMachines != null && sm.stateMachines.Length > 0)
        {
            sb.AppendLine($"{pad}  Sub StateMachines:");
            foreach (var child in sm.stateMachines)
            {
                AppendStateMachine(sb, child.stateMachine, indent + 2);
            }
        }
    }

    // 用於 state.transitions / anyStateTransitions（AnimatorStateTransition，含 Exit Time 等時間屬性）
    static void AppendTransition(StringBuilder sb, AnimatorStateTransition t, string pad, string fromName)
    {
        string toName = "(Exit)";

        if (t.destinationState != null)
            toName = t.destinationState.name;
        else if (t.destinationStateMachine != null)
            toName = $"StateMachine:{t.destinationStateMachine.name}";

        sb.AppendLine($"{pad}- {fromName} -> {toName}");
        sb.AppendLine($"{pad}  HasExitTime: {t.hasExitTime}");
        sb.AppendLine($"{pad}  ExitTime: {t.exitTime:0.###}");
        sb.AppendLine($"{pad}  Duration: {t.duration:0.###}");
        sb.AppendLine($"{pad}  Offset: {t.offset:0.###}");
        sb.AppendLine($"{pad}  InterruptionSource: {t.interruptionSource}");
        sb.AppendLine($"{pad}  OrderedInterruption: {t.orderedInterruption}");
        sb.AppendLine($"{pad}  CanTransitionToSelf: {t.canTransitionToSelf}");

        AppendConditions(sb, t.conditions, pad);
    }

    // 用於 sm.entryTransitions（AnimatorTransition，Entry/Exit 過渡，無時間屬性）
    static void AppendTransition(StringBuilder sb, AnimatorTransition t, string pad, string fromName)
    {
        string toName = t.isExit ? "(Exit)" : "(unknown)";

        if (t.destinationState != null)
            toName = t.destinationState.name;
        else if (t.destinationStateMachine != null)
            toName = $"StateMachine:{t.destinationStateMachine.name}";

        sb.AppendLine($"{pad}- {fromName} -> {toName}");

        AppendConditions(sb, t.conditions, pad);
    }

    static void AppendConditions(StringBuilder sb, AnimatorCondition[] conditions, string pad)
    {
        if (conditions != null && conditions.Length > 0)
        {
            sb.AppendLine($"{pad}  Conditions:");
            foreach (var c in conditions)
            {
                sb.AppendLine($"{pad}    - {c.parameter} {c.mode} {c.threshold}");
            }
        }
        else
        {
            sb.AppendLine($"{pad}  Conditions: (none)");
        }
    }
}
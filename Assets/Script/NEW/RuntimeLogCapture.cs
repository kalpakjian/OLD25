using System;
using System.IO;
using UnityEngine;

public class RuntimeLogCapture : MonoBehaviour
{
    [SerializeField] private bool captureStackTrace = false;
    [SerializeField] private bool filterCombatOnly = true;

    private string logFilePath;
    private StreamWriter writer;

    private void Awake()
    {
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string folder = Path.Combine(projectRoot, "Logs", "combat_logs");
#else
        string folder = Path.Combine(Application.persistentDataPath, "combat_logs");
#endif
        Directory.CreateDirectory(folder);

        string fileName = $"combat_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        logFilePath = Path.Combine(folder, fileName);

        writer = new StreamWriter(logFilePath, true);
        writer.AutoFlush = true;

        Application.logMessageReceived += HandleLog;

        Debug.Log($"[RuntimeLogCapture] writing combat logs to: {logFilePath}");
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (writer == null) return;

        if (filterCombatOnly)
        {
            if (!condition.Contains("[WeaponHitbox]") &&
                !condition.Contains("[CombatAttack]"))
            {
                return;
            }
        }

        string time = DateTime.Now.ToString("HH:mm:ss.fff");
        writer.WriteLine($"[{time}] [{type}] {condition}");

        if (captureStackTrace && !string.IsNullOrWhiteSpace(stackTrace))
        {
            writer.WriteLine(stackTrace);
        }
    }
}
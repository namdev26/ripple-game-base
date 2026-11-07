using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[Serializable]
internal sealed class BuildReportsHubPage : ToolHubPage
{
    [NonSerialized] private BuildReport report;
    [SerializeField] private Vector2 scroll;

    internal override void Initialize(Action repaint) => report = BuildReport.GetLatestReport();

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            if (GUILayout.Button("Refresh Latest Build", GUILayout.Height(28))) report = BuildReport.GetLatestReport();
            GUILayout.Space(14);
            if (report == null)
                EditorGUILayout.HelpBox("No build report is available yet. Build the project, then refresh this page.", MessageType.Info);
            else
            {
                var summary = report.summary;
                EditorGUILayout.LabelField("Latest build", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Result", summary.result.ToString());
                EditorGUILayout.LabelField("Platform", summary.platform.ToString());
                EditorGUILayout.LabelField("Total size", EditorUtility.FormatBytes((long)summary.totalSize));
                EditorGUILayout.LabelField("Duration", summary.totalTime.ToString());
                EditorGUILayout.LabelField("Errors / Warnings", summary.totalErrors + " / " + summary.totalWarnings);
                EditorGUILayout.LabelField("Output", summary.outputPath, EditorStyles.wordWrappedLabel);
            }
            GUILayout.Space(20);
            if (ToolHubCommands.HasType("BuildReportTool.ReportGenerator"))
            {
                if (GUILayout.Button("Open Detailed Build Report")) ToolHubCommands.Run("BuildReportTool.ReportGenerator", "ShowBuildReport");
            }
        }
    }
}

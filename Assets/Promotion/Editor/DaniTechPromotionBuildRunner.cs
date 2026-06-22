using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DaniTechPromotionBuildRunner
{
    private const string ScenePath = "Assets/Promotion/Scenes/PromotionMain.unity";
    private const string BuildPath = "Builds/Windows/3D_Promotion.exe";

    // Windows 제출용 실행 파일을 배치모드에서 생성합니다.
    public static void BuildWindowsPlayer()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Windows 빌드 실패: {summary.result}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }

            return;
        }

        Debug.Log($"Windows 빌드 성공: {BuildPath}, Size={summary.totalSize}");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}

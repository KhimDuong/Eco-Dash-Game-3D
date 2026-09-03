using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildScript
{
    [MenuItem("Eco-Dash/Build Windows Executable")]
    public static void BuildWindows()
    {
        string buildPath = "Builds/Windows/EcoDash3D.exe";
        string buildDir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string[] scenes = new string[]
        {
            "Assets/_Scenes/MainMenu.unity",
            "Assets/_Scenes/Intro_Story.unity",
            "Assets/_Scenes/Level1_BarrenFarm.unity",
            "Assets/_Scenes/Shop_RecyclingStation.unity",
            "Assets/_Scenes/Level2_FactoryMaze.unity",
            "Assets/_Scenes/Ending_Story.unity"
        };

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("Starting Eco-Dash 3D Windows Build...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize / (1024 * 1024)} MB at {buildPath}");
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"Build failed with {summary.totalErrors} errors.");
        }
    }
}

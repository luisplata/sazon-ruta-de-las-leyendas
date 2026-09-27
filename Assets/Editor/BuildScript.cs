using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CLI-ready build entry points. Usage:
///   Unity -batchmode -quit -projectPath <proj> -executeMethod BuildScript.BuildWindows
///   Unity -batchmode -quit -projectPath <proj> -executeMethod BuildScript.BuildAndroid
/// Append "-devBuild" for a Development build (e.g. -executeMethod BuildScript.BuildWindows -devBuild).
/// Scenes come from EditorBuildSettings (Prototype only).
/// </summary>
public static class BuildScript
{
    private const string WindowsDir = "Builds/Windows";
    private const string AndroidDir = "Builds/Android";

    public static void BuildWindows()
    {
        BuildPlayer(BuildTarget.StandaloneWindows64, WindowsDir, "Sazon-Ruta-de-Las-Leyendas.exe");
    }

    public static void BuildAndroid()
    {
        BuildPlayer(BuildTarget.Android, AndroidDir, "Sazon-Ruta-de-Las-Leyendas.apk");
    }

    private static void BuildPlayer(BuildTarget target, string outputDir, string fileName)
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in EditorBuildSettings.");
        }

        Directory.CreateDirectory(outputDir);
        var options = BuildOptions.None;
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-devBuild") >= 0)
        {
            options |= BuildOptions.Development;
        }

        var report = BuildPipeline.BuildPlayer(
            scenes,
            Path.Combine(outputDir, fileName),
            target,
            options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Build failed for {target}: {report.summary.result} ({report.summary.totalErrors} errors).");
        }

        Debug.Log($"Build succeeded: {report.summary.outputPath}");
    }
}
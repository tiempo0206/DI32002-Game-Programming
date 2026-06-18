using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SplatFightersWebGLBuild
{
    private const string DefaultBuildPath = "Builds/WebGL";

    [MenuItem("Splat Fighters/Build/WebGL for itch.io")]
    public static void BuildForItch()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new BuildFailedException("No enabled scenes were found in Editor Build Settings.");
        }

        string outputPath = ResolveOutputPath();
        PrepareOutputDirectory(outputPath);
        ConfigureWebGLForItch();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"Building Splat Fighters WebGL release to {outputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"WebGL build failed with result {report.summary.result} and " +
                $"{report.summary.totalErrors} error(s). See the Editor log for details.");
        }

        string indexPath = Path.Combine(outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            throw new BuildFailedException($"WebGL build completed without an index.html at {indexPath}.");
        }

        Debug.Log(
            $"Splat Fighters WebGL build succeeded: {report.summary.totalSize} bytes, " +
            $"{report.summary.totalWarnings} warning(s), output {outputPath}");
    }

    private static void ConfigureWebGLForItch()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.threadsSupport = false;
    }

    private static string ResolveOutputPath()
    {
        string requestedPath = GetCommandLineValue("-buildPath") ?? DefaultBuildPath;
        if (Path.IsPathRooted(requestedPath))
        {
            return Path.GetFullPath(requestedPath);
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new BuildFailedException("Unable to resolve the Unity project root.");
        }

        return Path.GetFullPath(Path.Combine(projectRoot, requestedPath));
    }

    private static string GetCommandLineValue(string key)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], key, StringComparison.Ordinal))
            {
                return arguments[index + 1];
            }
        }

        return null;
    }

    private static void PrepareOutputDirectory(string outputPath)
    {
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        Directory.CreateDirectory(outputPath);
    }
}

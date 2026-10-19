using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// "BioValue/Build Web (WebGL)": builds the game for the browser into Builds/WebGL, ready to upload
// (itch.io, Netlify, GitHub Pages, any web server). Uses the scenes ticked in Build Settings.
// The first build switches the project to WebGL, which re-imports assets and takes a while.
public static class WebBuild
{
    private const string OutputFolder = "Builds/WebGL";

    [MenuItem("BioValue/Build Web (WebGL)")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Web", "Stop Play mode first.", "OK");
            return;
        }
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Build Web", "No scenes are ticked in Build Settings.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Build Web",
                "Build the game for the browser into " + OutputFolder + "?\n\n" +
                "The first time, Unity switches the project to WebGL and re-imports assets: this can take several minutes.\n\n" +
                "Scenes: " + string.Join(", ", scenes.Select(Path.GetFileNameWithoutExtension)),
                "Build", "Cancel"))
            return;

        // Works on any host: gzip files, and the page unpacks them itself when the server doesn't.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;     // faster second visits
        PlayerSettings.runInBackground = true;       // keep Photon connected when the tab isn't focused
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputFolder,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[WebBuild] Done: {Path.GetFullPath(OutputFolder)} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}).");
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, "index.html"));
        }
        else
        {
            Debug.LogError($"[WebBuild] {summary.result}: {summary.totalErrors} error(s). See the Console.");
        }
    }
}

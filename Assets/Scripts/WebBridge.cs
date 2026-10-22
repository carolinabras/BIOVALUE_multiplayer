using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// What differs in the web build: the clipboard and saving files go through the browser
// (Assets/Plugins/WebGL/BioValueWeb.jslib), and "Quit" shows a goodbye screen (a tab can't close itself).
public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BioValue_CopyText(string text);
    [DllImport("__Internal")] private static extern void BioValue_DownloadText(string fileName, string text);
    [DllImport("__Internal")] private static extern void BioValue_Quit();
    [DllImport("__Internal")] private static extern string BioValue_TabId();
    public const bool IsWeb = true;
#else
    public const bool IsWeb = false;
#endif

    public static void CopyText(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BioValue_CopyText(text ?? "");
#else
        GUIUtility.systemCopyBuffer = text ?? "";
#endif
    }

    // Web: the browser downloads it. Elsewhere: written to persistentDataPath. Returns where it went.
    public static string SaveText(string fileName, string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BioValue_DownloadText(fileName, text ?? "");
        return "your downloads (" + fileName + ")";
#else
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, text ?? "", Encoding.UTF8);
        return path;
#endif
    }

    // Web: an id kept by this browser tab (survives a reload). Elsewhere: null (a new id each launch).
    public static string TabId()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return BioValue_TabId();
#else
        return null;
#endif
    }

    public static void Quit()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BioValue_Quit();
#else
        Application.Quit();
#endif
    }
}

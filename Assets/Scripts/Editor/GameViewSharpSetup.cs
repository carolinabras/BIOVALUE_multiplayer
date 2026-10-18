using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Makes the Game view show the game at full sharpness: "Low Resolution Aspect Ratios" off and the
// zoom reset so the picture is never stretched above 1x. The chosen resolution is kept, so any
// resolution can still be tested (the UI adapts through the Canvas Scalers).
// Runs once by itself; run it again any time from "BioValue > Sharp Game View".
// Unity keeps these settings in its internal GameView class, so this goes through reflection.
[InitializeOnLoad]
public static class GameViewSharpSetup
{
    private const string DonePref = "BioValue.GameViewSharpSetup.Done";

    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static GameViewSharpSetup()
    {
        if (EditorPrefs.GetBool(DonePref, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (Apply()) EditorPrefs.SetBool(DonePref, true);
        };
    }

    [MenuItem("BioValue/Sharp Game View")]
    private static void ApplyFromMenu()
    {
        if (Apply()) Debug.Log("[GameViewSharpSetup] Game view set to full resolution.");
        else Debug.LogWarning("[GameViewSharpSetup] Couldn't change the Game view. Set it by hand: resolution dropdown > untick 'Low Resolution Aspect Ratios' and set Scale to 1x or lower.");
    }

    private static bool Apply()
    {
        try
        {
            var asm = typeof(EditorWindow).Assembly;
            var gameViewType = asm.GetType("UnityEditor.GameView");
            if (gameViewType == null) return false;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows.Length == 0) return false; // no Game view open yet; try again next time

            foreach (var w in windows)
            {
                var lowRes = gameViewType.GetProperty("lowResolutionForAspectRatios", Any);
                if (lowRes != null && lowRes.CanWrite) lowRes.SetValue(w, false);

                // Smallest zoom that still fits the window (never stretched above 1x).
                var fit = gameViewType.GetMethod("SnapZoom", Any, null, new[] { typeof(float) }, null);
                var minScale = gameViewType.GetProperty("minScale", Any);
                if (fit != null && minScale != null)
                    fit.Invoke(w, new object[] { Mathf.Min(1f, (float)minScale.GetValue(w)) });

                ((EditorWindow)w).Repaint();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameViewSharpSetup] " + e.Message);
            return false;
        }
    }
}

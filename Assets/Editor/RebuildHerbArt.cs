using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor auto-hook: Ensures ArtPass runs to bake exact, valid 3D model references
/// for Herb.prefab using ArtKit's URP material converter.
/// </summary>
[InitializeOnLoad]
public static class RebuildHerbArt
{
    static RebuildHerbArt()
    {
        EditorApplication.delayCall += () =>
        {
            ArtPass.Run();
        };
    }
}

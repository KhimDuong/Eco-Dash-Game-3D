using System;
using UnityEngine;

/// <summary>
/// Which track each scene plays. Lives at <c>Assets/Resources/MusicKit.asset</c> so
/// <see cref="MusicPlayer"/> can load it from any entry scene without a single reference
/// wired into a scene — which is the point, because three of the six scenes are generated
/// and would lose anything wired into them on the next rebuild.
///
/// <para>The asset is written by <c>Eco-Dash → Run the audio pass (C5)</c>, so the clip
/// assignments live in <c>AudioPass.cs</c> alongside every other one. Edit it there.</para>
/// </summary>
[CreateAssetMenu(fileName = "MusicKit", menuName = "Eco-Dash/Music Kit")]
public class MusicKit : ScriptableObject
{
    [Serializable]
    public struct SceneTrack
    {
        public string sceneName;
        public AudioClip track;
    }

    [Tooltip("Played in any scene without an entry below. Null means silence.")]
    public AudioClip defaultTrack;

    [Tooltip("Per-scene overrides. An entry with no clip means that scene is deliberately silent.")]
    public SceneTrack[] perScene = Array.Empty<SceneTrack>();

    [Tooltip("Authored reference volume, before the Music slider scales it.")]
    [Range(0f, 1f)] public float volume = 0.5f;

    [Tooltip("Seconds to fade out the old track and in the new one when a scene changes it.")]
    [Range(0f, 3f)] public float fadeSeconds = 0.5f;

    /// <summary>The track <paramref name="sceneName"/> wants, or null for silence.</summary>
    public AudioClip For(string sceneName)
    {
        if (perScene != null)
            foreach (var entry in perScene)
                if (entry.sceneName == sceneName) return entry.track;
        return defaultTrack;
    }

    /// <summary>True if this scene is listed at all — a listed scene with no clip means silence.</summary>
    public bool Mentions(string sceneName)
    {
        if (perScene == null) return false;
        foreach (var entry in perScene)
            if (entry.sceneName == sceneName) return true;
        return false;
    }
}

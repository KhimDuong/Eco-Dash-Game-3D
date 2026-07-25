using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-scene run progress (M9 save rework): remembers which placed objects have been
/// "consumed" (cores collected, enemies killed, pickups taken) and the objective
/// count per scene, so revisiting a stage — within a session (hub ↔ stage travel) or
/// via <b>Continue</b> — keeps that progress instead of resetting the level. Also
/// tracks the last gameplay scene the player was in, for the menu's Continue.
///
/// <para>Static, JSON-persisted via <see cref="SaveSystem"/> like the other M9 stores;
/// wiped by <see cref="SaveSystem.ResetNewGame"/>. Objects identify themselves by name
/// + rounded position (stable across reloads for placed objects).</para>
/// </summary>
public static class SceneProgress
{
    const string SaveKey = "eco_sceneprogress";

    // Scenes whose progress we persist / that count as "where the player is".
    static readonly HashSet<string> GameplayScenes = new HashSet<string>
    {
        "Level1_BarrenFarm", "Level2_FactoryMaze", "Shop_RecyclingStation"
    };

    [Serializable] class SceneRec { public string scene; public List<string> consumed = new List<string>(); public int cores; }
    [Serializable] class SaveData { public List<SceneRec> scenes = new List<SceneRec>(); public string lastScene = ""; }

    static SaveData data;

    // Record the last gameplay scene entered (for Continue), no per-scene wiring needed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => SceneManager.sceneLoaded += OnSceneLoaded;

    static void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (!GameplayScenes.Contains(s.name)) return;
        EnsureLoaded();
        if (data.lastScene != s.name) { data.lastScene = s.name; Save(); }
    }

    public static bool HasSave { get { EnsureLoaded(); return !string.IsNullOrEmpty(data.lastScene); } }
    public static string LastScene { get { EnsureLoaded(); return data.lastScene; } }

    // --- Consumed objects ---------------------------------------------------

    /// <summary>Stable id for a placed object: name + rounded world position.</summary>
    public static string IdFor(GameObject go)
    {
        var p = go.transform.position;
        return $"{go.name}@{Mathf.RoundToInt(p.x * 100f)},{Mathf.RoundToInt(p.y * 100f)}";
    }

    public static bool IsConsumed(GameObject go) =>
        IsConsumed(SceneManager.GetActiveScene().name, IdFor(go));

    public static void Consume(GameObject go) =>
        MarkConsumed(SceneManager.GetActiveScene().name, IdFor(go));

    public static bool IsConsumed(string scene, string id)
    {
        EnsureLoaded();
        var rec = Find(scene);
        return rec != null && rec.consumed.Contains(id);
    }

    public static void MarkConsumed(string scene, string id)
    {
        EnsureLoaded();
        var rec = GetOrCreate(scene);
        if (rec.consumed.Contains(id)) return;
        rec.consumed.Add(id);
        Save();
    }

    // --- Objective count per scene ------------------------------------------

    public static int GetCores(string scene)
    {
        EnsureLoaded();
        var rec = Find(scene);
        return rec == null ? 0 : rec.cores;
    }

    public static void SetCores(string scene, int count)
    {
        EnsureLoaded();
        GetOrCreate(scene).cores = count;
        Save();
    }

    // --- Reset --------------------------------------------------------------

    public static void ResetAll()
    {
        data = new SaveData();
        Save();
    }

    // --- Internals ----------------------------------------------------------

    static SceneRec Find(string scene) => data.scenes.Find(r => r.scene == scene);

    static SceneRec GetOrCreate(string scene)
    {
        var rec = Find(scene);
        if (rec == null) { rec = new SceneRec { scene = scene }; data.scenes.Add(rec); }
        return rec;
    }

    static void EnsureLoaded()
    {
        if (data == null) data = SaveSystem.LoadJson<SaveData>(SaveKey);
    }

    static void Save() => SaveSystem.SaveJson(SaveKey, data);
}

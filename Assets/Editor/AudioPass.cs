using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// C5: put the right clip in every sound field in the game, and write the
/// <see cref="MusicKit"/> that <see cref="MusicPlayer"/> reads.
///
/// <para><b>Why a generator rather than dragging clips in the Inspector?</b> The same reason
/// <see cref="ArtPass"/> exists. Most of the prefabs that need a clip — every enemy, the whole
/// factory kit, the hub's benches and portals — are rebuilt from primitives by
/// <see cref="EnemyPrefabBuilder"/>, <see cref="FactoryKitBuilder"/> and
/// <see cref="HubBuilder"/>, so a hand-dragged clip lives until the next <i>Rebuild</i> and
/// then vanishes without a word. Those three builders call their section here at the end, so
/// the sound comes back with the art.</para>
///
/// <para><b>The clip list is short on purpose.</b> Eight files came over from the 2D game and
/// no new audio was sourced, so several roles share a clip — that is inherited from the 2D
/// build, where <c>seed_shoot</c> was already both Greenie's gun and the Mega-Smog's attack.
/// Assignments marked <i>2D</i> below reproduce what the 2D project had; the rest are fields
/// the 2D build declared and never filled, so the closest of the eight is used and said out
/// loud rather than left silent.</para>
///
/// <para>Two fields are deliberately left empty, and should stay that way:
/// <c>PlayerHealth.deathSfx</c>, because <see cref="EndScreenController"/> already plays
/// <c>lose_jingle</c> on the same frame Greenie dies and two cues on one frame just smear each
/// other; and <c>HealthPickup</c>/<c>SpeedBoostPickup</c>'s <c>collectSfx</c>, because no
/// prefab in the project uses those two scripts — the 3D build folded both into the generic
/// <c>ItemPickup</c>.</para>
///
/// Menu: <b>Eco-Dash → Run the audio pass (C5)</b>. Idempotent — safe to re-run.
/// </summary>
public static class AudioPass
{
    [MenuItem("Eco-Dash/Run the audio pass (C5)")]
    public static void Run() => Debug.Log(Execute());

    const string Sfx = "Assets/Audio/SFX/";
    const string Music = "Assets/Audio/Music/";
    const string ResourcesDir = "Assets/Resources";
    const string KitPath = ResourcesDir + "/" + MusicPlayer.KitResourcePath + ".asset";

    /// <summary>The single background track the 2D game shipped with, reused in every scene.</summary>
    public const string DefaultTrack = "good_morning";

    static StringBuilder log;
    static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    // A prefab and the (component, field, clip) rows it wants.
    struct Row
    {
        public string Prefab, Component, Field, Clip;
        public Row(string prefab, string component, string field, string clip)
        { Prefab = prefab; Component = component; Field = field; Clip = clip; }
    }

    // --- the table ---------------------------------------------------------------------

    static Row[] PlayerKit => new[]
    {
        new Row("Assets/Prefabs/Player.prefab", "PlayerHealth", "hurtSfx", "player_hurt"),      // 2D
        new Row("Assets/Prefabs/Player.prefab", "PlayerShooter", "shootSfx", "seed_shoot"),     // 2D
        new Row("Assets/Prefabs/HUD.prefab", "EndScreenController", "winSfx", "win_fanfare"),   // 2D
        new Row("Assets/Prefabs/HUD.prefab", "EndScreenController", "loseSfx", "lose_jingle"),  // 2D
        new Row("Assets/Prefabs/HUD.prefab", "DialogueRunner", "blipSfx", "item_pickup"),       // 2D
    };

    static Row[] EnemyRows => new[]
    {
        new Row("Assets/Prefabs/Enemies/PlasticSlime.prefab", "PlasticSlime", "deathSfx", "slime_death"),      // 2D
        new Row("Assets/Prefabs/Enemies/PollutionFlyBot.prefab", "PollutionFlyBot", "deathSfx", "slime_death"),// 2D
        new Row("Assets/Prefabs/Enemies/PollutionFlyBot.prefab", "PollutionFlyBot", "shootSfx", "seed_shoot"), // 2D
        new Row("Assets/Prefabs/Enemies/MegaSmogBoss.prefab", "MegaSmogBoss", "deathSfx", "slime_death"),      // 2D
        new Row("Assets/Prefabs/Enemies/MegaSmogBoss.prefab", "MegaSmogBoss", "attackSfx", "seed_shoot"),      // 2D
        // The King never had one in 2D; he is a bigger slime, so he dies like one (Sfx's pitch
        // scatter keeps the 29 ordinary slimes from sounding like a copy of him).
        new Row("Assets/Prefabs/Enemies/SlimeKing.prefab", "SlimeKing", "deathSfx", "slime_death"),
    };

    static Row[] WorldRows => new[]
    {
        new Row("Assets/Prefabs/Greybox/EnergyCore.prefab", "EnergyCore", "collectSfx", "core_collect"),   // 2D
        new Row("Assets/Prefabs/Greybox/Chest.prefab", "Chest", "openSfx", "item_pickup"),                 // 2D
        new Row("Assets/Prefabs/Greybox/Chest.prefab", "Chest", "coreSfx", "core_collect"),                // 2D
        new Row("Assets/Prefabs/Greybox/Litter.prefab", "Litter", "cleanSfx", "item_pickup"),              // 2D
        new Row("Assets/Prefabs/Greybox/TeleportGate.prefab", "TeleportGate", "activateSfx", "core_collect"), // 2D
        new Row("Assets/Prefabs/Greybox/ItemPickup.prefab", "ItemPickup", "collectSfx", "item_pickup"),    // 2D
        new Row("Assets/Prefabs/Greybox/Herb.prefab", "QuestItemPickup", "pickupSound", "item_pickup"),
        new Row("Assets/Prefabs/Greybox/LoreNote.prefab", "LoreNote", "foundSfx", "item_pickup"),
    };

    static Row[] FactoryRows => new[]
    {
        new Row("Assets/Prefabs/Factory/Keycard.prefab", "Keycard", "grabSfx", "core_collect"),            // 2D
        new Row("Assets/Prefabs/Factory/BossDoor.prefab", "BossDoor", "openSfx", "core_collect"),
        // Nothing in the eight sounds like a hatch dropping open; slime_death is the only one
        // with any weight to it, and a wet clank under the factory floor is close enough.
        new Row("Assets/Prefabs/Factory/ManholeTrap.prefab", "ManholeTrap", "openSfx", "slime_death"),
        new Row("Assets/Prefabs/Factory/SweepingLaser.prefab", "SweepingLaser", "fireSfx", "seed_shoot"),
        new Row("Assets/Prefabs/Factory/ReturnPortal.prefab", "ReturnPortal", "travelSfx", "core_collect"),
    };

    static Row[] HubRows => new[]
    {
        new Row("Assets/Prefabs/Hub/ShopUI.prefab", "ShopController", "openSfx", "item_pickup"),   // 2D
        new Row("Assets/Prefabs/Hub/ShopUI.prefab", "ShopController", "buySfx", "core_collect"),   // 2D
        new Row("Assets/Prefabs/Hub/CraftingBench.prefab", "CraftingBench", "openSfx", "item_pickup"),
        new Row("Assets/Prefabs/Hub/StagePortal.prefab", "StagePortal", "travelSfx", "core_collect"),
        // Powering a stage open is the hub's one payoff moment, so it gets the fanfare.
        new Row("Assets/Prefabs/Hub/StagePortal.prefab", "StagePortal", "powerSfx", "win_fanfare"),
    };

    // --- entry points ------------------------------------------------------------------

    /// <summary>
    /// Re-apply one section. The three prefab builders rebuild their prefabs from primitives,
    /// which drops the clips along with the art, so each calls its own section at the end —
    /// right after it calls <see cref="ArtPass"/> for the same reason.
    /// </summary>
    public static string ReapplyEnemies() => Section(EnemyRows);
    public static string ReapplyFactory() => Section(FactoryRows);
    public static string ReapplyHub() => Section(HubRows);

    public static string Execute()
    {
        log = new StringBuilder();
        clipCache.Clear();
        log.AppendLine("Audio pass (C5):");
        int n = 0;
        n += Apply(PlayerKit);
        n += Apply(EnemyRows);
        n += Apply(WorldRows);
        n += Apply(FactoryRows);
        n += Apply(HubRows);
        BuildMusicKit();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        log.AppendLine($"  {n} field(s) changed.");
        return log.ToString();
    }

    static string Section(Row[] rows)
    {
        log = new StringBuilder();
        clipCache.Clear();
        int n = Apply(rows);
        AssetDatabase.SaveAssets();
        return $"  audio pass: {n} field(s) re-wired.\n" + log;
    }

    // --- the work ----------------------------------------------------------------------

    static int Apply(Row[] rows)
    {
        int changed = 0;
        // Group by prefab so each one is loaded and saved once, however many fields it wants.
        var byPrefab = new Dictionary<string, List<Row>>();
        foreach (var row in rows)
        {
            if (!byPrefab.TryGetValue(row.Prefab, out var list))
                byPrefab[row.Prefab] = list = new List<Row>();
            list.Add(row);
        }

        foreach (var pair in byPrefab)
            changed += WirePrefab(pair.Key, pair.Value);
        return changed;
    }

    static int WirePrefab(string prefabPath, List<Row> rows)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            log.AppendLine($"  AUDIO WARNING missing prefab {prefabPath}");
            return 0;
        }

        int changed = 0;
        try
        {
            foreach (var row in rows)
            {
                var component = FindComponent(root, row.Component);
                if (component == null)
                {
                    log.AppendLine($"  AUDIO WARNING {Name(prefabPath)}: no {row.Component}");
                    continue;
                }
                var clip = Clip(row.Clip);
                if (clip == null)
                {
                    log.AppendLine($"  AUDIO WARNING missing clip {row.Clip}");
                    continue;
                }

                var so = new SerializedObject(component);
                var prop = so.FindProperty(row.Field);
                if (prop == null)
                {
                    log.AppendLine($"  AUDIO WARNING {row.Component}.{row.Field} does not exist");
                    continue;
                }
                if (prop.objectReferenceValue == clip) continue;   // already right — stay idempotent

                prop.objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine($"  {Name(prefabPath)}.{row.Component}.{row.Field} = {row.Clip}");
                changed++;
            }
            if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return changed;
    }

    static Component FindComponent(GameObject root, string typeName)
    {
        foreach (var c in root.GetComponentsInChildren<Component>(true))
            if (c != null && c.GetType().Name == typeName) return c;
        return null;
    }

    static AudioClip Clip(string name)
    {
        if (clipCache.TryGetValue(name, out var cached)) return cached;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + name + ".wav")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + name + ".ogg")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(Music + name + ".ogg")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>(Music + name + ".wav");
        clipCache[name] = clip;
        return clip;
    }

    static string Name(string assetPath)
    {
        int slash = assetPath.LastIndexOf('/');
        return slash < 0 ? assetPath : assetPath.Substring(slash + 1).Replace(".prefab", "");
    }

    // --- music -------------------------------------------------------------------------

    // The kit goes in Resources so MusicPlayer can find it from whichever scene the editor
    // happened to start in, without a reference wired into any of the six scenes — three of
    // which are generated and would drop it on the next rebuild.
    static void BuildMusicKit()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var kit = AssetDatabase.LoadAssetAtPath<MusicKit>(KitPath);
        bool created = kit == null;
        if (created)
        {
            kit = ScriptableObject.CreateInstance<MusicKit>();
            AssetDatabase.CreateAsset(kit, KitPath);
        }

        kit.defaultTrack = Clip(DefaultTrack);
        // 0.5 is the volume the 2D game's per-scene music sources were authored at, kept so the
        // Music slider still means the same thing at the same position.
        kit.volume = 0.5f;
        kit.fadeSeconds = 0.5f;
        // Every scene gets the same track — one is all the 2D project ever had. Per-scene
        // entries go here if a second one is ever sourced.
        kit.perScene = Array.Empty<MusicKit.SceneTrack>();

        EditorUtility.SetDirty(kit);
        log.AppendLine($"  MusicKit {(created ? "created" : "updated")} at {KitPath} " +
                       $"(track = {DefaultTrack}, volume {kit.volume})");
    }
}

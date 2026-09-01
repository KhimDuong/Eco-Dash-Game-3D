using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// B5: replace every greybox mesh with a real low-poly model, in the prefabs themselves so the
/// level builders keep working unchanged and the art survives a scene rebuild.
///
/// <para>The rule throughout: <b>only the visual is replaced.</b> Colliders, trigger radii,
/// scripts, "Nhấn E" prompt canvases and the toggled-child contracts (<c>Visual_Open</c> /
/// <c>Visual_Locked</c>, <c>Lid</c> / <c>Hole</c>, <c>Visual_Unconscious</c> /
/// <c>Visual_Awake</c>) are load-bearing gameplay and are left exactly as they were.</para>
///
/// <para>The one deliberate exception is the loose props (crate, barrel, rock, fence, hut).
/// Those carried their collider on a root scaled to fake the primitive's shape, and the level
/// builder lifted each one by a hand-measured amount because a primitive is centred on its own
/// origin. Real models pivot on the floor, so the pass rebuilds those colliders from the art's
/// measured bounds and <see cref="Level1Builder"/> no longer lifts them.</para>
///
/// Menu: <b>Eco-Dash → Run the art pass</b>. Idempotent — safe to re-run.
/// </summary>
public static class ArtPass
{
    [MenuItem("Eco-Dash/Run the art pass (B5)")]
    public static void Run() => Debug.Log(Execute());

    const string Greybox = "Assets/Prefabs/Greybox/";
    const string Enemies = "Assets/Prefabs/Enemies/";
    const string FactoryP = "Assets/Prefabs/Factory/";
    const string Hub = "Assets/Prefabs/Hub/";

    static StringBuilder log;
    static StringBuilder Log => log ??= new StringBuilder();

    /// <summary>
    /// Re-apply just one section. The prefab builders (<see cref="HubBuilder"/>,
    /// <see cref="FactoryKitBuilder"/>, <see cref="EnemyPrefabBuilder"/>) rebuild their prefabs
    /// from scratch, which throws the art away — so each of them calls its own section here at
    /// the end. Without that, rebuilding the hub silently reverts Ông Bear to a grey capsule.
    /// </summary>
    public static string ReapplyHub() => Section(HubKit);
    public static string ReapplyFactory() => Section(FactoryKit);
    public static string ReapplyEnemies() => Section(EnemyKit);

    static string Section(Action section)
    {
        log = new StringBuilder();
        ArtKit.ClearWarnings();
        section();
        AssetDatabase.SaveAssets();
        foreach (string w in ArtKit.Warnings) Log.AppendLine("  ART WARNING " + w);
        return log.ToString();
    }

    public static string Execute()
    {
        log = new StringBuilder();
        ArtKit.ClearWarnings();

        Player();
        EnemyKit();
        FarmProps();
        VillageKit();
        FarmInteractables();
        Villagers();
        FactoryKit();
        HubKit();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (ArtKit.Warnings.Count > 0)
        {
            Log.AppendLine("--- " + ArtKit.Warnings.Count + " warnings ---");
            foreach (string w in ArtKit.Warnings) Log.AppendLine("  " + w);
        }
        else Log.AppendLine("--- no warnings ---");
        return log.ToString();
    }

    // --- the player -------------------------------------------------------------------------

    static void Player()
    {
        // Greenie is Kenney's "oopi" — a one-eyed mint robot that already looks the part.
        // The Visual child stays: PlayerController turns it to face travel and PlayerAnimator
        // bobs and squashes its localScale, so the model goes *inside* it.
        Edit("Assets/Prefabs/Player.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            Strip(visual);
            Kill(visual.Find("Nose"));
            // The capsule this replaces was centred on the Visual node, not standing on it, so
            // the model is centred too — otherwise Greenie floats a body-height above his feet.
            // oopi is modelled facing +X; PlayerController turns Visual toward the travel
            // direction, which is Unity forward (+Z), so the art is turned to match.
            ArtKit.Spawn(ArtKit.Factory + "oopi.fbx", visual, 1.0f, rotY: 270f, lift: -0.5f);
            Log.AppendLine("Player: Greenie = oopi (1.0 m) centred on Visual, facing +Z");
        });
    }

    // --- enemies ----------------------------------------------------------------------------

    public static void EnemyKit()
    {
        Edit(Enemies + "PlasticSlime.prefab", root =>
        {
            Kill(root.transform.Find("Body"));
            Kill(root.transform.Find("Shard_L"));
            Kill(root.transform.Find("Shard_R"));
            var art = ArtKit.Spawn(ArtKit.Quat + "Slime.glb", root.transform, 0.8f, idle: true);
            if (art != null) ArtKit.MakeFlashable(art);
            Log.AppendLine("PlasticSlime: Quaternius slime (0.8 m), idle looping");
        });

        Edit(Enemies + "PollutionFlyBot.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            for (int i = visual.childCount - 1; i >= 0; i--) Kill(visual.GetChild(i));
            // The bot is placed hovering, so the model is centred on the Visual node rather
            // than grounded — lift by half its height to undo Spawn's floor pivot.
            var art = ArtKit.Spawn(ArtKit.Quat + "RobotEnemyFlyingGun.glb", visual, 0.9f,
                                   rotY: 180f, lift: -0.45f, idle: true);
            if (art != null) ArtKit.MakeFlashable(art);
            Log.AppendLine("PollutionFlyBot: Quaternius flying robot (0.9 m), lights stripped");
        });

        // C3's bosses. The King is deliberately the same mesh as an ordinary slime, twice the
        // size and in sicker colours — the 2D bestiary calls him a giant mutated slime, and
        // reusing the silhouette is what makes "that one is the big one" read instantly.
        Edit(Enemies + "SlimeKing.prefab", root =>
        {
            Kill(root.transform.Find("Body"));
            for (int i = 0; i < 5; i++) Kill(root.transform.Find("Crown_" + i));
            var art = ArtKit.Spawn(ArtKit.Quat + "Slime.glb", root.transform, 1.55f, idle: true,
                                   variant: "King", recolour: SlimeKingPalette);
            if (art != null) ArtKit.MakeFlashable(art);
            Log.AppendLine("SlimeKing: Quaternius slime at boss scale (1.55 m), bruised-plastic recolour");
        });

        // The Mega-Smog is assembled rather than swapped: no single Kenney model is a boss, but
        // a fortified machine flanked by two industrial hoppers is one. Each piece sits in its
        // own Art_ holder so ClearArt takes the whole rig away when the pass re-runs.
        Edit(Enemies + "MegaSmogBoss.prefab", root =>
        {
            Kill(root.transform.Find("Plinth"));
            Kill(root.transform.Find("Body"));
            Kill(root.transform.Find("Stack_L"));
            Kill(root.transform.Find("Stack_R"));

            Rig(root.transform, "Hull", Vector3.zero, ArtKit.Factory + "machine-fortified.fbx", 1.90f);
            Rig(root.transform, "StackL", new Vector3(-1.45f, 0f, 0f),
                ArtKit.Factory + "hopper-high-round.fbx", 2.20f);
            Rig(root.transform, "StackR", new Vector3(1.45f, 0f, 0f),
                ArtKit.Factory + "hopper-high-round.fbx", 2.20f);

            // Core stays a sphere and stays where the builder put it — MegaSmogBoss pulses its
            // localScale, so anything that replaced the node would have to pulse too.
            Paint(root.transform.Find("Core"), ArtKit.SolidMaterial(
                "MegaSmogCore", new Color(0.55f, 0.85f, 0.25f), 0.85f, new Color(0.45f, 1f, 0.20f)));
            Log.AppendLine("MegaSmogBoss: fortified machine + two hoppers, toxic core re-lit");
        });

        // Projectiles stay spheres on purpose — they are 0.3 m and read by their glow, not
        // their silhouette. They get proper emissive materials instead of the greybox ones.
        Edit(Enemies + "SmogOrb.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial(
                "SmogOrb", new Color(0.30f, 0.24f, 0.34f), 0.4f, new Color(0.55f, 0.25f, 0.65f)));
            Log.AppendLine("SmogOrb: emissive smog material");
        });

        Edit("Assets/Prefabs/Seed.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial(
                "Seed", new Color(0.55f, 0.85f, 0.35f), 0.5f, new Color(0.40f, 0.95f, 0.30f)));
            Log.AppendLine("Seed: emissive sprout material");
        });
    }

    // --- Level 1 loose props ------------------------------------------------------------------

    static void FarmProps()
    {
        Prop(Greybox + "Greybox_Crate.prefab", ArtKit.Survival + "box.fbx", 0.85f);
        Prop(Greybox + "Greybox_Barrel.prefab", ArtKit.Survival + "barrel.fbx", 0.90f);
        // Fitting by height only works on models that are roughly as tall as they are wide.
        // rock_largeA is a flat slab (0.78 x 0.26 x 1.02) and fitting it to a 0.55 m height
        // blew it out to 2.15 m across — wide enough to close corridors the 2D layout leaves
        // open. rock_tallA is the boulder-shaped one; the fence keeps its own 1 m scale for
        // the same reason (a 1 m-tall Kenney fence panel would be 2.9 m wide).
        Prop(Greybox + "Greybox_Rock.prefab", ArtKit.Nature + "rock_tallA.fbx", 0.70f);
        // The fence keeps its 1 m panel width — the 2D layout posts them one metre apart, and a
        // Kenney panel fitted to a 1 m *height* would be 2.9 m across — but it gets stretched
        // vertically. At its import proportions it stands 0.35 m, which next to a 1.75 m
        // villager reads as a ladder lying on the ground rather than as a fence.
        TallFence(Greybox + "Greybox_Fence.prefab", 2.4f);
        // Greybox_Hut is built by VillageKit now — the Survival Kit "hut" it used to wear is an
        // open scaffold roof, not a house.

        // The dead tree keeps its two-node shape: the trunk owns the collider, the crown never
        // had one so the player can walk under the canopy.
        //
        // It also has to ask for its colour now. Before cycle 2 this model was "dead" only by
        // accident — the Nature Kit's own leafsDark imported as a washed-out turquoise. With
        // ArtKit's palette repainting that back to the deep green Kenney meant, an unrecoloured
        // tree_thin_dark is a *healthy* tree, and Level 1's 38 dead trees would all come back
        // to life. The dead read is a deliberate recolour from here on.
        Edit(Greybox + "Greybox_DeadTree.prefab", root =>
        {
            var trunk = root.transform.Find("Trunk");
            Kill(root.transform.Find("Crown"));
            Strip(trunk);
            trunk.localPosition = Vector3.zero;
            trunk.localScale = Vector3.one;

            var art = ArtKit.Spawn(ArtKit.Nature + "tree_thin_dark.fbx", trunk, 2.60f,
                                   variant: "Dead", recolour: DeadWoodPalette);
            if (art != null && trunk.TryGetComponent<CapsuleCollider>(out var cap))
            {
                cap.center = new Vector3(0f, 1.30f, 0f);
                cap.radius = 0.18f;
                cap.height = 2.60f;
                cap.direction = 1;
            }
            Log.AppendLine("Greybox_DeadTree: nature-kit tree, recoloured dead (2.6 m)");
        });

        // B1: the valley is not uniformly dead. Same prefab shape, same trunk collider, living
        // canopy — Level1Builder plants these where people still live and where the highland
        // spring still runs, so the polluted plain's dead trees read as a contrast.
        LivingTree("Greybox_TreeOak", "tree_oak", 3.40f, 0.26f);
        LivingTree("Greybox_TreePine", "tree_pineTallA", 4.20f, 0.22f);

        // Ground and boundary keep their ProBuilder geometry — ReclamationPatch re-tints the
        // floor tile by tile as its wave passes, so the tiling has to survive.
        Edit(Greybox + "Greybox_Floor.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial("FarmGround", new Color(0.42f, 0.38f, 0.26f)));
            Log.AppendLine("Greybox_Floor: barren-earth material (tiling kept for ReclamationPatch)");
        });
        Edit(Greybox + "Greybox_Wall.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial("FarmBoundary", new Color(0.30f, 0.28f, 0.22f)));
            Log.AppendLine("Greybox_Wall: earth-bank material (fence posts tiled by Level1Builder)");
        });
    }

    /// <summary>
    /// The fence panel at its own width but <paramref name="stretchY"/> times its height, with
    /// the box collider following. Raising a 0.35 m collider to 0.85 m changes nothing about
    /// what it blocks — a CharacterController was never stepping over either — so this is a
    /// look-only change.
    /// </summary>
    static void TallFence(string prefabPath, float stretchY)
    {
        Edit(prefabPath, root =>
        {
            var t = root.transform;
            Strip(t);
            t.localScale = Vector3.one;

            var art = ArtKit.Spawn(ArtKit.Nature + "fence_planksDouble.fbx", t, 0f);
            if (art == null) return;
            var s = art.transform.localScale;
            art.transform.localScale = new Vector3(s.x, s.y * stretchY, s.z);

            var size = ArtKit.Measure(art).size;
            if (root.TryGetComponent<BoxCollider>(out var box))
            {
                box.size = size;
                box.center = new Vector3(0f, size.y * 0.5f, 0f);
            }
            Log.AppendLine($"Greybox_Fence: fence_planksDouble stretched to " +
                           $"{size.x:F2}x{size.y:F2}x{size.z:F2} m, collider refit");
        });
    }

    /// <summary>
    /// A living tree, cloned off the dead one so the two-node shape (trunk owns the collider,
    /// canopy has none so the player walks under it) is identical by construction.
    /// </summary>
    static void LivingTree(string name, string model, float height, float radius)
    {
        Variant(Greybox + "Greybox_DeadTree.prefab", Greybox + name + ".prefab", root =>
        {
            var trunk = root.transform.Find("Trunk");
            ArtKit.Spawn(ArtKit.Nature + model + ".fbx", trunk, height);
            if (trunk.TryGetComponent<CapsuleCollider>(out var cap))
            {
                cap.center = new Vector3(0f, height * 0.5f, 0f);
                cap.radius = radius;
                cap.height = height;
                cap.direction = 1;
            }
            Log.AppendLine($"{name}: {model} ({height:F1} m), living canopy");
        });
    }

    /// <summary>
    /// A loose prop: root scale back to 1, collider rebuilt from the model's real bounds with
    /// its base on the floor, art dropped in at the origin.
    /// </summary>
    static void Prop(string prefabPath, string modelPath, float height)
    {
        Edit(prefabPath, root =>
        {
            var t = root.transform;
            Strip(t);
            t.localScale = Vector3.one;

            var art = ArtKit.Spawn(modelPath, t, height);
            if (art == null) return;

            var b = ArtKit.Measure(art);
            var size = b.size;
            switch (root.GetComponent<Collider>())
            {
                case BoxCollider box:
                    box.size = size;
                    box.center = new Vector3(0f, size.y * 0.5f, 0f);
                    break;
                case CapsuleCollider cap:
                    cap.direction = 1;
                    cap.radius = Mathf.Max(size.x, size.z) * 0.5f;
                    cap.height = size.y;
                    cap.center = new Vector3(0f, size.y * 0.5f, 0f);
                    break;
                case SphereCollider sphere:
                    sphere.radius = Mathf.Max(size.x, size.z) * 0.5f;
                    sphere.center = new Vector3(0f, size.y * 0.5f, 0f);
                    break;
            }
            Log.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(prefabPath)}: " +
                           $"{System.IO.Path.GetFileNameWithoutExtension(modelPath)} " +
                           $"({size.x:F2}x{size.y:F2}x{size.z:F2} m), collider refit");
        });
    }

    // --- cycle 2: the village kit -----------------------------------------------------------

    /// <summary>
    /// One Fantasy Town module, in metres — a cottage's whole footprint. Villagers are 1.75 m,
    /// so a one-storey cottage stands 2.9 m to the ridge and a two-storey one 4.8 m.
    ///
    /// <para>1.9 m and not more because the 2D layout puts two of the pen's four huts 2.24 m
    /// apart: at the 2.2 m this started out as, those two interpenetrated.</para>
    /// </summary>
    const float Cell = 1.9f;

    /// <summary>
    /// B4: the village. Kenney's Survival Kit — the pack the four huts were built from — has no
    /// houses in it at all, only open scaffold frames, which is why the "làng" read as a fence
    /// with four lean-tos. The Fantasy Town Kit does, as modules: one wall panel, one roof cap,
    /// assembled here.
    ///
    /// <para><see cref="Greybox_Hut"/> is rebuilt in place rather than replaced, so the four
    /// huts the 2D layout already places become real houses without touching the CSV.</para>
    /// </summary>
    static void VillageKit()
    {
        // Kenney ships this kit's colours as an atlas of flat bands, and the 2.0 download only
        // carries "variation-a" — whose roof band is lavender. The walls sample brown wood and
        // grey plaster and are kept as they import; the roof gets a solid material instead, so
        // the hamlet reads thatch-and-tile rather than purple.
        Cottage(Greybox + "Greybox_Hut.prefab", "wall-wood", false,
                new Color(0.52f, 0.40f, 0.21f));
        Cottage(Greybox + "Greybox_House.prefab", "wall", true,
                new Color(0.58f, 0.29f, 0.20f));

        // Single-piece village dressing. Each is its own prefab because Level1Builder places
        // them as instances and the stalls/fountain need to stop the player walking through.
        Solid(Greybox + "Greybox_Stall.prefab", ArtKit.Town + "stall-red.fbx", 2.30f);
        Solid(Greybox + "Greybox_Cart.prefab", ArtKit.Town + "cart.fbx", 0.90f);
        // The fountain is a wide, shallow basin: fitting it by height would blow a 2 m
        // bowl out to 6 m across.
        Solid(Greybox + "Greybox_Fountain.prefab", ArtKit.Town + "fountain-round.fbx", 0.45f);
    }

    /// <summary>
    /// Assemble a cottage inside an existing prefab: four wall panels round one module cell,
    /// a pyramid roof on top, and the root's box collider refit to the result.
    ///
    /// <para>The kit is authored on a 1 m grid with each wall panel lying on the <b>−X edge</b>
    /// of its cell, so the four walls are the same model at 0/90/180/270° — west, north, east,
    /// south. The door faces south because that is the side Greenie approaches every village
    /// building from.</para>
    /// </summary>
    static void Cottage(string prefabPath, string wall, bool twoStorey, Color roofColour)
    {
        Stub(prefabPath, "Obstacle");
        Edit(prefabPath, root =>
        {
            Strip(root.transform);
            root.transform.localScale = Vector3.one;

            int storeys = twoStorey ? 2 : 1;
            for (int s = 0; s < storeys; s++)
            {
                float y = s * Cell;
                // West and east stay blank walls; north takes a window, south the door on the
                // ground floor and a window above it.
                Module(root.transform, wall, 0f, y);
                Module(root.transform, wall + "-window-shutters", 90f, y);
                Module(root.transform, wall, 180f, y);
                Module(root.transform, s == 0 ? wall + "-door" : wall + "-window-shutters", 270f, y);
            }

            var roof = ArtKit.SpawnModule(ArtKit.Town + "roof-point.fbx", root.transform, Cell,
                                          cell: new Vector3(0f, storeys * Cell, 0f));
            Paint(roof.transform, ArtKit.SolidMaterial(
                "VillageRoof_" + ColorUtility.ToHtmlStringRGB(roofColour), roofColour), all: true);

            float height = (storeys + 0.5f) * Cell;
            if (root.TryGetComponent<BoxCollider>(out var box))
            {
                box.size = new Vector3(Cell, height, Cell);
                box.center = new Vector3(0f, height * 0.5f, 0f);
            }
            Log.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(prefabPath)}: " +
                           $"{storeys}-storey {wall} cottage ({Cell:F1} m footprint, {height:F1} m ridge)");
        });
    }

    static void Module(Transform root, string model, float rotY, float y) =>
        ArtKit.SpawnModule(ArtKit.Town + model + ".fbx", root, Cell, rotY, new Vector3(0f, y, 0f));

    /// <summary>A one-model prop prefab with a box collider fitted to its art.</summary>
    static void Solid(string prefabPath, string modelPath, float height)
    {
        Stub(prefabPath, "Obstacle");
        Edit(prefabPath, root =>
        {
            Strip(root.transform);
            root.transform.localScale = Vector3.one;
            var art = ArtKit.Spawn(modelPath, root.transform, height);
            if (art == null) return;
            var size = ArtKit.Measure(art).size;
            if (root.TryGetComponent<BoxCollider>(out var box))
            {
                box.size = size;
                box.center = new Vector3(0f, size.y * 0.5f, 0f);
            }
            Log.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(prefabPath)}: " +
                           $"{System.IO.Path.GetFileNameWithoutExtension(modelPath)} " +
                           $"({size.x:F2}x{size.y:F2}x{size.z:F2} m)");
        });
    }

    /// <summary>
    /// Make sure a prefab exists before <see cref="Edit"/> opens it, so the village kit can
    /// introduce new prefabs and still be re-runnable. Existing prefabs are left untouched —
    /// only the art inside them is rebuilt.
    /// </summary>
    static void Stub(string prefabPath, string layer)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        var stub = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath))
        {
            layer = LayerMask.NameToLayer(layer),
        };
        stub.AddComponent<BoxCollider>();
        PrefabUtility.SaveAsPrefabAsset(stub, prefabPath);
        UnityEngine.Object.DestroyImmediate(stub);
    }

    // --- Level 1 interactables -------------------------------------------------------------------

    static void FarmInteractables()
    {
        Edit(Greybox + "Chest.prefab", root =>
        {
            Kill(root.transform.Find("Body"));
            Kill(root.transform.Find("Lid"));
            ArtKit.Spawn(ArtKit.Survival + "chest.fbx", root.transform, 0.75f);
            // The core floats above the open chest; keep the node, make it glow properly.
            Paint(root.transform.Find("Core"), CoreMaterial());
            Log.AppendLine("Chest: survival-kit chest (0.75 m), core re-lit");
        });

        Edit(Greybox + "EnergyCore.prefab", root =>
        {
            Paint(root.transform.Find("Core"), CoreMaterial());
            Log.AppendLine("EnergyCore: emissive clean-energy material");
        });

        Edit(Greybox + "Herb.prefab", root =>
        {
            Kill(root.transform.Find("Leaf"));
            Kill(root.transform.Find("Stem"));
            ArtKit.Spawn(ArtKit.Nature + "flower_purpleB.fbx", root.transform, 0.55f);
            Log.AppendLine("Herb: nature-kit purple medicinal flower (0.55 m)");
        });

        Edit(Greybox + "Litter.prefab", root =>
        {
            Kill(root.transform.Find("Bag"));
            ArtKit.Spawn(ArtKit.Survival + "bottle-large.fbx", root.transform, 0.40f);
            Log.AppendLine("Litter: discarded bottle (0.40 m)");
        });

        Edit(Greybox + "LoreNote.prefab", root =>
        {
            Kill(root.transform.Find("Page"));
            ArtKit.Spawn(ArtKit.Nature + "sign.fbx", root.transform, 0.85f);
            Log.AppendLine("LoreNote: nature-kit sign (0.85 m)");
        });

        Edit(Greybox + "ItemPickup.prefab", root =>
        {
            Kill(root.transform.Find("Item"));
            ArtKit.Spawn(ArtKit.Survival + "bottle.fbx", root.transform, 0.35f, lift: 0.12f);
            Log.AppendLine("ItemPickup: survival bottle (0.35 m)");
        });

        // Hazards and the reclamation wave stay flat and readable — what the player must parse
        // is *which floor is unsafe*, so these keep their discs and get honest materials.
        Edit(Greybox + "ToxicMud.prefab", root =>
        {
            // Dark enough to read as a sludge pit. At the mid-green it had, a 7 x 5 m pool under
            // the farm's warm key light came out lighter than the grass around it and the two
            // biggest pools looked like lawn rugs laid on the dirt.
            Paint(root.transform.Find("Pool"), ArtKit.SolidMaterial(
                "ToxicMud", new Color(0.10f, 0.13f, 0.07f), 0.75f, new Color(0.06f, 0.11f, 0.02f)));
            Log.AppendLine("ToxicMud: wet toxic-sludge material (flat pool kept)");
        });

        Edit(Greybox + "ReclamationPatch.prefab", root =>
        {
            Paint(root.transform.Find("Disc"), ArtKit.SolidMaterial(
                "ReclaimedGrass", new Color(0.35f, 0.55f, 0.25f)));
            Log.AppendLine("ReclamationPatch: grass material (script re-tints it at runtime)");
        });

        Edit(Greybox + "TeleportGate.prefab", root =>
        {
            Strip(root.transform.Find("Base"));
            ArtKit.Spawn(ArtKit.Nature + "statue_ring.fbx", root.transform, 3.0f);
            Paint(root.transform.Find("Portal").Find("Panel"), ArtKit.SolidMaterial(
                "PortalSheet", new Color(0.25f, 0.75f, 0.95f), 0.8f, new Color(0.25f, 0.85f, 1f)));
            Log.AppendLine("TeleportGate: stone ring arch (3.0 m) + emissive portal sheet");
        });
    }

    // --- villagers ---------------------------------------------------------------------------

    static void Villagers()
    {
        // Ông Sáu and the male villagers.
        Edit(Greybox + "NPC_Villager.prefab", root =>
        {
            var body = root.transform.Find("Body");
            Strip(body);
            body.localScale = Vector3.one;
            body.localPosition = Vector3.zero;
            ArtKit.Spawn(ArtKit.Quat + "Farmer.glb", body, 1.75f, rotY: 180f, idle: true);
            if (body.TryGetComponent<CapsuleCollider>(out var cap))
            {
                cap.center = new Vector3(0f, 0.88f, 0f);
                cap.radius = 0.30f;
                cap.height = 1.75f;
                cap.direction = 1;
            }
            Log.AppendLine("NPC_Villager: Quaternius farmer (1.75 m), idle looping");
        });

        // Bà Tư needs a different silhouette, and Level1Builder now asks for this variant.
        Variant(Greybox + "NPC_Villager.prefab", Greybox + "NPC_VillagerWoman.prefab", root =>
        {
            // ClearArt has already removed the farmer this prefab was just given.
            var body = root.transform.Find("Body");
            ArtKit.Spawn(ArtKit.Quat + "Farmer.glb", body, 1.62f, rotY: 180f, idle: true,
                         variant: "BaTu", recolour: BaTuPalette);
            Log.AppendLine("NPC_VillagerWoman: recoloured farmer (1.62 m) — Bà Tư");
        });
    }

    /// <summary>
    /// Bà Tư and Tí are the same Quaternius farmer mesh in different clothes and at different
    /// heights. The other CC0 characters Poly Pizza offers are atlas-textured and ship without
    /// the atlas, so they import pure white — this mesh carries its colours per material and
    /// recolours cleanly.
    /// </summary>
    static readonly (string, Color)[] BaTuPalette =
    {
        ("Brown", new Color(0.46f, 0.13f, 0.16f)),        // dark red áo bà ba
        ("Brown2", new Color(0.30f, 0.10f, 0.12f)),
        ("LightBlue", new Color(0.20f, 0.20f, 0.23f)),    // black trousers
        ("Beige", new Color(0.78f, 0.77f, 0.73f)),        // grey hair under the nón lá
    };

    /// <summary>
    /// A tree the smoke has killed: grey-brown canopy, near-black bark. Only Level 1's dead
    /// trees and the Slime King's grove ask for it — every other Nature Kit model keeps
    /// <see cref="ArtKit"/>'s shared palette.
    /// </summary>
    static readonly (string, Color)[] DeadWoodPalette =
    {
        ("leafsDark", new Color(0.33f, 0.29f, 0.22f)),
        ("woodBarkDark", new Color(0.24f, 0.20f, 0.16f)),
    };

    /// <summary>Slime Chúa: the same mesh as a Plastic Slime, gone bruised-purple and gold-eyed.</summary>
    static readonly (string, Color)[] SlimeKingPalette =
    {
        ("Body", new Color(0.44f, 0.26f, 0.54f)),
        ("Eyes", new Color(0.98f, 0.80f, 0.25f)),
    };

    static readonly (string, Color)[] TiPalette =
    {
        ("Brown", new Color(0.88f, 0.72f, 0.20f)),        // bright yellow shirt
        ("Brown2", new Color(0.55f, 0.42f, 0.12f)),
        ("LightBlue", new Color(0.20f, 0.34f, 0.60f)),    // blue shorts
        ("Beige", new Color(0.68f, 0.22f, 0.18f)),        // red cap
    };

    // --- Level 2 ------------------------------------------------------------------------------

    public static void FactoryKit()
    {
        Edit(FactoryP + "Greybox_FactoryFloor.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial("FactoryFloor", new Color(0.20f, 0.21f, 0.24f), 0.25f));
            Log.AppendLine("Greybox_FactoryFloor: dark plant-floor material (stays scalable)");
        });
        Edit(FactoryP + "Greybox_FactoryWall.prefab", root =>
        {
            Paint(root.transform, ArtKit.SolidMaterial("FactoryWall", new Color(0.27f, 0.28f, 0.32f), 0.35f));
            Log.AppendLine("Greybox_FactoryWall: industrial panel material (stays scalable)");
        });

        Edit(FactoryP + "Keycard.prefab", root =>
        {
            Paint(root.transform.Find("Card"), ArtKit.SolidMaterial(
                "Keycard", new Color(0.10f, 0.55f, 0.70f), 0.7f, new Color(0.20f, 0.85f, 1f)));
            Log.AppendLine("Keycard: emissive card (kept small and flat so it reads as a pickup)");
        });

        Edit(FactoryP + "ManholeTrap.prefab", root =>
        {
            var lid = root.transform.Find("Lid");
            Kill(lid.Find("Plate"));
            ArtKit.Spawn(ArtKit.Factory + "button-floor-round.fbx", lid, 0.18f);
            Paint(root.transform.Find("Hole").Find("Pit"), ArtKit.SolidMaterial(
                "ManholePit", new Color(0.03f, 0.03f, 0.04f)));
            Log.AppendLine("ManholeTrap: factory floor plate as the lid, black pit below");
        });

        Edit(FactoryP + "SweepingLaser.prefab", root =>
        {
            var emitter = root.transform.Find("Emitter");
            Strip(emitter);
            emitter.localScale = Vector3.one;
            ArtKit.Spawn(ArtKit.Factory + "scanner-high.fbx", emitter, 1.30f, lift: -0.65f);
            // Bar is resized every Awake from beamLength/Width/Height — never hand-scale it.
            Paint(root.transform.Find("Beam").Find("Bar"), ArtKit.SolidMaterial(
                "LaserBeam", new Color(1f, 0.25f, 0.20f), 0.9f, new Color(1f, 0.30f, 0.15f)));
            Log.AppendLine("SweepingLaser: factory scanner emitter + hot emissive beam");
        });

        Edit(FactoryP + "ToxicGasZone.prefab", root =>
        {
            Paint(root.transform.Find("Cloud").Find("Disc"), ArtKit.SolidMaterial(
                "ToxicGas", new Color(0.45f, 0.60f, 0.22f), 0.3f, new Color(0.30f, 0.45f, 0.10f)));
            Log.AppendLine("ToxicGasZone: gas material (flat disc kept — a volume would hide the floor)");
        });

        Edit(FactoryP + "BossDoor.prefab", root =>
        {
            var door = ArtKit.SolidMaterial("BossDoorPanel", new Color(0.32f, 0.24f, 0.10f), 0.45f);
            var frame = ArtKit.SolidMaterial("BossDoorFrame", new Color(0.18f, 0.19f, 0.22f), 0.5f);
            Paint(root.transform.Find("LeftPanel").Find("Slab"), door);
            Paint(root.transform.Find("RightPanel").Find("Slab"), door);
            Paint(root.transform.Find("Frame_L"), frame);
            Paint(root.transform.Find("Frame_R"), frame);
            // Light_L / Light_R are tinted red -> green by BossDoor through MaterialTint, which
            // drives _BaseColor only. They deliberately get *no* emission: a fixed emissive
            // colour would keep glowing red under the green tint and the door would read locked
            // after it opened.
            var lamp = ArtKit.SolidMaterial("BossDoorLight", new Color(0.90f, 0.20f, 0.20f), 0.85f);
            Paint(root.transform.Find("Light_L"), lamp);
            Paint(root.transform.Find("Light_R"), lamp);
            ArtKit.Spawn(ArtKit.Factory + "warning-orange.fbx", root.transform.Find("Frame_L"), 1.20f);
            ArtKit.Spawn(ArtKit.Factory + "warning-orange.fbx", root.transform.Find("Frame_R"), 1.20f);
            Log.AppendLine("BossDoor: industrial door + frame materials, warning posts added");
        });

        Edit(FactoryP + "ReturnPortal.prefab", root =>
        {
            Strip(root.transform.Find("Base"));
            ArtKit.Spawn(ArtKit.Factory + "structure-doorway.fbx", root.transform, 3.0f);
            Paint(root.transform.Find("Ring"), ArtKit.SolidMaterial(
                "ReturnPortalRing", new Color(0.30f, 0.85f, 0.55f), 0.8f, new Color(0.25f, 1f, 0.60f)));
            Log.AppendLine("ReturnPortal: factory doorway + emissive ring");
        });

        Edit(FactoryP + "RescueNPC_Ti.prefab", root =>
        {
            var sick = root.transform.Find("Visual_Unconscious");
            var awake = root.transform.Find("Visual_Awake");
            Kill(sick.Find("Body"));
            Kill(awake.Find("Body"));

            // Two poses, not two textures: slumped on the floor, then standing. Tí is a boy,
            // so the same villager mesh at child height in brighter clothes.
            var down = ArtKit.Spawn(ArtKit.Quat + "Farmer.glb", sick, 1.35f,
                                    variant: "Ti", recolour: TiPalette);
            if (down != null)
            {
                down.transform.localRotation = Quaternion.Euler(-78f, 0f, 0f);
                down.transform.localPosition += new Vector3(0f, 0.18f, -0.30f);
            }
            ArtKit.Spawn(ArtKit.Quat + "Farmer.glb", awake, 1.35f, rotY: 180f, idle: true,
                         variant: "Ti", recolour: TiPalette);
            Log.AppendLine("RescueNPC_Ti: recoloured farmer at child height (1.35 m)");
        });
    }

    // --- the hub -------------------------------------------------------------------------------

    public static void HubKit()
    {
        Edit(Hub + "MrBear.prefab", root =>
        {
            Kill(root.transform.Find("Body"));
            Kill(root.transform.Find("Ear_L"));
            Kill(root.transform.Find("Ear_R"));
            var art = ArtKit.Spawn(ArtKit.Pets + "animal-polar.fbx", root.transform, 1.55f, rotY: 180f);
            // Kenney's only bear is a polar bear; the shop keeper is a brown one.
            if (art != null)
                Paint(art.transform, ArtKit.TintedFrom("MrBearFur",
                    ArtKit.Pets + "Textures/colormap.png", new Color(0.52f, 0.35f, 0.22f)), all: true);
            Log.AppendLine("MrBear: cube-pets bear (1.55 m), tinted brown");
        });

        Edit(Hub + "RecyclingCounter.prefab", root =>
        {
            Kill(root.transform.Find("Counter"));
            Kill(root.transform.Find("Bin"));
            ArtKit.Spawn(ArtKit.Factory + "hopper-square.fbx", root.transform, 1.30f);
            Log.AppendLine("RecyclingCounter: factory hopper (1.30 m) as the recycling chute");
        });

        Edit(Hub + "CraftingBench.prefab", root =>
        {
            Kill(root.transform.Find("Top"));
            Kill(root.transform.Find("Leg_L"));
            Kill(root.transform.Find("Leg_R"));
            Kill(root.transform.Find("Vice"));
            ArtKit.Spawn(ArtKit.Survival + "workbench.fbx", root.transform, 1.05f);
            Log.AppendLine("CraftingBench: survival workbench (1.05 m)");
        });

        Edit(Hub + "StagePortal.prefab", root =>
        {
            Strip(root.transform.Find("Base"));
            Kill(root.transform.Find("Arch_L"));
            Kill(root.transform.Find("Arch_R"));
            ArtKit.Spawn(ArtKit.Factory + "structure-doorway.fbx", root.transform, 3.2f);
            // Visual_Open / Visual_Locked are toggled by StagePortal — keep both, re-light them
            // and tuck the sheet inside the doorway's opening instead of overhanging the frame.
            foreach (var (node, mat) in new[]
            {
                ("Visual_Open", ArtKit.SolidMaterial("PortalOpen",
                    new Color(0.25f, 0.80f, 0.95f), 0.8f, new Color(0.25f, 0.85f, 1f))),
                ("Visual_Locked", ArtKit.SolidMaterial("PortalLocked",
                    new Color(0.35f, 0.10f, 0.12f), 0.4f, new Color(0.40f, 0.05f, 0.05f))),
            })
            {
                var sheet = root.transform.Find(node).Find("Sheet");
                Paint(sheet, mat);
                sheet.localScale = new Vector3(1.15f, 1.95f, 0.06f);
                sheet.localPosition = new Vector3(0f, 1.02f, 0f);
            }
            Log.AppendLine("StagePortal: factory doorway (3.2 m), open/locked sheets re-lit");
        });
    }

    // --- helpers ---------------------------------------------------------------------------------

    static Material CoreMaterial() => ArtKit.SolidMaterial(
        "EnergyCore", new Color(0.45f, 0.95f, 0.70f), 0.9f, new Color(0.35f, 1f, 0.65f));

    static void Edit(string prefabPath, Action<GameObject> mutate)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) { Log.AppendLine("MISSING PREFAB " + prefabPath); return; }
        try
        {
            ClearArt(root);
            mutate(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>Save a modified copy of an existing prefab under a new path.</summary>
    static void Variant(string sourcePath, string newPath, Action<GameObject> mutate)
    {
        var root = PrefabUtility.LoadPrefabContents(sourcePath);
        if (root == null) { Log.AppendLine("MISSING PREFAB " + sourcePath); return; }
        try
        {
            ClearArt(root);
            mutate(root);
            PrefabUtility.SaveAsPrefabAsset(root, newPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>
    /// Drop anything a previous run added, so the pass is re-runnable. Without this a second
    /// run stacks a second copy of every model on top of the first.
    /// </summary>
    static void ClearArt(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true).ToArray())
            if (t != null && t != root.transform && t.name.StartsWith("Art_"))
                UnityEngine.Object.DestroyImmediate(t.gameObject);
    }

    /// <summary>Drop a node's mesh but keep the node — several of them own a collider.</summary>
    static void Strip(Transform t)
    {
        if (t == null) return;
        if (t.TryGetComponent<MeshRenderer>(out var mr)) UnityEngine.Object.DestroyImmediate(mr);
        if (t.TryGetComponent<MeshFilter>(out var mf)) UnityEngine.Object.DestroyImmediate(mf);
    }

    static void Kill(Transform t)
    {
        if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
    }

    /// <summary>
    /// Drop a model at an offset inside a prefab. <see cref="ArtKit.Spawn"/> always grounds and
    /// centres its model on the node it is given, which is exactly what a single-mesh swap
    /// wants and useless when a boss is assembled from three pieces — so each piece gets a
    /// holder of its own. The holder is named <c>Art_*</c> so <see cref="ClearArt"/> takes the
    /// whole rig away and the pass stays re-runnable.
    /// </summary>
    static GameObject Rig(Transform parent, string name, Vector3 local, string model,
                          float height, float rotY = 0f)
    {
        var holder = new GameObject("Art_" + name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = local;
        if (ArtKit.Spawn(model, holder.transform, height, rotY) != null) return holder;
        UnityEngine.Object.DestroyImmediate(holder);
        return null;
    }

    static void Paint(Transform t, Material mat, bool all = false)
    {
        if (t == null || mat == null) return;
        var targets = all ? t.GetComponentsInChildren<Renderer>(true) : t.GetComponents<Renderer>();
        foreach (var r in targets)
            r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
    }
}

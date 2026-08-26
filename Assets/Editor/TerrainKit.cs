using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cycle 2: the landscape generators — the half of the A2 brief the 2D layout could never supply.
///
/// <para>The 2D scene is a flat tilemap, so <see cref="Level1Builder"/> faithfully reproduces a
/// flat valley: one ProBuilder slab at y = 0, four 3 m walls, and nothing beyond them. That is
/// correct as a port and wrong as a place. This adds the parts that were never in the 2D scene
/// to be ported: hills, a highland mesa, water, and a village that looks like somewhere people
/// lived.</para>
///
/// <para><b>Elevation here is scenery, not platforming.</b> Golden rule #1 still holds — Greenie
/// moves on the XZ plane and gravity is not a mechanic — so nothing below is climbable. The
/// mesa and the pond are obstacles the player walks <i>around</i> (one box collider, one sphere,
/// both on Obstacle so the NavMesh carves them and the slimes path around them too), and
/// everything outside the boundary walls is pure silhouette with no collider at all.</para>
///
/// <para>Placement is checked against <see cref="Level1Builder"/>'s <c>occupied</c> list and
/// feeds back into it, so the ground scatter never buries anything and nothing here lands on a
/// chest, a herb or a slime spawn.</para>
/// </summary>
public static class TerrainKit
{
    const string Kit = "Assets/Prefabs/Greybox/";

    // The mesa and its spring, in the empty north-west quarter: far enough from the corner
    // chest at (-30, 22) and the slime spawn at (-19.3, 12.2) to leave both reachable.
    static readonly Vector2 MesaCentre = new(-24.5f, 17.5f);
    static readonly Vector2 MesaSize = new(8f, 6f);
    static readonly Vector2 PondCentre = new(-24.5f, 10.5f);
    const float PondRadius = 3.4f;
    const float MesaHeight = 4.2f;

    static StringBuilder log;
    static List<Vector2> occupied;
    static System.Random rng;

    public static void Level1(Transform envRoot, Transform propRoot,
                             List<Vector2> occupiedXZ, StringBuilder builderLog,
                             float halfX, float halfZ)
    {
        log = builderLog;
        occupied = occupiedXZ;
        rng = new System.Random(20260820);   // deterministic: a rebuild reproduces the valley

        var root = new GameObject("Terrain").transform;
        root.SetParent(envRoot, false);

        OuterGround(root, halfX, halfZ);
        OuterHills(root, halfX, halfZ);
        Mesa(root);
        Pond(root);
        Village(propRoot);
    }

    /// <summary>
    /// The cheap version, for a scene that only needs to stop ending in void: outer ground and
    /// one ring of hills, no village and no water. Used by the hub, whose 3 m walls the camera
    /// comfortably sees over.
    /// </summary>
    public static void Surround(Transform envRoot, StringBuilder builderLog,
                                float halfX, float halfZ, int seed)
    {
        log = builderLog;
        rng = new System.Random(seed);

        var root = new GameObject("Terrain").transform;
        root.SetParent(envRoot, false);
        OuterGround(root, halfX, halfZ);

        int blocks = 0, trees = 0;
        foreach (var (distance, size, storeys, step) in new[] { (8f, 6f, 1, 6f), (18f, 9f, 2, 8f) })
        {
            foreach (var p in Ring(halfX + distance, halfZ + distance, step))
            {
                var at = new Vector3(p.x + Jitter(step * 0.3f), 0f, p.y + Jitter(step * 0.3f));
                int n = Mathf.Max(1, storeys + rng.Next(-1, 2));
                float top = Stack(root, at, size * (0.8f + (float)rng.NextDouble() * 0.4f), n);
                blocks += n;

                if (rng.Next(3) != 0) continue;
                var tree = new GameObject("RidgeTree");
                tree.transform.SetParent(root, false);
                tree.transform.position = at + new Vector3(Jitter(size * 0.3f), top, Jitter(size * 0.3f));
                ArtKit.Spawn(ArtKit.Nature + (rng.Next(2) == 0 ? "tree_pineTallA" : "tree_oak") + ".fbx",
                             tree.transform, 4.5f + (float)rng.NextDouble() * 2.5f);
                GameObjectUtility.SetStaticEditorFlags(tree, StaticEditorFlags.BatchingStatic);
                trees++;
            }
        }
        log.AppendLine($"  surroundings: {blocks} hill blocks, {trees} trees beyond the walls");
    }

    /// <summary>
    /// One dark slab under everything, for a level whose floor is laid corridor by corridor.
    /// Level 2's maze leaves gaps between its rooms, and the camera sees straight through them
    /// to the skybox — a black hole in the middle of a lit factory. Two centimetres below the
    /// floor slabs, so it never shows where there *is* floor.
    /// </summary>
    public static void Underlay(Transform envRoot, StringBuilder builderLog,
                                Vector2 centre, Vector2 size, Color colour)
    {
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Underlay";
        slab.transform.SetParent(envRoot, false);
        slab.transform.position = new Vector3(centre.x, -0.52f, centre.y);   // top face at y = -0.02
        slab.transform.localScale = new Vector3(size.x, 1f, size.y);
        Object.DestroyImmediate(slab.GetComponent<Collider>());
        slab.GetComponent<Renderer>().sharedMaterial = ArtKit.SolidMaterial("PlantYard", colour, 0.2f);
        GameObjectUtility.SetStaticEditorFlags(slab, StaticEditorFlags.BatchingStatic);
        builderLog.AppendLine($"  underlay: {size.x:F0} x {size.y:F0} m slab under the maze");
    }

    // --- beyond the walls ---------------------------------------------------------------------

    /// <summary>
    /// Land outside the boundary walls. Without it the hills and the lake stand in empty space
    /// and read as blocks floating over a void, because <see cref="Level1Builder"/>'s ground is
    /// only the 65 × 49 m the 2D tilemap covered.
    ///
    /// <para>Four slabs framing the arena rather than one plane through it: a single plane would
    /// be coplanar with the play floor and z-fight it. They are green, not the valley's barren
    /// brown — the smoke is a valley problem, and seeing living land just past the wall is the
    /// clearest statement of that the level can make.</para>
    /// </summary>
    static void OuterGround(Transform parent, float halfX, float halfZ)
    {
        const float reach = 110f;
        var ground = new GameObject("OuterGround").transform;
        ground.SetParent(parent, false);
        var grass = ArtKit.SolidMaterial("OuterPlain", new Color(0.31f, 0.40f, 0.23f));

        float side = (reach - halfX) * 0.5f;
        float cap = (reach - halfZ) * 0.5f;
        foreach (var (name, centre, size) in new[]
        {
            ("West", new Vector3(-(halfX + side), 0f, 0f), new Vector3(side * 2f, 1f, reach * 2f)),
            ("East", new Vector3(halfX + side, 0f, 0f), new Vector3(side * 2f, 1f, reach * 2f)),
            ("North", new Vector3(0f, 0f, halfZ + cap), new Vector3(halfX * 2f, 1f, cap * 2f)),
            ("South", new Vector3(0f, 0f, -(halfZ + cap)), new Vector3(halfX * 2f, 1f, cap * 2f)),
        })
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Plain_" + name;
            slab.transform.SetParent(ground, false);
            slab.transform.position = centre + Vector3.down * 0.5f;   // top face at y = 0
            slab.transform.localScale = size;
            Object.DestroyImmediate(slab.GetComponent<Collider>());   // never walkable
            slab.GetComponent<Renderer>().sharedMaterial = grass;
            GameObjectUtility.SetStaticEditorFlags(slab, StaticEditorFlags.BatchingStatic);
        }
        log.AppendLine($"  outer plain: 4 slabs out to {reach:F0} m, no colliders");
    }

    /// <summary>
    /// Rings the arena in stepped hills so the valley sits in a landscape instead of ending at a
    /// 3 m wall with skybox behind it. Three bands, each further out and taller, built from the
    /// Nature Kit's 1 m cliff cubes stretched to chunky blocks — the same trick the kit's own
    /// promo shots use. No colliders: the player can never reach them.
    /// </summary>
    static void OuterHills(Transform parent, float halfX, float halfZ)
    {
        var hills = new GameObject("OuterHills").transform;
        hills.SetParent(parent, false);

        // A lake in the far north-east, framed by the hills that step around it.
        var lake = new Rect(2f, 38f, 52f, 34f);

        int blocks = 0, trees = 0;
        foreach (var (distance, size, storeys, step) in new[]
        {
            (10f, 8f, 1, 7f), (21f, 10f, 2, 9f), (35f, 12f, 3, 11f),
        })
        {
            float x0 = halfX + distance, z0 = halfZ + distance;
            foreach (var p in Ring(x0, z0, step))
            {
                var at = new Vector3(p.x + Jitter(step * 0.35f), 0f, p.y + Jitter(step * 0.35f));
                if (lake.Contains(new Vector2(at.x, at.z))) continue;

                int n = Mathf.Max(1, storeys + rng.Next(-1, 2));
                float top = Stack(hills, at, size * (0.8f + (float)rng.NextDouble() * 0.4f), n);
                blocks += n;

                // A treeline on the ridges, so the hills read as forest rather than as rock.
                if (rng.Next(3) != 0) continue;
                var tree = new GameObject("RidgeTree");
                tree.transform.SetParent(hills, false);
                tree.transform.position = at + new Vector3(Jitter(size * 0.3f), top, Jitter(size * 0.3f));
                ArtKit.Spawn(ArtKit.Nature + (rng.Next(2) == 0 ? "tree_pineTallA" : "tree_pineRoundC") + ".fbx",
                             tree.transform, 5f + (float)rng.NextDouble() * 4f);
                GameObjectUtility.SetStaticEditorFlags(tree, StaticEditorFlags.BatchingStatic);
                trees++;
            }
        }

        var water = WaterPlane(hills, "OuterLake",
                               new Vector3(lake.center.x, 0.02f, lake.center.y),
                               new Vector3(lake.width, 0.6f, lake.height));
        GameObjectUtility.SetStaticEditorFlags(water, StaticEditorFlags.BatchingStatic);

        log.AppendLine($"  outer hills: {blocks} blocks in 3 bands, {trees} ridge trees, " +
                       $"lake {lake.width:F0}x{lake.height:F0} m beyond the north-east wall");
    }

    /// <summary>
    /// A column of Kenney cliff cubes, and the Y its top face lands on.
    ///
    /// <para>The cubes are scaled <b>uniformly</b> and stacked, never stretched. A cliff block
    /// carries a grass cap that is a fixed fraction of its own height, so scaling Y alone turns
    /// that cap into a metre-thick slab of green and the whole thing reads as a chocolate cake —
    /// which is exactly what the first attempt at these hills looked like. Stacking keeps every
    /// cap in proportion, and a stacked cube hides the cap of the one under it.</para>
    /// </summary>
    /// <param name="solid">
    /// Give each column its own collider. Only the mesa wants this — the outer hills stand past
    /// the boundary walls where the player can never reach them, and colliding scenery there
    /// would only spend NavMesh budget.
    /// </param>
    static float Stack(Transform parent, Vector3 at, float size, int count, bool solid = false)
    {
        Bounds? rock = null;
        for (int i = 0; i < count; i++)
        {
            var holder = new GameObject("Rock");
            holder.transform.SetParent(parent, false);
            holder.transform.position = at + new Vector3(0f, i * size, 0f);
            string face = rng.Next(3) == 0 ? "cliff_block_stone" : "cliff_block_rock";
            var art = ArtKit.SpawnModule(ArtKit.Nature + face + ".fbx", holder.transform, size,
                                         rng.Next(4) * 90f);
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);

            if (!solid || art == null) continue;
            var b = ArtKit.Measure(art);
            if (rock == null) rock = b;
            else { var grown = rock.Value; grown.Encapsulate(b); rock = grown; }
        }
        if (solid && rock != null) Column(parent, rock.Value);
        return count * size;
    }

    /// <summary>
    /// One box per built column, tracing the rock that is actually there.
    ///
    /// <para>The mesa used to carry a single <see cref="BoxCollider"/> sized to the bounds of
    /// every cell that got built. The intent was right — a full-grid box would have stopped the
    /// player short of thin air — but <see cref="Bounds.Encapsulate"/> yields an axis-aligned
    /// rectangle and the whole point of the silhouette is that it is <i>not</i> one: the cells
    /// the height rule rolled empty fell inside it, leaving 6.5 m² of invisible wall in open
    /// ground and phantom corners up to 1.75 m deep (QA C3). A box per column is exact, and the
    /// three dozen of them are static and batched.</para>
    /// </summary>
    static void Column(Transform parent, Bounds rock)
    {
        var go = new GameObject("Column");
        go.layer = LayerMask.NameToLayer("Obstacle");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(rock.center.x, 0f, rock.center.z);
        var box = go.AddComponent<BoxCollider>();
        // Grown down to the ground: the cubes are stacked up from y = 0, and a box floating at
        // the mesh's own min would let the player walk in under the lowest tier.
        box.size = new Vector3(rock.size.x, rock.max.y, rock.size.z);
        box.center = new Vector3(0f, rock.max.y * 0.5f, 0f);
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>Points along the perimeter of a rectangle, spaced roughly <paramref name="step"/> apart.</summary>
    static IEnumerable<Vector2> Ring(float halfX, float halfZ, float step)
    {
        for (float x = -halfX; x <= halfX; x += step)
        {
            yield return new Vector2(x, -halfZ);
            yield return new Vector2(x, halfZ);
        }
        for (float z = -halfZ + step; z < halfZ; z += step)
        {
            yield return new Vector2(-halfX, z);
            yield return new Vector2(halfX, z);
        }
    }

    // --- inside the walls ---------------------------------------------------------------------

    /// <summary>
    /// The one piece of elevation the player actually walks up to: a stepped rock mesa in the
    /// empty north-west quarter, tall enough (4.2 m) to read as terrain next to a 1 m robot.
    /// Its whole footprint is one box collider, so it behaves exactly like a very large rock.
    /// </summary>
    static void Mesa(Transform parent)
    {
        var root = new GameObject("Highlands");
        root.layer = LayerMask.NameToLayer("Obstacle");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(MesaCentre.x, 0f, MesaCentre.y);

        const float cell = 1.4f;
        int nx = Mathf.RoundToInt(MesaSize.x / cell), nz = Mathf.RoundToInt(MesaSize.y / cell);
        var summits = new List<Vector3>();
        int placed = 0, columns = 0;
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < nz; j++)
            {
                // Height falls off from the middle, plus noise: a radial mound with a ragged
                // edge rather than concentric rectangles, which is what a tier-per-ring rule
                // gives and which reads as a layer cake.
                float fx = (i - (nx - 1) * 0.5f) / (nx * 0.5f);
                float fz = (j - (nz - 1) * 0.5f) / (nz * 0.5f);
                float r = Mathf.Sqrt(fx * fx + fz * fz);
                int n = Mathf.Clamp(Mathf.RoundToInt(3.5f - r * 3.1f + Jitter(0.85f)), 0, 3);
                if (n == 0) continue;

                var at = new Vector3(MesaCentre.x + (i - (nx - 1) * 0.5f) * cell, 0f,
                                     MesaCentre.y + (j - (nz - 1) * 0.5f) * cell);
                float top = Stack(root.transform, at, cell, n, solid: true);
                placed += n;
                columns++;
                if (n >= 2) summits.Add(new Vector3(at.x, top, at.z));
            }
        }

        // Pines on the summit — the only living trees in the valley the smoke never reached.
        // Planted on columns that were actually built, at that column's own top, because the
        // tiers are jittered and a fixed height would leave some of them hanging in the air.
        int pines = 0;
        for (int i = 0; i < summits.Count && pines < 5; i += Mathf.Max(1, summits.Count / 5))
        {
            var tree = new GameObject("SummitPine");
            tree.transform.SetParent(root.transform, false);
            tree.transform.position = summits[i] + new Vector3(Jitter(0.4f), 0f, Jitter(0.4f));
            ArtKit.Spawn(ArtKit.Nature + "tree_pineTallA.fbx", tree.transform,
                         3.2f + (float)rng.NextDouble() * 1.4f);
            pines++;
        }

        Reserve(MesaCentre, nx * cell * 0.5f + 1.5f);
        log.AppendLine($"  highlands: {placed} rock cubes ({nx * cell:F1}x{nz * cell:F1} m, " +
                       $"{MesaHeight:F1} m), {pines} summit pines, {columns} column colliders");
    }

    /// <summary>
    /// The spring at the mesa's foot. A blue disc on the Water layer with a sphere blocker under
    /// it — the blocker is what the NavMesh bakes against, so Greenie and the slimes both walk
    /// round the pool rather than across it.
    /// </summary>
    static void Pond(Transform parent)
    {
        var root = new GameObject("Spring").transform;
        root.SetParent(parent, false);
        root.position = new Vector3(PondCentre.x, 0f, PondCentre.y);

        // The surface sits 3 cm *above* the floor slab, not level with it. A water plane at
        // y = 0 is coplanar with the ground and z-fights it into a mottled mess — which is how
        // the first pass ended up looking like a mushroom-shaped stain rather than a pool.
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Water";
        disc.layer = LayerMask.NameToLayer("Water");
        disc.transform.SetParent(root, false);
        disc.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        disc.transform.localScale = new Vector3(PondRadius * 2f, 0.05f, PondRadius * 2f);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.GetComponent<Renderer>().sharedMaterial = ArtKit.SolidMaterial(
            "SpringWater", new Color(0.20f, 0.48f, 0.64f), 0.90f);
        GameObjectUtility.SetStaticEditorFlags(disc, StaticEditorFlags.BatchingStatic);

        // Nothing solid over the water. The old blocker was a sphere of this radius sunk to
        // center.y = -1.4, meant to let the player stand on the bank; a sphere cut at Greenie's
        // shins is narrower than at its equator, so it stopped him 0.45 m short of water he
        // could see (QA C1) — and its top reached y = 2.00, four times his own 0.60 m fire
        // height, so it quietly destroyed every Seed fired across the pool (QA C2).
        var wade = new GameObject("Wade");
        wade.layer = LayerMask.NameToLayer("Water");
        wade.transform.SetParent(root, false);
        var trigger = wade.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = PondRadius;
        trigger.center = new Vector3(0f, 0.4f, 0f);   // around the player's waist, not his shins
        wade.AddComponent<WaterWade>();

        // The slimes still keep to the bank — carved out of the NavMesh rather than walled off,
        // so the water stops what walks without standing in the way of what flies over it.
        var carve = new GameObject("NoSwim");
        carve.transform.SetParent(root, false);
        var volume = carve.AddComponent<NavMeshModifierVolume>();
        volume.size = new Vector3(PondRadius * 2f, 2f, PondRadius * 2f);
        volume.center = new Vector3(0f, 0.5f, 0f);
        volume.area = 1;                              // "Not Walkable"

        int rim = 0;
        for (int i = 0; i < 26; i++)
        {
            float a = i * Mathf.PI * 2f / 26f;
            var holder = new GameObject("Bank");
            holder.transform.SetParent(root, false);
            float ring = PondRadius + 0.25f + Jitter(0.35f);
            holder.transform.localPosition = new Vector3(Mathf.Cos(a) * ring, 0f, Mathf.Sin(a) * ring);
            string model = (i % 3) switch
            {
                0 => "stone_smallA",
                1 => "plant_bushSmall",
                _ => "grass_large",
            };
            if (ArtKit.Spawn(ArtKit.Nature + model + ".fbx", holder.transform, 0f,
                             (float)rng.NextDouble() * 360f) != null) rim++;
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
        }

        for (int i = 0; i < 4; i++)
        {
            var lily = new GameObject("Lily");
            lily.transform.SetParent(root, false);
            lily.transform.localPosition = new Vector3(Jitter(2f), 0.05f, Jitter(2f));
            ArtKit.Spawn(ArtKit.Nature + "lily_large.fbx", lily.transform, 0f,
                         (float)rng.NextDouble() * 360f);
        }

        var canoe = new GameObject("Canoe");
        canoe.transform.SetParent(root, false);
        canoe.transform.localPosition = new Vector3(1.9f, 0f, -3.1f);
        canoe.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);   // turn the holder, not the hull
        var hull = ArtKit.Spawn(ArtKit.Nature + "canoe.fbx", canoe.transform, 0.55f);
        ArtKit.Solidify(canoe, hull, minHeight: 0.4f);

        Reserve(PondCentre, PondRadius + 1.5f);
        log.AppendLine($"  spring: {PondRadius * 2f:F1} m wade pool at the mesa's foot, " +
                       $"{rim} bank props, 4 lilies, a solid beached canoe");
    }

    /// <summary>A flat, unlit-blue water quad with no collider — outer scenery only.</summary>
    static GameObject WaterPlane(Transform parent, string name, Vector3 centre, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = LayerMask.NameToLayer("Water");
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = ArtKit.SolidMaterial(
            "DistantWater", new Color(0.17f, 0.42f, 0.58f), 0.92f);
        return go;
    }

    // --- the village ---------------------------------------------------------------------------

    /// <summary>
    /// B4: a village district in the empty strip north of the 2D layout's fenced pen (z 17–23,
    /// which the CSV leaves completely bare). The pen's own four huts are already real cottages
    /// by now — <see cref="ArtPass"/> rebuilt <c>Greybox_Hut</c> — so this adds the rest of the
    /// hamlet around them: taller houses, a square with a fountain, market stalls and the trees
    /// people would have planted.
    /// </summary>
    static void Village(Transform parent)
    {
        var root = new GameObject("Village").transform;
        root.SetParent(parent, false);

        var buildings = new (string prefab, float x, float z, float rotY)[]
        {
            ("Greybox_House", -7f, 20.5f, 8f),
            ("Greybox_Hut", -2f, 20.2f, -6f),
            ("Greybox_House", 3.5f, 20.5f, -4f),
            ("Greybox_Hut", 9f, 20f, 12f),
            ("Greybox_Stall", -4.5f, 18.3f, 15f),
            ("Greybox_Stall", 6.5f, 18.3f, -20f),
            ("Greybox_Fountain", 1f, 18.2f, 0f),
            ("Greybox_Cart", -9.5f, 18.5f, 40f),
        };

        int placed = 0;
        foreach (var (prefabName, x, z, rotY) in buildings)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + prefabName + ".prefab");
            if (prefab == null) { log.AppendLine("  MISSING " + prefabName); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.name = prefabName.Replace("Greybox_", "Village_") + "_" + placed;
            go.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, rotY, 0f));
            Reserve(new Vector2(x, z), 2.6f);
            placed++;
        }

        // Living trees, only where the village is: the plain keeps its dead ones, so the two
        // read against each other instead of the whole valley looking equally sick.
        var trees = new (string prefab, float x, float z)[]
        {
            ("Greybox_TreeOak", -10.5f, 21.5f),
            ("Greybox_TreeOak", 11.5f, 21.5f),
            ("Greybox_TreeOak", -6.5f, 17.4f),
            ("Greybox_TreePine", 13f, 19f),
            ("Greybox_TreeOak", 0.5f, 22.8f),
            ("Greybox_TreePine", -12.5f, 18.5f),
        };
        foreach (var (prefabName, x, z) in trees)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + prefabName + ".prefab");
            if (prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.name = "Village" + prefabName.Replace("Greybox_", "");
            go.transform.SetPositionAndRotation(
                new Vector3(x, 0f, z), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            Reserve(new Vector2(x, z), 1.8f);
        }

        foreach (var (x, z) in new[] { (-2.6f, 18.2f), (4.6f, 18.2f), (1f, 22f) })
        {
            var holder = new GameObject("Lantern");
            holder.transform.SetParent(root, false);
            holder.transform.position = new Vector3(x, 0f, z);
            var post = ArtKit.Spawn(ArtKit.Town + "lantern.fbx", holder.transform, 2.6f);
            // A 2.6 m post you walk through is the same tell as the hub's props (QA C7).
            ArtKit.Solidify(holder, post, maxHalfExtent: 0.3f);
            Reserve(new Vector2(x, z), 1.2f);
        }

        log.AppendLine($"  village: {placed} buildings + {trees.Length} living trees + 3 lanterns " +
                       "in the empty strip north of the 2D pen");
    }

    // --- helpers ---------------------------------------------------------------------------------

    static float Jitter(float amount) => (float)(rng.NextDouble() * 2.0 - 1.0) * amount;

    /// <summary>
    /// Claim an area so <see cref="Level1Builder"/>'s ground scatter keeps out of it. Approximated
    /// with a ring of points because the scatter's own test is a single clearance radius.
    /// </summary>
    static void Reserve(Vector2 centre, float radius)
    {
        occupied.Add(centre);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            occupied.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
    }
}

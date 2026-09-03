using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// B1 + B2: build Assets/_Scenes/Level1_BarrenFarm.unity from the 2D level's own
/// layout. Every placement comes out of Temp/level1_layout.csv, exported straight
/// from the 2D scene YAML, so the farm keeps its exact proportions: 2D (x, y) maps
/// to 3D (x, z) on the ground plane, 1 tile = 1 m.
///
/// Idempotent — re-running rebuilds the scene from scratch. Regenerate the CSV with
/// <c>Tools/dump_scene.py</c> + <c>Tools/export_layout.py</c> if the 2D level changes.
/// </summary>
public static class Level1Builder
{
    [MenuItem("Eco-Dash/Rebuild Level 1 from the 2D layout")]
    public static void Rebuild() => Debug.Log(Execute());

    const string ScenePath = "Assets/_Scenes/Level1_BarrenFarm.unity";
    const string LayoutCsv = "Tools/level1_layout.csv";
    const string Kit = "Assets/Prefabs/Greybox/";

    // The 2D level's walls sat at x = ±32 and y = ±24, 1 m thick (64 x 48 m tile grid).
    const float HalfX = 32f, HalfZ = 24f, WallHeight = 3f, Tile = 4f;

    // The boss grove's centre, shared by BossGrove (which builds it) and BuildProfile (which
    // holds the ground flat under it).
    static readonly Vector3 GroveCentre = new(-27f, 0f, -19f);

    /// <summary>
    /// Whether the valley gets its Toxic Mud pools. <b>Temporarily false</b> (2026-08-31, PO
    /// call): the art reads badly and the hazard has a defect, so the mud is out until it is
    /// redrawn. Flip this back to <c>true</c> and rebuild to restore it — nothing else needs
    /// changing, and the <c>mud</c> rows are still in
    /// <see href="../../Tools/level1_layout.csv">level1_layout.csv</see>.
    ///
    /// <para>It governs <b>both</b> uses of the prefab, because they are the same pool with the
    /// same art: the three <c>mud</c> rows out on the farm, and the two <c>GroveSludge</c> pools
    /// <see cref="BossGrove"/> puts around the Slime King. Level 1 is the only scene that has
    /// ever used it, so this switch is the whole of its presence in the game.</para>
    ///
    /// <para><b>The pond is unaffected and deliberately so.</b> <see cref="WaterWade"/> is a
    /// separate script on a separate prefab; it only shares
    /// <c>PlayerController.EnterMud</c>/<c>ExitMud</c>, which is a speed hook and not a mud
    /// hook. Wading still slows Greenie exactly as before.</para>
    ///
    /// <para><c>static readonly</c> rather than <c>const</c> deliberately: a <c>const false</c>
    /// is folded at compile time and every line it guards becomes unreachable code the compiler
    /// warns about. This is a switch meant to be flipped, not dead code.</para>
    /// </summary>
    static readonly bool ToxicMudEnabled = false;

    static StringBuilder log;
    static Transform envRoot, propRoot, playRoot, enemyRoot;
    static string[] layout;      // the CSV, read once: the profile needs it before placement does
    static GroundProfile ground; // B8: the valley's relief, and the only copy of it

    public static string Execute()
    {
        log = new StringBuilder();
        if (!File.Exists(LayoutCsv)) return "ERROR: " + LayoutCsv + " missing — run export_layout.py first";
        layout = File.ReadAllLines(LayoutCsv);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // The CameraRig prefab brings its own camera; the template one would fight it.
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Main Camera") Object.DestroyImmediate(go);

        envRoot = new GameObject("Environment").transform;
        propRoot = new GameObject("Props").transform;
        playRoot = new GameObject("Gameplay").transform;
        enemyRoot = new GameObject("Enemies").transform;

        BuildProfile();
        BuildGround();
        BuildWalls();
        int placed = PlaceFromLayout();
        AddExtras();
        BossGrove();
        WireChestsAndGate();
        TerrainKit.Level1(envRoot, propRoot, occupied, log, HalfX, HalfZ);
        Dress();
        SettleToGround();
        PlaceSystems();
        SetupLighting();
        int navOk = BakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();

        log.AppendLine("placed " + placed + " objects from the 2D layout; navmesh areas=" + navOk);
        log.AppendLine("saved " + ScenePath);
        return log.ToString();
    }

    // --- geometry ---------------------------------------------------------------------

    /// <summary>
    /// B8: decide the shape of the ground before anything is built on it.
    ///
    /// <para><see cref="TerrainKit.ValleyProfile"/> supplies the relief and masks out the
    /// features it owns (the mesa, the spring, the village). The two this file owns are added
    /// here, because this file is what places them.</para>
    /// </summary>
    static void BuildProfile()
    {
        ground = TerrainKit.ValleyProfile(HalfX, HalfZ);

        // A ring of dead trees round a boss fight, tight against two boundary walls.
        ground.Flatten(new Vector2(GroveCentre.x, GroveCentre.z), 5.5f);

        // A reclamation patch blooms into a 9 m disc of clean ground, and the teleport gate's
        // pad is one of them. That disc is a single flat mesh: on a slope it buries its uphill
        // half and floats a visible lip along the downhill one — the classic decal-on-a-hill
        // tell. Level ground under each is far cheaper than conforming geometry, and a healed
        // glade being flat is not something anyone will read as wrong.
        foreach (var line in layout)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = line.Split(',');
            if (f.Length < 4 || f[0] != "patch") continue;
            ground.Flatten(new Vector2(P(f[2]), P(f[3])), 5.2f, 3.5f);
        }
    }

    /// <summary>
    /// The valley floor: 192 tiles, each a generated surface sampled from
    /// <see cref="GroundHeight"/>'s field rather than a flat 4 m slab.
    ///
    /// <para><b>Why generated meshes and not prefab instances.</b> Two requirements pull in
    /// opposite directions. The ground has to be <i>seamless</i> — B8's first acceptance line
    /// is "no step or seam at tile boundaries" — and it has to stay <i>192 separate
    /// renderers</i>, because <c>GroundCleanser.TintGround</c> repaints the ground a renderer
    /// at a time and that is what drives the codex's Độ Sạch metric. A single continuous mesh
    /// satisfies the first and breaks the second. Tiles that each sample one shared height
    /// function satisfy both: two neighbours evaluate the same function at the same world
    /// position along their shared edge, so their vertices land on the same point to the
    /// float, and the tiles remain separate objects.</para>
    ///
    /// <para>Normals come from the field too, analytically, and that is not a detail —
    /// <c>RecalculateNormals</c> averages only the faces present <i>on this tile</i>, so the
    /// same shared vertex gets a different normal from each of its two owners and the grid
    /// reappears as a lighting seam even though the geometry is watertight.</para>
    ///
    /// <para>Everything else about the tiles is unchanged: same 4 m grid, same three earth
    /// tones picked by the same smooth noise (QA C6), same Ground layer, same batching, same
    /// bounds — <c>TintGround</c>'s footprint cap compares 16 m² against 30.7 m² exactly as
    /// before. The <c>BoxCollider</c> becomes a <c>MeshCollider</c>, which is what the
    /// CharacterController walks and what the NavMesh bakes.</para>
    /// </summary>
    static void BuildGround()
    {
        var parent = new GameObject("Ground").transform;
        parent.SetParent(envRoot, false);

        // Three near-identical earths rather than one: 192 tiles sharing a single flat brown made
        // the valley read as a painted plane. That was the intent last time too, and it came out
        // as a checkerboard (QA C6) for two compounding reasons, both fixed here.
        //
        // The spread was 0.035 on the widest channel — 8.6% of the base colour, roughly triple
        // the "few percent" the tiles want; it is +-0.006 now, about 2.9%.
        //
        // And the material was drawn per tile from a die roll, so two neighbours could differ by
        // the whole spread along a dead-straight 4 m seam, which is the one thing natural ground
        // never has. Smooth noise over (i, j) instead puts neighbours in the same band and the
        // grid stops being legible.
        var earths = new[]
        {
            ArtKit.SolidMaterial("FarmGround", new Color(0.420f, 0.380f, 0.260f)),
            ArtKit.SolidMaterial("FarmGround_B", new Color(0.426f, 0.386f, 0.266f)),
            ArtKit.SolidMaterial("FarmGround_C", new Color(0.414f, 0.374f, 0.254f)),
        };
        var soil = new System.Random(20260820);
        float drift = (float)soil.NextDouble() * 128f;   // deterministic, but not on a lattice node

        int layer = LayerMask.NameToLayer("Ground");
        if (layer < 0) log.AppendLine("  WARNING: no Ground layer — the cleanser will not tint");

        int nx = Mathf.RoundToInt(HalfX * 2f / Tile);      // 16 tiles across
        int nz = Mathf.RoundToInt(HalfZ * 2f / Tile);      // 12 tiles deep
        var meshes = new List<Mesh>(nx * nz);
        int n = 0;
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < nz; j++)
            {
                float x = -HalfX + Tile * 0.5f + i * Tile;
                float z = -HalfZ + Tile * 0.5f + j * Tile;

                var t = new GameObject($"Floor_{i}_{j}") { layer = Mathf.Max(0, layer) };
                t.transform.SetParent(parent, false);
                t.transform.position = new Vector3(x, 0f, z);

                var mesh = TileMesh(x, z);
                meshes.Add(mesh);
                t.AddComponent<MeshFilter>().sharedMesh = mesh;
                t.AddComponent<MeshCollider>().sharedMesh = mesh;

                // ~7 tiles per noise period. At 3 the bands still changed at 47% of the 356
                // tile edges and the grid stayed readable, just fainter; this drops it to a
                // quarter, so what is left reads as patches of earth rather than as a lattice.
                float shade = Mathf.PerlinNoise(drift + i * 0.145f, drift + j * 0.145f);
                t.AddComponent<MeshRenderer>().sharedMaterial =
                    shade < 0.44f ? earths[2] : shade < 0.58f ? earths[0] : earths[1];

                GameObjectUtility.SetStaticEditorFlags(t, StaticEditorFlags.BatchingStatic);
                n++;
            }
        }

        SaveMeshes(meshes);

        var (low, high, slope) = MeasureRelief();
        log.AppendLine("ground: " + n + " tiles (" + nx + "x" + nz + " @ " + Tile + " m), " +
                       "3 earth tones within 2.9%, picked by smooth noise");
        log.AppendLine($"  relief: y {low:F2} to {high:F2} m, steepest slope {slope:F1} deg " +
                       $"(CharacterController.slopeLimit is 45), {ground.flat.Count} flat zones");
    }

    /// <summary>
    /// Persist the 192 tile meshes into one asset beside the scene, for the same reason
    /// <see cref="BakeNavMesh"/> does it with the NavMesh: anything a scene references but that
    /// is not an asset gets serialised *into the .unity file*. Left inline they add roughly
    /// 900 KB of vertex YAML, and every rebuild rewrites all of it — which in a repo where three
    /// devs own scenes between them is a diff nobody can read. As one asset the scene keeps 192
    /// plain references and its diff stays about the objects that moved.
    /// </summary>
    static void SaveMeshes(List<Mesh> meshes)
    {
        if (meshes.Count == 0) return;
        const string dir = "Assets/_Scenes/Level1_BarrenFarm";
        Directory.CreateDirectory(dir);
        const string assetPath = dir + "/GroundMesh-Level1.asset";

        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(meshes[0], assetPath);
        for (int i = 1; i < meshes.Count; i++)
            AssetDatabase.AddObjectToAsset(meshes[i], assetPath);
        AssetDatabase.SaveAssets();
        log.AppendLine("  ground meshes saved to " + assetPath);
    }

    /// <summary>Quads per tile edge: 1 m facets, fine enough that a 16 m roll reads as smooth.</summary>
    const int TileSub = 4;

    /// <summary>
    /// One tile's surface, in the tile's own local space. Vertices are sampled at world
    /// coordinates so that neighbouring tiles agree exactly along the edge they share.
    /// </summary>
    static Mesh TileMesh(float cx, float cz)
    {
        const int n = TileSub + 1;
        const float step = Tile / TileSub, half = Tile * 0.5f;

        var verts = new Vector3[n * n];
        var norms = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                float lx = -half + i * step, lz = -half + j * step;
                float wx = cx + lx, wz = cz + lz;
                int k = j * n + i;
                verts[k] = new Vector3(lx, ground.Evaluate(wx, wz), lz);
                norms[k] = ground.NormalAt(wx, wz);
                uvs[k] = new Vector2(wx * 0.25f, wz * 0.25f);
            }
        }

        var tris = new int[TileSub * TileSub * 6];
        int at = 0;
        for (int j = 0; j < TileSub; j++)
        {
            for (int i = 0; i < TileSub; i++)
            {
                int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                tris[at++] = a; tris[at++] = c; tris[at++] = b;   // wound to face +Y
                tris[at++] = b; tris[at++] = c; tris[at++] = d;
            }
        }

        var mesh = new Mesh { name = $"GroundTile_{cx:0.#}_{cz:0.#}" };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// What the relief actually came out as, sampled on a half-metre grid. Logged rather than
    /// guessed, because the number that matters — the steepest slope — has to stay well under
    /// the CharacterController's 45 degree <c>slopeLimit</c> and the NavMesh's own 45 degree
    /// cutoff, and "well under" is a judgement about a measured value, not about a Perlin
    /// gradient somebody reasoned their way to.
    /// </summary>
    static (float low, float high, float slope) MeasureRelief()
    {
        float low = 0f, high = 0f, steepest = 0f;
        for (float x = -HalfX; x <= HalfX; x += 0.5f)
        {
            for (float z = -HalfZ; z <= HalfZ; z += 0.5f)
            {
                float h = ground.Evaluate(x, z);
                low = Mathf.Min(low, h);
                high = Mathf.Max(high, h);
                steepest = Mathf.Max(steepest, Vector3.Angle(Vector3.up, ground.NormalAt(x, z)));
            }
        }
        return (low, high, steepest);
    }

    static void BuildWalls()
    {
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Greybox_Wall.prefab");
        var parent = new GameObject("Walls").transform;
        parent.SetParent(envRoot, false);

        Wall(wall, parent, "Wall_West", new Vector3(-HalfX, WallHeight * 0.5f, 0f), new Vector3(1f, 1f, HalfZ * 2f + 1f));
        Wall(wall, parent, "Wall_East", new Vector3(HalfX, WallHeight * 0.5f, 0f), new Vector3(1f, 1f, HalfZ * 2f + 1f));
        Wall(wall, parent, "Wall_North", new Vector3(0f, WallHeight * 0.5f, HalfZ), new Vector3(HalfX * 2f + 1f, 1f, 1f));
        Wall(wall, parent, "Wall_South", new Vector3(0f, WallHeight * 0.5f, -HalfZ), new Vector3(HalfX * 2f + 1f, 1f, 1f));
    }

    static void Wall(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    // --- everything the 2D scene had --------------------------------------------------

    static readonly List<(Transform t, Vector2 pos2D)> patches = new();
    static readonly List<(Transform t, Vector2 pos2D)> chests = new();
    // Every XZ the layout used, so B5's ground dressing never buries something interactive.
    static readonly List<Vector2> occupied = new();
    static Transform gate;
    static Vector3 playerStart = new Vector3(0f, 0f, -5f);
    static GameObject slimePrefab;
    static int slimeCount;

    static int PlaceFromLayout()
    {
        patches.Clear(); chests.Clear(); occupied.Clear(); gate = null; slimeCount = 0;
        int n = 0;
        foreach (var line in layout)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = line.Split(',');
            string kind = f[0], name = f[1];
            float x = P(f[2]), z = P(f[3]), a = P(f[4]), b = P(f[5]), rotY = P(f[6]);
            var pos = new Vector3(x, 0f, z);

            switch (kind)
            {
                case "prop":
                    // B5: the props are real models now and every one of them pivots on the
                    // floor, so the hand-measured lift the primitives needed is gone.
                    var prop = Spawn("Greybox_" + name, propRoot, pos, rotY);
                    prop.name = name;
                    break;
                case "chest": chests.Add((Spawn("Chest", playRoot, pos).transform, new Vector2(x, z))); break;
                case "patch": patches.Add((Spawn("ReclamationPatch", playRoot, pos).transform, new Vector2(x, z))); break;
                case "gate": gate = Spawn("TeleportGate", playRoot, pos).transform; break;
                case "litter": Spawn("Litter", playRoot, pos).name = name; break;
                case "mud":
                    // continue, not break: skipping the row must also leave its footprint out of
                    // `occupied`, so the dressing pass is free to fill the ground the pool left.
                    if (!ToxicMudEnabled) continue;
                    var mud = Spawn("ToxicMud", playRoot, pos);
                    mud.transform.localScale = new Vector3(a, 1f, b);   // 2D box size → XZ footprint
                    break;
                case "npc_batu": BaTu(pos); break;
                case "npc_ongsau": OngSau(pos); break;
                case "herb": Spawn("Herb", playRoot, ClampInside(pos)).name = name; break;
                case "energydrink": Pickup("energy_drink", "EnergyDrink", pos); break;
                case "player": playerStart = pos; break;
                case "slimespawn": PlaceSlime(name, ClampInside(pos)); break;
                default: continue;
            }
            occupied.Add(new Vector2(x, z));
            n++;
        }
        // The player's start and the four lore notes AddExtras() places are reserved too.
        occupied.Add(new Vector2(playerStart.x, playerStart.z));
        return n;
    }

    // C1: the 29 spawn points from the 2D scene now carry real slimes. Each gets a
    // numbered name because SceneProgress ids objects by name + position, and two of
    // the chest-guards clamp to the same spot inside the west wall.
    static void PlaceSlime(string name, Vector3 pos)
    {
        if (slimePrefab == null)
            slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/PlasticSlime.prefab");

        if (slimePrefab == null)
        {
            log.AppendLine("  MISSING PlasticSlime.prefab — left a bare marker at " + pos);
            var mark = new GameObject(name + "_Spawn");
            mark.transform.SetParent(enemyRoot, false);
            mark.transform.position = pos;
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(slimePrefab, enemyRoot);
        go.name = $"{name}_{++slimeCount:00}";
        go.transform.position = pos;
    }

    static Vector3 ClampInside(Vector3 p) => new(
        Mathf.Clamp(p.x, -HalfX + 2f, HalfX - 2f), p.y, Mathf.Clamp(p.z, -HalfZ + 2f, HalfZ - 2f));

    static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    static GameObject Spawn(string prefabName, Transform parent, Vector3 pos, float rotY = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + prefabName + ".prefab");
        if (prefab == null) { log.AppendLine("  MISSING PREFAB " + prefabName); return new GameObject(prefabName); }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        if (!Mathf.Approximately(rotY, 0f)) go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        occupied.Add(new Vector2(pos.x, pos.z));
        return go;
    }

    static GameObject Pickup(string itemId, string name, Vector3 pos)
    {
        var go = Spawn("ItemPickup", playRoot, pos);
        go.name = name;
        var so = new SerializedObject(go.GetComponent<ItemPickup>());
        so.FindProperty("itemId").stringValue = itemId;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // --- NPCs --------------------------------------------------------------------------

    static void BaTu(Vector3 pos)
    {
        // B5 gave the villagers real models; Bà Tư uses the woman variant so the valley's two
        // quest-givers don't read as the same person.
        var go = Spawn("NPC_VillagerWoman", playRoot, pos);
        go.name = "BaTu";
        var npc = go.AddComponent<DialogueNPC>();
        var so = new SerializedObject(npc);
        SetLines(so.FindProperty("lines"), new[]
        {
            ("Bà Tư", "Ôi, một con robot biết dọn rác sao? Tốt quá... Giếng làng nhiễm độc hết cả rồi, cháu ơi."),
            ("Bà Tư", "Tìm giúp ta 3 Lõi Năng Lượng Sạch giấu trong mấy cái rương gỗ cũ nhé! Mở hết rương thì đất sẽ hồi sinh và cổng dịch chuyển sẽ mở."),
            ("Bà Tư", "Nhớ kỹ: nguồn độc chảy ra từ cái nhà máy Black Smoke. Cẩn thận đấy, cháu!"),
        });
        SetLines(so.FindProperty("postCompletionLines"), new[]
        {
            ("Bà Tư", "Nước giếng trong trở lại rồi! Cảm ơn cháu nhiều lắm."),
            ("Bà Tư", "Nhưng gốc rễ vẫn ở cái nhà máy Black Smoke kia kìa... Bước qua cổng đi, và cẩn thận nghe cháu!"),
        });
        so.FindProperty("autoBriefOnStart").boolValue = true;
        so.FindProperty("briefDelay").floatValue = 0.6f;
        so.FindProperty("swapOnAllCores").boolValue = true;
        so.FindProperty("prompt").objectReferenceValue = go.transform.Find("Prompt").gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void OngSau(Vector3 pos)
    {
        var go = Spawn("NPC_Villager", playRoot, pos);
        go.name = "OngSau";
        var npc = go.AddComponent<QuestGiverNPC>();
        var so = new SerializedObject(npc);
        SetLines(so.FindProperty("offerLines"), new[]
        {
            ("Ông Sáu", "Cứu với! Greenie ơi, có ai không cứu ông với!"),
            ("Greenie", "Có chuyện gì vậy Ông Sáu?"),
            ("Ông Sáu", "Thằng Tí nhà ông nó chui vào khu nhà máy rồi. Chỗ đó khói độc lắm! Nhờ cháu tìm 3 cây Lá Thuốc quanh nông trại để ông chế thuốc giải phòng thân cho nó!"),
        });
        SetLines(so.FindProperty("herbsInProgressLines"), new[]
        {
            ("Ông Sáu", "Nhanh lên cháu ơi, 3 cây Lá Thuốc! Tí nó không trụ được lâu đâu!"),
        });
        SetLines(so.FindProperty("herbsReadyLines"), new[]
        {
            ("Ông Sáu", "Cháu tìm đủ rồi sao? Giỏi quá! Đưa ông xem nào..."),
            ("Ông Sáu", "Đây là Thuốc Giải Mầm Xanh ông vừa nghiền. Cháu hãy cầm lấy, mau qua khu nhà máy tìm Tí giúp ông!"),
        });
        SetLines(so.FindProperty("antidoteHeldLines"), new[]
        {
            ("Ông Sáu", "Cháu còn đứng đó làm gì? Tí đang nguy hiểm lắm, mau qua nhà máy đi!"),
        });
        SetLines(so.FindProperty("tiSavedLines"), new[]
        {
            ("Ông Sáu", "Ông nghe tin cháu đã cứu được Tí rồi. Cảm ơn cháu nhiều lắm Greenie!"),
        });
        so.FindProperty("prompt").objectReferenceValue = go.transform.Find("Prompt").gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BeMay(Vector3 pos)
    {
        var go = Spawn("NPC_VillagerWoman", playRoot, pos);
        go.name = "BeMay";
        var npc = go.AddComponent<SideQuestNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("questId").stringValue = QuestCatalog.MayPet;
        SetLines(so.FindProperty("offerLines"), new[]
        {
            ("Bé Mây", "Huhu... anh Greenie ơi, con Robot cưng của em bị lạc mất rồi!"),
            ("Bé Mây", "Em thấy nó bị dạt về phía góc thung lũng, nơi có mấy con Slime hung dữ lắm... Anh giúp em tìm lại nó với!"),
        });
        SetLines(so.FindProperty("inProgressLines"), new[]
        {
            ("Bé Mây", "Anh tìm thấy Robot cưng của em ở góc thung lũng chưa? Huhu..."),
        });
        SetLines(so.FindProperty("doneLines"), new[]
        {
            ("Bé Mây", "Cảm ơn anh Greenie đã tìm lại Robot cho em! Em sẽ dọn lên Trạm Tái Chế ở với Ông Bear cho an toàn."),
        });
        var promptTr = go.transform.Find("Prompt");
        if (promptTr != null) so.FindProperty("prompt").objectReferenceValue = promptTr.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void OngTai(Vector3 pos)
    {
        var go = Spawn("NPC_Villager", playRoot, pos);
        go.name = "OngTai";
        var npc = go.AddComponent<SideQuestNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("questId").stringValue = QuestCatalog.TaiPond;
        so.FindProperty("rewardItemId").stringValue = "portal_shard";
        so.FindProperty("rewardItemCount").intValue = 1;
        SetLines(so.FindProperty("offerLines"), new[]
        {
            ("Ông Tài", "Chào cháu robot! Cái ao này bị ô nhiễm rác thải nặng quá, lão không câu cá được nữa."),
            ("Ông Tài", "Cháu giúp lão dọn sạch rác quanh ao độc này nhé! Lão sẽ thưởng cho cháu 1 Mảnh Cổng Dịch Chuyển quý giá."),
        });
        SetLines(so.FindProperty("inProgressLines"), new[]
        {
            ("Ông Tài", "Dọn thêm rác quanh ao giúp lão nhé cháu! Đủ rác lão mới câu cá lại được."),
        });
        SetLines(so.FindProperty("doneLines"), new[]
        {
            ("Ông Tài", "Nước ao trong sạch lại rồi! Cảm ơn cháu nhiều lắm, đây là Mảnh Cổng như lão đã hứa."),
        });
        var promptTr = go.transform.Find("Prompt");
        if (promptTr != null) so.FindProperty("prompt").objectReferenceValue = promptTr.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLines(SerializedProperty array, (string speaker, string text)[] lines)
    {
        array.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            var e = array.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
            e.FindPropertyRelative("portrait").objectReferenceValue = null;
        }
    }

    // --- things the 2D level only test-placed ------------------------------------------

    static void AddExtras()
    {
        // Side-quest NPCs (Bé Mây at the village, Ông Tài at the pond)
        BeMay(new Vector3(1.5f, 0f, 18.5f));
        OngTai(new Vector3(-21f, 0f, 10.5f));

        // The 2D build had a single TEST_LoreNote; the valley owns four catalogue notes.
        Note("ln_valley_1", new Vector3(-14f, 0f, 8f));
        Note("ln_valley_2", new Vector3(20f, 0f, 6f));
        Note("ln_valley_3", new Vector3(-20f, 0f, -12f));
        Note("ln_valley_4", new Vector3(9f, 0f, -14f));

        // Support consumables (2D placed these only in its test corner).
        Pickup("spring_water", "SpringWater_1", new Vector3(-16f, 0f, 2f));
        Pickup("spring_water", "SpringWater_2", new Vector3(24f, 0f, -6f));
        Pickup("bottle", "Bottle_1", new Vector3(-8f, 0f, 14f));
        Pickup("scrap", "Scrap_1", new Vector3(12f, 0f, 18f));
    }

    // --- C3: the Slime King's grove -------------------------------------------------------

    /// <summary>
    /// Level 1's mini-boss and the toxic grove it guards.
    ///
    /// <para>This is the one placement in Level 1 that is <b>not</b> ported from the 2D scene,
    /// because the 2D scene has nothing to port: its only Slime King is a <c>TEST_SlimeKing</c>
    /// parked at (-6, -5), six metres from where the player spawns and with half the prefab's
    /// health. That is a test rig, not a level design — a mini-boss you walk into before you
    /// have met Bà Tư.</para>
    ///
    /// <para>So it goes where the level already had a hole. The farm's other three corners
    /// each hold a chest with its own guard slimes; the south-west one is empty, it is 34 m
    /// from the player's start, and the bestiary already says the King <i>"canh giữ một Mảnh
    /// Cổng trong khu rừng độc"</i> — guards a Portal Shard in the toxic grove. A ring of dead
    /// trees with a gap facing the approach, two pools of sludge, and the King in the middle
    /// makes that sentence true and makes the fight a deliberate detour.</para>
    /// </summary>
    static void BossGrove()
    {
        var at = GroveCentre;

        // Ring radius stays under 4.5 m: the boundary wall's inner face is at x = -32 / z = -24
        // and B5's fence posts run just inside that.
        const float ring = 4.2f;
        int trees = 0;
        for (int i = 0; i < 8; i++)
        {
            if (i == 1) continue;               // the north-east gap — the way in
            float a = i * Mathf.PI * 2f / 8f;
            var p = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring;
            Spawn("Greybox_DeadTree", propRoot, p, i * 45f).name = "GroveTree_" + i;
            trees++;
        }

        // The grove's sludge is the same ToxicMud prefab wearing a different name, so it comes
        // and goes with the same switch (see ToxicMudEnabled). Without it the Slime King fights
        // on bare ground, which is a look, not a bug.
        if (ToxicMudEnabled)
            foreach (var (offset, size) in new[]
            {
                (new Vector3(2f, 0f, 2f), new Vector3(3f, 1f, 3f)),
                (new Vector3(-2.2f, 0f, -1.8f), new Vector3(2.5f, 1f, 2.5f)),
            })
            {
                var mud = Spawn("ToxicMud", playRoot, at + offset);
                mud.name = "GroveSludge";
                mud.transform.localScale = size;
            }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/SlimeKing.prefab");
        if (prefab == null)
        {
            log.AppendLine("  MISSING SlimeKing.prefab — grove built without its boss");
            return;
        }
        var king = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemyRoot);
        king.name = "SlimeKing";
        king.transform.position = at;
        occupied.Add(new Vector2(at.x, at.z));
        // Report what was built, not what the code used to build: the sludge count was a
        // literal 2 and went on claiming two pools after ToxicMudEnabled removed them.
        log.AppendLine("  boss grove at " + at + ": SlimeKing, " + trees + " dead trees, " +
                       (ToxicMudEnabled ? "2 sludge pools" : "no sludge (ToxicMudEnabled is off)"));
    }

    static void Note(string id, Vector3 pos)
    {
        var go = Spawn("LoreNote", playRoot, pos);
        go.name = "LoreNote_" + id;
        var so = new SerializedObject(go.GetComponent<LoreNote>());
        so.FindProperty("noteId").stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- wiring ------------------------------------------------------------------------

    static void WireChestsAndGate()
    {
        // Same pairing as the 2D scene: each corner chest heals one patch near the village.
        var pairs = new Dictionary<Vector2, Vector2>
        {
            { new Vector2(-30f, 22f), new Vector2(-10.5f, 5.5f) },   // Patch_0
            { new Vector2(30f, -22f), new Vector2(8.5f, 4.5f) },     // Patch_1
            { new Vector2(30f, 22f), new Vector2(7.5f, -6.5f) },     // Patch_2
        };

        foreach (var (chestT, chestPos) in chests)
        {
            if (!pairs.TryGetValue(chestPos, out var patchPos)) { log.AppendLine("  chest " + chestPos + " unpaired"); continue; }
            var patch = Nearest(patchPos);
            if (patch == null) continue;
            var so = new SerializedObject(chestT.GetComponent<Chest>());
            so.FindProperty("patch").objectReferenceValue = patch.GetComponent<ReclamationPatch>();
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  chest " + chestPos + " -> patch " + patchPos);
        }

        // The gate's pad was a child patch at the gate's own position in 2D.
        if (gate != null)
        {
            var pad = Nearest(new Vector2(gate.position.x, gate.position.z));
            if (pad != null)
            {
                pad.SetParent(gate, true);
                pad.name = "Pad";
                var so = new SerializedObject(gate.GetComponent<TeleportGate>());
                so.FindProperty("pad").objectReferenceValue = pad.GetComponent<ReclamationPatch>();
                // Stage 1 exits to the hub, not straight to the factory.
                so.FindProperty("overrideTargetScene").stringValue = "Shop_RecyclingStation";
                so.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  gate -> pad + overrideTargetScene=Shop_RecyclingStation");
            }
        }
    }

    static Transform Nearest(Vector2 target)
    {
        Transform best = null;
        float bestD = float.MaxValue;
        foreach (var (t, p) in patches)
        {
            float d = Vector2.Distance(p, target);
            if (d < bestD) { bestD = d; best = t; }
        }
        return bestD < 0.5f ? best : null;
    }

    // --- systems ------------------------------------------------------------------------

    /// <summary>
    /// B8: put the whole level back on the ground.
    ///
    /// <para>Every one of the ~1 500 objects in this scene was authored at y = 0 against a flat
    /// plane, across four generator files and a CSV exported from the 2D project. Rather than
    /// thread a height lookup through every placement call — and have the next person who adds
    /// one forget — this drops them all in a single pass, after everything is placed and before
    /// the NavMesh is baked.</para>
    ///
    /// <para>What is deliberately left alone: the ground itself, the boundary walls (the relief
    /// is masked to zero along the perimeter, so there is nothing to follow), and the Terrain
    /// holder — the mesa, the spring and the outer hills all stand on ground that
    /// <see cref="BuildProfile"/> holds flat for exactly that reason.</para>
    /// </summary>
    static void SettleToGround()
    {
        // Flat by nature, so they tilt with the ground: the sludge pools are lying films, and
        // the ground scatter grows out of the surface rather than out of the vertical. Nothing
        // built or grown is in this list — a cottage or a tree leaning 8 degrees reads as a bug
        // rather than as terrain.
        string[] lieFlat = { "ToxicMud", "GroveSludge", "Detail_" };

        int props = TerrainKit.Drop(propRoot, ground, skip: "Village")
                  + TerrainKit.Drop(propRoot.Find("Village"), ground);
        int play = TerrainKit.Drop(playRoot, ground, lieFlat);
        int foes = TerrainKit.Drop(enemyRoot, ground);
        int dress = TerrainKit.Drop(envRoot.Find("Dressing"), ground, lieFlat);

        log.AppendLine($"  settled onto the relief: {props} props, {play} gameplay objects, " +
                       $"{foes} enemies, {dress} pieces of dressing");
    }

    static void PlaceSystems()
    {
        // Greenie starts standing on the ground, whatever height the relief put it at.
        Inst("Assets/Prefabs/Player.prefab", "Greenie",
             playerStart + Vector3.up * (0.7f + ground.Evaluate(playerStart.x, playerStart.z)));
        Inst("Assets/Prefabs/CameraRig.prefab", "CameraRig", Vector3.zero);
        Inst("Assets/Prefabs/HUD.prefab", "HUD", Vector3.zero);

        var gm = Inst("Assets/Prefabs/GameManager.prefab", "GameManager", Vector3.zero);
        if (gm != null)
        {
            var so = new SerializedObject(gm.GetComponent<GameManager>());
            var req = so.FindProperty("requiredCores");
            if (req != null) req.intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  GameManager.requiredCores = 3");
        }

        // The one thing that carries B8 out of the editor: gameplay reads the ground back
        // through GroundHeight, and this is what puts the field there. Nothing else in the
        // project has one, which is why every other scene is provably still flat.
        var field = new GameObject("GroundField");
        field.transform.SetParent(envRoot, false);
        field.AddComponent<GroundHeightField>().Author(ground);
        EditorUtility.SetDirty(field);
        log.AppendLine("  GroundField published (" + ground.flat.Count + " flat zones)");
    }

    static GameObject Inst(string path, string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.AppendLine("  MISSING " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    static void SetupLighting()
    {
        // B5 owns the look now — warm, hazy, washed-out valley light + the post stack.
        log.AppendLine("  " + SceneLook.Apply(SceneLook.Look.Farm, "Assets/_Scenes/Level1_BarrenFarm"));
    }

    // --- B5 dressing ------------------------------------------------------------------------

    /// <summary>
    /// A fence line inside the boundary walls, and a scatter of grass, flowers and stones over
    /// the bare ground. Both are decoration only — no colliders, and nothing is placed within
    /// <c>Clearance</c> of anything the player has to reach.
    /// </summary>
    static void Dress()
    {
        var parent = new GameObject("Dressing").transform;
        parent.SetParent(envRoot, false);

        int posts = 0;
        const float step = 2f;
        for (float x = -HalfX + 1f; x <= HalfX - 1f; x += step)
        {
            posts += Post(parent, new Vector3(x, 0f, -HalfZ + 0.6f), 0f);
            posts += Post(parent, new Vector3(x, 0f, HalfZ - 0.6f), 180f);
        }
        for (float z = -HalfZ + 1f; z <= HalfZ - 1f; z += step)
        {
            posts += Post(parent, new Vector3(-HalfX + 0.6f, 0f, z), 90f);
            posts += Post(parent, new Vector3(HalfX - 0.6f, 0f, z), -90f);
        }

        // Deterministic so a rebuild reproduces the same valley. Weighted toward grass: at the
        // old 320-over-3185-m² density the ground read as one flat brown plane with the odd
        // pebble on it, and only three of the eight models were green.
        var rng = new System.Random(20260812);
        string[] detail =
        {
            ArtKit.Nature + "grass.fbx", ArtKit.Nature + "grass.fbx",
            ArtKit.Nature + "grass_large.fbx", ArtKit.Nature + "grass_large.fbx",
            ArtKit.Nature + "plant_bushSmall.fbx", ArtKit.Nature + "plant_bush.fbx",
            ArtKit.Nature + "stone_smallA.fbx", ArtKit.Nature + "stone_smallFlatA.fbx",
            ArtKit.Nature + "flower_yellowA.fbx", ArtKit.Nature + "flower_redA.fbx",
            ArtKit.Nature + "mushroom_tan.fbx", ArtKit.Nature + "stump_old.fbx",
        };

        int scattered = 0;
        for (int i = 0; i < 900; i++)
        {
            var p = new Vector3(
                (float)(rng.NextDouble() * (HalfX * 2f - 6f) - (HalfX - 3f)), 0f,
                (float)(rng.NextDouble() * (HalfZ * 2f - 6f) - (HalfZ - 3f)));
            if (TooClose(p)) continue;

            var holder = new GameObject("Detail_" + scattered);
            holder.transform.SetParent(parent, false);
            holder.transform.position = p;
            ArtKit.Spawn(detail[rng.Next(detail.Length)], holder.transform, 0f,
                         rotY: (float)(rng.NextDouble() * 360.0));
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
            scattered++;
        }

        log.AppendLine("  dressing: " + posts + " fence posts, " + scattered + " ground details");
    }

    const float Clearance = 2.2f;

    static int Post(Transform parent, Vector3 pos, float rotY)
    {
        var holder = new GameObject("FencePost");
        holder.transform.SetParent(parent, false);
        holder.transform.position = pos;
        var art = ArtKit.Spawn(ArtKit.Nature + "fence_planksDouble.fbx", holder.transform, 0f, rotY);
        GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
        return art != null ? 1 : 0;
    }

    static bool TooClose(Vector3 p)
    {
        foreach (var q in occupied)
            if ((new Vector2(p.x, p.z) - q).sqrMagnitude < Clearance * Clearance) return true;
        return false;
    }

    static int BakeNavMesh()
    {
        var go = new GameObject("NavMesh");
        go.transform.SetParent(envRoot, false);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) return 0;

        // BuildNavMesh() only builds in memory; saving the scene then embeds the mesh
        // as a binary blob, which turns the .unity file binary and defeats Smart Merge.
        // Persist it beside the scene so the scene keeps a plain asset reference.
        const string dir = "Assets/_Scenes/Level1_BarrenFarm";
        Directory.CreateDirectory(dir);
        const string assetPath = dir + "/NavMesh-Level1.asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
        AssetDatabase.SaveAssets();
        log.AppendLine("  navmesh baked to " + assetPath);
        return 1;
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == ScenePath)) return;
        // GameManager.NextLevel is buildIndex + 1, so Level 1 slots in right after MainMenu.
        int at = list.FindIndex(s => s.path.EndsWith("MainMenu.unity"));
        list.Insert(at >= 0 ? at + 1 : list.Count, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        for (int i = 0; i < list.Count; i++) log.AppendLine("  build [" + i + "] " + list[i].path);
    }
}

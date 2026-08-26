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

    // The 2D level's walls sat at x = ±32.5 and y = ±24.5, 1 m thick.
    const float HalfX = 32.5f, HalfZ = 24.5f, WallHeight = 3f, Tile = 4f;

    static StringBuilder log;
    static Transform envRoot, propRoot, playRoot, enemyRoot;

    public static string Execute()
    {
        log = new StringBuilder();
        if (!File.Exists(LayoutCsv)) return "ERROR: " + LayoutCsv + " missing — run export_layout.py first";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // The CameraRig prefab brings its own camera; the template one would fight it.
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Main Camera") Object.DestroyImmediate(go);

        envRoot = new GameObject("Environment").transform;
        propRoot = new GameObject("Props").transform;
        playRoot = new GameObject("Gameplay").transform;
        enemyRoot = new GameObject("Enemies").transform;

        BuildGround();
        BuildWalls();
        int placed = PlaceFromLayout();
        AddExtras();
        BossGrove();
        WireChestsAndGate();
        TerrainKit.Level1(envRoot, propRoot, occupied, log, HalfX, HalfZ);
        Dress();
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

    static void BuildGround()
    {
        var floor = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Greybox_Floor.prefab");
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
        //
        // The tiles still have to stay separate meshes: GroundCleanser repaints them one at a
        // time, and its runtime tint goes through a MaterialPropertyBlock, so it overrides
        // whichever of the three a tile got.
        var earths = new[]
        {
            ArtKit.SolidMaterial("FarmGround", new Color(0.420f, 0.380f, 0.260f)),
            ArtKit.SolidMaterial("FarmGround_B", new Color(0.426f, 0.386f, 0.266f)),
            ArtKit.SolidMaterial("FarmGround_C", new Color(0.414f, 0.374f, 0.254f)),
        };
        var soil = new System.Random(20260820);
        float drift = (float)soil.NextDouble() * 128f;   // deterministic, but not on a lattice node

        int nx = Mathf.RoundToInt(HalfX * 2f / Tile);      // 16 tiles across
        int nz = Mathf.RoundToInt(HalfZ * 2f / Tile);      // 12 tiles deep
        int n = 0;
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < nz; j++)
            {
                float x = -HalfX + Tile * 0.5f + i * Tile;
                float z = -HalfZ + Tile * 0.5f + j * Tile;
                var t = (GameObject)PrefabUtility.InstantiatePrefab(floor, parent);
                t.name = $"Floor_{i}_{j}";
                t.transform.position = new Vector3(x, -0.25f, z);   // slab top sits at y = 0
                if (t.TryGetComponent<Renderer>(out var r))
                {
                    // ~7 tiles per noise period. At 3 the bands still changed at 47% of the 356
                    // tile edges and the grid stayed readable, just fainter; this drops it to a
                    // quarter, so what is left reads as patches of earth rather than as a lattice.
                    float shade = Mathf.PerlinNoise(drift + i * 0.145f, drift + j * 0.145f);
                    r.sharedMaterial = shade < 0.44f ? earths[2] : shade < 0.58f ? earths[0] : earths[1];
                }
                GameObjectUtility.SetStaticEditorFlags(t, StaticEditorFlags.BatchingStatic);
                n++;
            }
        }
        log.AppendLine("ground: " + n + " tiles (" + nx + "x" + nz + " @ " + Tile + " m), " +
                       "3 earth tones within 2.9%, picked by smooth noise");
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
        foreach (var line in File.ReadAllLines(LayoutCsv))
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
        var at = new Vector3(-27f, 0f, -19f);

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
        log.AppendLine("  boss grove at " + at + ": SlimeKing, " + trees + " dead trees, 2 sludge pools");
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

    static void PlaceSystems()
    {
        Inst("Assets/Prefabs/Player.prefab", "Greenie", playerStart + Vector3.up * 0.7f);
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

using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// C1/C2: builds the greybox enemy prefabs (and their materials) from code, the same way
/// <see cref="Level1Builder"/> builds the level — so the kit can be regenerated after a
/// component or stat change instead of being re-dragged by hand. C3 extends this with
/// the bosses.
///
/// Menu: <b>Eco-Dash → Rebuild enemy prefabs</b>. Idempotent.
/// </summary>
public static class EnemyPrefabBuilder
{
    const string PrefabDir = "Assets/Prefabs/Enemies/";
    const string MatDir = "Assets/Models/Materials/";

    // C2: the fly-bot hovers this high, its nozzle hangs this far below the body, and the
    // orb therefore flies at (Hover - Nozzle) ≈ 0.88 m — inside Greenie's 1.15 m capsule.
    // Change one of these and shots start sailing over his head; they belong together.
    const float FlyBotHover = 1.6f;
    const float FlyBotNozzle = 0.72f;

    [MenuItem("Eco-Dash/Rebuild enemy prefabs")]
    public static void Rebuild() => Debug.Log(Execute());

    public static string Execute()
    {
        System.IO.Directory.CreateDirectory(PrefabDir);
        string log = BuildPlasticSlime();
        log += BuildSmogOrb();          // before the fly-bot — the bot references it
        log += BuildPollutionFlyBot();
        log += ExcludePlayerFromNavMeshBake();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return log;
    }

    // --- PlasticSlime -----------------------------------------------------------------

    static string BuildPlasticSlime()
    {
        var body = MakeMaterial("Greybox_Slime", new Color(0.36f, 0.62f, 0.30f), 0.75f);
        var trim = MakeMaterial("Greybox_SlimeTrim", new Color(0.86f, 0.83f, 0.55f), 0.85f);

        var root = new GameObject("PlasticSlime");
        root.tag = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");

        // Body: a squashed sphere reads as a blob at the ¾ camera angle.
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mesh.name = "Body";
        mesh.transform.SetParent(root.transform, false);
        mesh.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        mesh.transform.localScale = new Vector3(0.85f, 0.62f, 0.85f);
        Object.DestroyImmediate(mesh.GetComponent<Collider>());   // the root owns the hitbox
        mesh.GetComponent<Renderer>().sharedMaterial = body;

        // Two shards of rubbish sticking out of it, so a slime is readable as *plastic*
        // trash rather than a generic green ball while the greybox is still standing in.
        Shard(root.transform, "Shard_L", new Vector3(-0.22f, 0.62f, 0.06f), new Vector3(0f, 25f, 38f), trim);
        Shard(root.transform, "Shard_R", new Vector3(0.20f, 0.58f, -0.10f), new Vector3(0f, -40f, -30f), trim);

        var hit = root.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, 0.38f, 0f);
        hit.radius = 0.42f;

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 0.8f;
        agent.baseOffset = 0f;
        agent.speed = 1.5f;              // 2D moveSpeed
        agent.angularSpeed = 720f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
        agent.avoidancePriority = 50;

        // A slime must never carve the NavMesh it walks on: Level1Builder bakes from
        // physics colliders across every layer, and 29 of these would punch 29 holes.
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        var flash = root.AddComponent<HitFlash>();
        var slime = root.AddComponent<PlasticSlime>();
        var so = new SerializedObject(slime);
        so.FindProperty("flash").objectReferenceValue = flash;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "PlasticSlime.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return "built " + path + "\n";
    }

    static void Shard(Transform parent, string name, Vector3 pos, Vector3 euler, Material mat)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = name;
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos;
        s.transform.localRotation = Quaternion.Euler(euler);
        s.transform.localScale = new Vector3(0.30f, 0.04f, 0.16f);
        Object.DestroyImmediate(s.GetComponent<Collider>());
        s.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // --- SmogOrb (C2) -------------------------------------------------------------------

    // The enemies' answer to Seed.prefab, and deliberately built to match it: same
    // trigger-collider + non-kinematic Rigidbody shape, so both projectiles behave
    // identically and only the layer decides who they can hit.
    static string BuildSmogOrb()
    {
        var mat = MakeMaterial("Greybox_SmogOrb", new Color(0.42f, 0.22f, 0.52f), 0.6f,
                               new Color(0.55f, 0.15f, 0.70f));

        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "SmogOrb";
        root.tag = "Projectile";
        root.layer = LayerMask.NameToLayer("EnemyProjectile");
        root.transform.localScale = Vector3.one * 0.36f;
        root.GetComponent<Renderer>().sharedMaterial = mat;

        var col = root.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        root.AddComponent<EnemyProjectile>();

        string path = PrefabDir + "SmogOrb.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return "built " + path + "\n";
    }

    // --- PollutionFlyBot (C2) -----------------------------------------------------------

    static string BuildPollutionFlyBot()
    {
        var body = MakeMaterial("Greybox_FlyBot", new Color(0.30f, 0.28f, 0.37f), 0.55f);
        var trim = MakeMaterial("Greybox_FlyBotTrim", new Color(0.56f, 0.53f, 0.60f), 0.72f);
        var eye = MakeMaterial("Greybox_FlyBotEye", new Color(0.95f, 0.55f, 0.15f), 0.5f,
                               new Color(1.6f, 0.70f, 0.15f));

        var root = new GameObject("PollutionFlyBot");
        root.tag = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");

        // Everything visual lives under Visual: the script bobs and turns that child so
        // the physics body — which owns the hover height — is never yanked around by it.
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);

        Part(visual, "Body", PrimitiveType.Sphere, Vector3.zero, Vector3.zero,
             new Vector3(0.80f, 0.54f, 0.80f), body);
        // Rotor arms + housings, so the silhouette reads as a drone from directly above —
        // which is most of what the ¾ camera ever shows of it.
        for (int s = -1; s <= 1; s += 2)
        {
            string side = s < 0 ? "L" : "R";
            Part(visual, "Arm_" + side, PrimitiveType.Cube, new Vector3(0.28f * s, 0.09f, 0f),
                 Vector3.zero, new Vector3(0.40f, 0.06f, 0.10f), trim);
            Part(visual, "Rotor_" + side, PrimitiveType.Cylinder, new Vector3(0.50f * s, 0.12f, 0f),
                 Vector3.zero, new Vector3(0.34f, 0.03f, 0.34f), trim);
        }
        // The nozzle is not decoration: its tip is where FirePoint sits, and orbs fly flat
        // from there. It is what keeps a hovering shooter's shots at player height.
        Part(visual, "Nozzle", PrimitiveType.Cylinder, new Vector3(0f, -0.42f, 0f),
             Vector3.zero, new Vector3(0.20f, 0.30f, 0.20f), trim);
        Part(visual, "Eye", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.34f),
             Vector3.zero, new Vector3(0.24f, 0.10f, 0.10f), eye);

        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(root.transform, false);          // on the root, not Visual —
        firePoint.localPosition = Vector3.down * FlyBotNozzle; // the bob must not jitter shots

        // Two colliders, and the split is the whole reason the bot is shootable.
        //
        // The SOLID one is small and rides with the body, so the bot bumps walls but
        // sails over the low clutter a ground agent has to walk around — the point of
        // hovering. The TRIGGER one is a column dropped from the body to just above the
        // floor, because Greenie's Seeds fly flat at his fire point (y = 0.6) and a
        // hitbox that started at the body would sit a clear metre above every shot he
        // can take. Height is presentation here; hitting things stays a matter of XZ.
        var solid = root.AddComponent<SphereCollider>();
        solid.radius = 0.45f;

        float localFloor = 0.1f - FlyBotHover;   // hurtbox reaches to 0.1 m above the floor
        const float localTop = 0.5f;             // and up past the top of the body
        var hurtbox = root.AddComponent<CapsuleCollider>();
        hurtbox.isTrigger = true;
        hurtbox.direction = 1;                   // Y
        hurtbox.radius = 0.40f;
        hurtbox.height = localTop - localFloor;
        hurtbox.center = Vector3.up * (localTop + localFloor) * 0.5f;

        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Level builders bake the NavMesh from physics colliders on every layer; a bot
        // parked over the floor would stamp a hole in the mesh the slimes walk on.
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        var flash = root.AddComponent<HitFlash>();
        var bot = root.AddComponent<PollutionFlyBot>();
        var so = new SerializedObject(bot);
        so.FindProperty("flash").objectReferenceValue = flash;
        so.FindProperty("visual").objectReferenceValue = visual;
        so.FindProperty("firePoint").objectReferenceValue = firePoint;
        so.FindProperty("smogOrbPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "SmogOrb.prefab");
        so.FindProperty("hoverHeight").floatValue = FlyBotHover;
        so.FindProperty("sightBlockers").intValue = 1 << LayerMask.NameToLayer("Obstacle");
        so.FindProperty("groundMask").intValue = (1 << LayerMask.NameToLayer("Ground")) |
                                                 (1 << LayerMask.NameToLayer("Obstacle"));
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "PollutionFlyBot.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return "built " + path + "\n";
    }

    static void Part(Transform parent, string name, PrimitiveType type, Vector3 pos,
                     Vector3 euler, Vector3 scale, Material mat)
    {
        var p = GameObject.CreatePrimitive(type);
        p.name = name;
        p.transform.SetParent(parent, false);
        p.transform.localPosition = pos;
        p.transform.localRotation = Quaternion.Euler(euler);
        p.transform.localScale = scale;
        Object.DestroyImmediate(p.GetComponent<Collider>());   // the root owns the hitbox
        p.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // --- shared ------------------------------------------------------------------------

    static Material MakeMaterial(string name, Color color, float smoothness) =>
        MakeMaterial(name, color, smoothness, Color.black);

    static Material MakeMaterial(string name, Color color, float smoothness, Color emission)
    {
        string path = MatDir + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);

        // Every material this builder makes sits on an enemy that HitFlash drives, and
        // HitFlash pushes _EmissionColor through a property block — without the keyword
        // URP strips emission from the shader variant and the flash is silently invisible.
        //
        // RealtimeEmissive even when the resting emission is black, and that is the whole
        // point: under EmissiveIsBlack Unity decides the keyword is pointless and drops it
        // from m_ValidKeywords on the next reserialize, which turns the flash off again
        // days later in a diff that looks like meaningless churn.
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // The player is placed before the bake too, and a CharacterController is a physics
    // collider — without this it punches a hole in the NavMesh at the level's start point.
    static string ExcludePlayerFromNavMeshBake()
    {
        const string path = "Assets/Prefabs/Player.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return "MISSING " + path + "\n";
        if (prefab.GetComponent<NavMeshModifier>() != null) return "player already excluded from the bake\n";

        var contents = PrefabUtility.LoadPrefabContents(path);
        contents.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return "player excluded from the NavMesh bake\n";
    }
}

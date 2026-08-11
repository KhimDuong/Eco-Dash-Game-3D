using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// C1: builds the greybox enemy prefabs (and their materials) from code, the same way
/// <see cref="Level1Builder"/> builds the level — so the kit can be regenerated after a
/// component or stat change instead of being re-dragged by hand. C2/C3 extend this with
/// the fly-bot and the bosses.
///
/// Menu: <b>Eco-Dash → Rebuild enemy prefabs</b>. Idempotent.
/// </summary>
public static class EnemyPrefabBuilder
{
    const string PrefabDir = "Assets/Prefabs/Enemies/";
    const string MatDir = "Assets/Models/Materials/";

    [MenuItem("Eco-Dash/Rebuild enemy prefabs")]
    public static void Rebuild() => Debug.Log(Execute());

    public static string Execute()
    {
        System.IO.Directory.CreateDirectory(PrefabDir);
        string log = BuildPlasticSlime();
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

    // --- shared ------------------------------------------------------------------------

    static Material MakeMaterial(string name, Color color, float smoothness)
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
        // HitFlash pushes _EmissionColor through a property block; without the keyword
        // URP strips emission from the shader variant and the flash is invisible.
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        mat.SetColor("_EmissionColor", Color.black);
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

using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// B3: builds the Level 2 (Factory Maze) greybox prefabs from code, the same way
/// <see cref="EnemyPrefabBuilder"/> builds the enemies — so the kit can be regenerated
/// after a stat or component change instead of being re-dragged by hand.
///
/// Menu: <b>Eco-Dash → Rebuild factory kit</b>. Idempotent.
/// </summary>
public static class FactoryKitBuilder
{
    const string Dir = "Assets/Prefabs/Factory/";
    const string MatDir = "Assets/Models/Materials/";

    [MenuItem("Eco-Dash/Rebuild factory kit")]
    public static void Rebuild() => Debug.Log(Execute());

    public static string Execute()
    {
        System.IO.Directory.CreateDirectory(Dir);
        string log = "";
        log += BuildFloor();
        log += BuildWall();
        log += BuildKeycard();
        log += BuildManholeTrap();
        log += BuildSweepingLaser();
        log += BuildToxicGasZone();
        log += BuildBossDoor();
        log += BuildReturnPortal();
        log += BuildRescueNPC();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // The prefabs above are rebuilt from primitives, so B5's models have to go back on.
        log += ArtPass.ReapplyFactory();
        // ...and C5's clips with them, for exactly the same reason.
        log += AudioPass.ReapplyFactory();
        return log;
    }

    // --- geometry -------------------------------------------------------------------------

    // Level 2's floor and walls are scaled per-instance from the merged tilemap rectangles,
    // so unlike Level 1's fixed 4 m tiles these are unit cubes the builder stretches.
    static string BuildFloor()
    {
        var mat = Mat("Greybox_FactoryFloor", new Color(0.28f, 0.29f, 0.32f), 0.35f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Greybox_FactoryFloor";
        go.layer = LayerMask.NameToLayer("Ground");
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return Save(go, "Greybox_FactoryFloor");
    }

    static string BuildWall()
    {
        var mat = Mat("Greybox_FactoryWall", new Color(0.42f, 0.40f, 0.44f), 0.5f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Greybox_FactoryWall";
        go.layer = LayerMask.NameToLayer("Obstacle");
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return Save(go, "Greybox_FactoryWall");
    }

    // --- interactables ---------------------------------------------------------------------

    static string BuildKeycard()
    {
        var mat = Mat("Greybox_Keycard", new Color(0.95f, 0.82f, 0.25f), 0.7f, new Color(0.9f, 0.6f, 0.1f));
        var root = Interactable("Keycard", 0.8f);

        var card = Part(root.transform, "Card", PrimitiveType.Cube,
                        new Vector3(0f, 0.55f, 0f), new Vector3(0f, 0f, 18f),
                        new Vector3(0.42f, 0.60f, 0.05f), mat);
        Prompt(root.transform, "Nhấn E", 1.4f);

        var kc = root.AddComponent<Keycard>();
        var so = new SerializedObject(kc);
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        so.FindProperty("cardRenderer").objectReferenceValue = card.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "Keycard");
    }

    static string BuildReturnPortal()
    {
        // Its own material, not Level 1's Greybox_Portal: the two portals do opposite
        // things (onward vs back to the hub) and should not read as the same object.
        var mat = Mat("Greybox_ReturnPortal", new Color(0.45f, 0.75f, 1f), 0.8f, new Color(0.2f, 0.5f, 0.9f));
        var root = Interactable("ReturnPortal", 1.0f);

        Part(root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f),
             Vector3.zero, new Vector3(2.0f, 0.05f, 2.0f), Mat("Greybox_FactoryWall", new Color(0.42f, 0.40f, 0.44f), 0.5f));
        Part(root.transform, "Ring", PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0f),
             new Vector3(90f, 0f, 0f), new Vector3(1.6f, 0.08f, 1.6f), mat);
        Prompt(root.transform, "Về Trạm (E)", 2.0f);

        var rp = root.AddComponent<ReturnPortal>();
        var so = new SerializedObject(rp);
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        so.FindProperty("hubScene").stringValue = "Shop_RecyclingStation";
        so.FindProperty("walkOver").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "ReturnPortal");
    }

    static string BuildRescueNPC()
    {
        // Tí awake should read as one of the villagers, so he borrows B1's shared NPC
        // material — borrows, not rewrites. See Borrow() on why that distinction matters.
        var skin = Borrow("Greybox_NPC");
        var sick = Mat("Greybox_TiSick", new Color(0.55f, 0.62f, 0.48f), 0.25f);
        var root = Interactable("RescueNPC_Ti", 0.9f);

        // Slumped vs standing: a pose reads instantly under the ¾ rig, where a texture
        // swap (what the 2D version did) would be almost invisible.
        var down = new GameObject("Visual_Unconscious");
        down.transform.SetParent(root.transform, false);
        Part(down.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.25f, 0f),
             new Vector3(90f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f), sick);

        var up = new GameObject("Visual_Awake");
        up.transform.SetParent(root.transform, false);
        Part(up.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f),
             Vector3.zero, new Vector3(0.5f, 0.6f, 0.5f), skin);
        up.SetActive(false);

        Prompt(root.transform, "Nhấn E", 1.8f);

        var npc = root.AddComponent<RescueNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("unconsciousVisual").objectReferenceValue = down;
        so.FindProperty("awakeVisual").objectReferenceValue = up;
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        SetLines(so.FindProperty("unconsciousLines"), new[]
        {
            ("Tí (Thều thào)", "Khụ khụ... Cứu... Cứu em với... Khó thở quá..."),
        });
        SetLines(so.FindProperty("rescueLines"), new[]
        {
            ("Greenie", "Uống thuốc giải này đi!"),
            ("Tí (Tỉnh lại)", "Cảm ơn anh... Em ổn hơn nhiều rồi. Đây là thẻ từ để vào khu vực lõi nhà máy, anh cầm lấy đi!"),
        });
        SetLines(so.FindProperty("alreadySavedLines"), new[]
        {
            ("Tí", "Cảm ơn anh một lần nữa. Hãy mau ngăn chặn nhà máy xả thải!"),
        });
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "RescueNPC_Ti");
    }

    // --- hazards ----------------------------------------------------------------------------

    static string BuildManholeTrap()
    {
        var lidMat = Mat("Greybox_ManholeLid", new Color(0.38f, 0.36f, 0.34f), 0.45f);
        var holeMat = Mat("Greybox_ManholeHole", new Color(0.05f, 0.05f, 0.07f), 0.1f);

        var root = new GameObject("ManholeTrap");
        root.layer = LayerMask.NameToLayer("Trigger");
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.4f, 0f);
        col.radius = 0.8f;                // B2's walk-over lesson: 0.4 m can be stepped over

        var lid = new GameObject("Lid");
        lid.transform.SetParent(root.transform, false);
        Part(lid.transform, "Plate", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f),
             Vector3.zero, new Vector3(1.3f, 0.05f, 1.3f), lidMat);

        var hole = new GameObject("Hole");
        hole.transform.SetParent(root.transform, false);
        Part(hole.transform, "Pit", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
             Vector3.zero, new Vector3(1.25f, 0.02f, 1.25f), holeMat);
        hole.SetActive(false);

        var trap = root.AddComponent<ManholeTrap>();
        var so = new SerializedObject(trap);
        so.FindProperty("lid").objectReferenceValue = lid;
        so.FindProperty("hole").objectReferenceValue = hole;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "ManholeTrap");
    }

    static string BuildSweepingLaser()
    {
        var mount = Mat("Greybox_LaserMount", new Color(0.30f, 0.30f, 0.34f), 0.6f);
        var beamMat = Mat("Greybox_LaserBeam", new Color(0.6f, 1f, 0.4f), 0.9f, new Color(0.35f, 0.9f, 0.25f));

        var root = new GameObject("SweepingLaser");
        Part(root.transform, "Emitter", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f),
             Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f), mount);

        // The beam is a child stretched along +X; SweepingLaser resizes and positions it
        // from beamLength/Width/Height in Awake, so these numbers are only a preview.
        var beam = new GameObject("Beam");
        beam.transform.SetParent(root.transform, false);
        beam.transform.localPosition = new Vector3(2f, 1.2f, 0f);
        var bar = Part(beam.transform, "Bar", PrimitiveType.Cube, Vector3.zero,
                       Vector3.zero, Vector3.one, beamMat);

        var laser = root.AddComponent<SweepingLaser>();
        var so = new SerializedObject(laser);
        so.FindProperty("beam").objectReferenceValue = beam.transform;
        so.FindProperty("beamRenderer").objectReferenceValue = bar.GetComponent<Renderer>();
        so.FindProperty("hitMask").intValue = 1 << LayerMask.NameToLayer("Player");
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "SweepingLaser");
    }

    static string BuildToxicGasZone()
    {
        var mat = Mat("Greybox_ToxicGas", new Color(0.6f, 0.9f, 0.3f), 0.2f, new Color(0.3f, 0.6f, 0.1f));

        var root = new GameObject("ToxicGasZone");
        root.layer = LayerMask.NameToLayer("Trigger");
        var col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.direction = 1;

        var cloud = new GameObject("Cloud");
        cloud.transform.SetParent(root.transform, false);
        cloud.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        var disc = Part(cloud.transform, "Disc", PrimitiveType.Cylinder, Vector3.zero,
                        Vector3.zero, new Vector3(1f, 0.5f, 1f), mat);

        var gas = root.AddComponent<ToxicGasZone>();
        var so = new SerializedObject(gas);
        so.FindProperty("cloud").objectReferenceValue = cloud.transform;
        so.FindProperty("cloudRenderer").objectReferenceValue = disc.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "ToxicGasZone");
    }

    // --- boss door ------------------------------------------------------------------------

    static string BuildBossDoor()
    {
        var frame = Mat("Greybox_DoorFrame", new Color(0.30f, 0.31f, 0.36f), 0.55f);
        var panelMat = Mat("Greybox_DoorPanel", new Color(0.48f, 0.44f, 0.40f), 0.6f);
        var lightMat = Mat("Greybox_DoorLight", new Color(0.9f, 0.2f, 0.2f), 0.9f, new Color(0.8f, 0.1f, 0.1f));

        var root = new GameObject("BossDoor");

        // The blocker is what actually seals the corridor; the panels are the show.
        var blockerGo = new GameObject("Blocker");
        blockerGo.transform.SetParent(root.transform, false);
        blockerGo.layer = LayerMask.NameToLayer("Obstacle");
        var blocker = blockerGo.AddComponent<BoxCollider>();
        blocker.size = new Vector3(3f, 3f, 0.6f);
        blocker.center = new Vector3(0f, 1.5f, 0f);

        var left = new GameObject("LeftPanel");
        left.transform.SetParent(root.transform, false);
        left.transform.localPosition = new Vector3(-0.75f, 0f, 0f);
        Part(left.transform, "Slab", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f),
             Vector3.zero, new Vector3(1.5f, 2.8f, 0.5f), panelMat);

        var right = new GameObject("RightPanel");
        right.transform.SetParent(root.transform, false);
        right.transform.localPosition = new Vector3(0.75f, 0f, 0f);
        Part(right.transform, "Slab", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f),
             Vector3.zero, new Vector3(1.5f, 2.8f, 0.5f), panelMat);

        Part(root.transform, "Frame_L", PrimitiveType.Cube, new Vector3(-1.75f, 1.5f, 0f),
             Vector3.zero, new Vector3(0.5f, 3.2f, 0.7f), frame);
        Part(root.transform, "Frame_R", PrimitiveType.Cube, new Vector3(1.75f, 1.5f, 0f),
             Vector3.zero, new Vector3(0.5f, 3.2f, 0.7f), frame);

        var lightL = Part(root.transform, "Light_L", PrimitiveType.Cube, new Vector3(-1.75f, 2.6f, 0.36f),
                          Vector3.zero, new Vector3(0.3f, 0.16f, 0.06f), lightMat);
        var lightR = Part(root.transform, "Light_R", PrimitiveType.Cube, new Vector3(1.75f, 2.6f, 0.36f),
                          Vector3.zero, new Vector3(0.3f, 0.16f, 0.06f), lightMat);

        var door = root.AddComponent<BossDoor>();
        var so = new SerializedObject(door);
        so.FindProperty("blocker").objectReferenceValue = blocker;
        so.FindProperty("leftPanel").objectReferenceValue = left.transform;
        so.FindProperty("rightPanel").objectReferenceValue = right.transform;
        var lights = so.FindProperty("lights");
        lights.arraySize = 2;
        lights.GetArrayElementAtIndex(0).objectReferenceValue = lightL.GetComponent<Renderer>();
        lights.GetArrayElementAtIndex(1).objectReferenceValue = lightR.GetComponent<Renderer>();
        so.FindProperty("retract").floatValue = 1.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(root, "BossDoor");
    }

    // --- shared ------------------------------------------------------------------------------

    // Every walk-up interactable follows B2's shape: Trigger layer, generous sphere, and a
    // billboarded "Nhấn E" canvas that PlayerInteractor toggles.
    static GameObject Interactable(string name, float radius)
    {
        var root = new GameObject(name);
        root.layer = LayerMask.NameToLayer("Trigger");
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.5f, 0f);
        col.radius = radius;
        return root;
    }

    static void Prompt(Transform parent, string text, float height)
    {
        var go = new GameObject("Prompt");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);
        go.transform.localScale = Vector3.one * 0.01f;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<Billboard>();

        var label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(400f, 80f);

        go.SetActive(false);
    }

    static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos,
                           Vector3 euler, Vector3 scale, Material mat)
    {
        var p = GameObject.CreatePrimitive(type);
        p.name = name;
        p.transform.SetParent(parent, false);
        p.transform.localPosition = pos;
        p.transform.localRotation = Quaternion.Euler(euler);
        p.transform.localScale = scale;
        Object.DestroyImmediate(p.GetComponent<Collider>());   // the root owns the collider
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
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

    /// <summary>
    /// Use a material this builder does <b>not</b> own, without touching it.
    ///
    /// <para>Mat() is idempotent for materials the factory kit owns, but it is a
    /// <em>writer</em>: pointing it at a name from B1's kit silently repaints Level 1.
    /// That is exactly what happened first time round — the villagers went from blue to
    /// tan and the teleport gate from green to blue, in a commit about Level 2.</para>
    /// </summary>
    static Material Borrow(string name)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");
        if (mat == null) Debug.LogWarning($"[Eco-Dash] factory kit expected {name}.mat to exist already");
        return mat;
    }

    static Material Mat(string name, Color color, float smoothness) => Mat(name, color, smoothness, Color.black);

    static Material Mat(string name, Color color, float smoothness, Color emission)
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

        // Only the glowing ones get the keyword. Enabling _EMISSION on a black-emission
        // material looks harmless but Unity drops it again on the next reserialize, so
        // the asset re-dirties itself for ever and every later commit carries the churn.
        bool glows = emission.maxColorComponent > 0f;
        if (glows) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
        mat.globalIlluminationFlags = glows
            ? MaterialGlobalIlluminationFlags.RealtimeEmissive
            : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static string Save(GameObject root, string name)
    {
        string path = Dir + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return "built " + path + "\n";
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// B5 art pass — the one place that knows how to turn a third-party model file into a
/// visual child this project can use.
///
/// <para>Three things every imported model needs before it can stand in for a greybox:</para>
/// <list type="bullet">
/// <item><b>URP materials.</b> glTFast assigns its own Shader Graph whose colour property is
/// <c>baseColorFactor</c>, not <c>_BaseColor</c>. <see cref="HitFlash"/> and
/// <see cref="MaterialTint"/> both drive <c>_BaseColor</c>, so a raw glTF import makes the
/// hit flash and every tint a silent no-op. Converted materials are written once to
/// <see cref="MatDir"/> and reused.</item>
/// <item><b>A known size and a pivot on the floor.</b> The packs disagree wildly — Kenney's
/// Survival Kit is authored at 0.5 m, its Nature Kit at 1 m, and Quaternius' woman imports
/// 5.6 m tall. Callers ask for a height in metres and get it.</item>
/// <item><b>Nothing that leaks into the scene.</b> The Quaternius flying robot ships two
/// baked-in <b>directional lights</b> (intensity 4.3); one per fly-bot would blow out the
/// whole level. Lights are always stripped.</item>
/// </list>
/// </summary>
public static class ArtKit
{
    public const string Nature = "Assets/Models/ThirdParty/Kenney_NatureKit/";
    public const string Survival = "Assets/Models/ThirdParty/Kenney_SurvivalKit/";
    public const string Factory = "Assets/Models/ThirdParty/Kenney_FactoryKit/";
    public const string Pets = "Assets/Models/ThirdParty/Kenney_CubePets/";
    public const string Quat = "Assets/Models/ThirdParty/Quaternius/";

    const string MatDir = "Assets/Models/Materials/ThirdParty/";
    const string AnimDir = "Assets/Models/ThirdParty/_Animators/";

    static readonly List<string> warnings = new();
    public static IReadOnlyList<string> Warnings => warnings;
    public static void ClearWarnings() => warnings.Clear();

    static void Warn(string message)
    {
        warnings.Add(message);
        Debug.LogWarning("[Eco-Dash art] " + message);
    }

    // --- loading ---------------------------------------------------------------------------

    public static GameObject Load(string assetPath)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (go == null) Warn("model not found: " + assetPath);
        return go;
    }

    /// <summary>Kenney packs ship .fbx, Quaternius (via Poly Pizza) .glb — resolve either.</summary>
    public static string Resolve(string packDir, string name)
    {
        foreach (var ext in new[] { ".fbx", ".glb", ".gltf" })
            if (File.Exists(packDir + name + ext)) return packDir + name + ext;
        Warn("no model file for '" + name + "' under " + packDir);
        return null;
    }

    // --- measuring -------------------------------------------------------------------------

    /// <summary>
    /// World bounds of an instantiated model. Skinned renderers report padded bounds on a
    /// glTF import (the Quaternius slime reads 3.1 m tall when it is really ~1 m), so the
    /// skinned meshes are baked in their current pose and measured for real.
    /// </summary>
    public static Bounds Measure(GameObject instance)
    {
        Bounds? total = null;

        foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh == null) continue;
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            var b = baked.bounds;
            var centre = smr.transform.TransformPoint(b.center);
            var extent = smr.transform.TransformVector(b.size);
            var world = new Bounds(centre,
                new Vector3(Mathf.Abs(extent.x), Mathf.Abs(extent.y), Mathf.Abs(extent.z)));
            total = total == null ? world : Grow(total.Value, world);
            Object.DestroyImmediate(baked);
        }

        foreach (var mr in instance.GetComponentsInChildren<MeshRenderer>())
            total = total == null ? mr.bounds : Grow(total.Value, mr.bounds);

        return total ?? new Bounds(instance.transform.position, Vector3.one);
    }

    static Bounds Grow(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    // --- the main entry point ----------------------------------------------------------------

    /// <summary>
    /// Instantiate <paramref name="assetPath"/> under <paramref name="parent"/>, sized so its
    /// bounding box is <paramref name="height"/> metres tall, pivoted on the floor, and safe
    /// to drop into a scene.
    /// </summary>
    /// <param name="height">Target world height in metres. Pass 0 to keep the import scale.</param>
    /// <param name="lift">Extra world-space Y after grounding (e.g. a hovering pickup).</param>
    /// <param name="idle">Play the model's looping Idle clip, if it shipped one.</param>
    /// <param name="variant">
    /// Names a recoloured copy of the model's materials. Three of the villagers are the same
    /// Quaternius farmer mesh in different clothes — the alternative characters Poly Pizza
    /// serves are atlas-textured and their atlas is not in the .glb, so they import pure white.
    /// </param>
    /// <param name="recolour">Source material name → replacement base colour.</param>
    public static GameObject Spawn(string assetPath, Transform parent, float height,
                                   float rotY = 0f, float lift = 0f, bool idle = false,
                                   string[] hide = null, string variant = null,
                                   (string material, Color colour)[] recolour = null)
    {
        var source = Load(assetPath);
        if (source == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "Art_" + Path.GetFileNameWithoutExtension(assetPath);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // A model's own lights would multiply through every instance placed in a level.
        foreach (var light in go.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(light.gameObject.GetComponent<Light>());

        if (hide != null)
            foreach (string name in hide)
            {
                var child = FindDeep(go.transform, name);
                if (child != null) Object.DestroyImmediate(child.gameObject);
                else Warn($"{Path.GetFileName(assetPath)}: no child '{name}' to hide");
            }

        Retarget(go, assetPath, variant, recolour);

        var animator = go.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (idle)
            {
                var controller = IdleControllerFor(assetPath);
                if (controller != null) animator.runtimeAnimatorController = controller;
                else Object.DestroyImmediate(animator);
            }
            else Object.DestroyImmediate(animator);
        }

        Fit(go, height, rotY, lift);
        return go;
    }

    /// <summary>Scale to a target height, ground the pivot, then rotate about Y.</summary>
    static void Fit(GameObject go, float height, float rotY, float lift)
    {
        if (height > 0f)
        {
            var raw = Measure(go);
            if (raw.size.y > 0.0001f)
            {
                // Measure() is already world-space, so it has the parent's scale baked in.
                // The uniform *world* scale we want is therefore height * parentScale.y /
                // measuredHeight — and only then is the parent divided back out, once, so a
                // non-uniform parent (Player/Visual is 0.7 x 0.55 x 0.7, and the collider
                // depends on that) neither distorts the model nor resizes it. Dividing by the
                // parent without the compensating multiply scales everything by 1/0.55.
                var parentScale = go.transform.parent != null ? go.transform.parent.lossyScale : Vector3.one;
                float world = height * Safe(parentScale.y) / raw.size.y;
                go.transform.localScale = new Vector3(
                    world / Safe(parentScale.x), world / Safe(parentScale.y), world / Safe(parentScale.z));
            }
        }

        var fitted = Measure(go);
        float dy = go.transform.position.y - fitted.min.y + lift;
        go.transform.position += new Vector3(0f, dy, 0f);
        // Centre horizontally: several Kenney models (oopi, the whole factory kit) pivot on a
        // tile corner rather than the mesh centre.
        var after = Measure(go);
        go.transform.position += new Vector3(
            go.transform.position.x - after.center.x, 0f, go.transform.position.z - after.center.z);

        if (!Mathf.Approximately(rotY, 0f))
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
    }

    static float Safe(float f) => Mathf.Approximately(f, 0f) ? 1f : f;

    public static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var hit = FindDeep(child, name);
            if (hit != null) return hit;
        }
        return null;
    }

    // --- materials ----------------------------------------------------------------------------

    /// <summary>
    /// Swap every glTF material for a saved URP/Lit equivalent. Kenney's FBX materials already
    /// import as URP/Lit and are left alone.
    /// </summary>
    static void Retarget(GameObject go, string assetPath, string variant,
                         (string material, Color colour)[] recolour)
    {
        string model = Path.GetFileNameWithoutExtension(assetPath);
        if (!string.IsNullOrEmpty(variant)) model += "_" + variant;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !mats[i].shader.name.Contains("glTF")) continue;
                mats[i] = ToUrp(mats[i], model, recolour);
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    static readonly Dictionary<string, Material> converted = new();

    static Material ToUrp(Material source, string model, (string material, Color colour)[] recolour = null)
    {
        string key = model + "_" + source.name;
        if (converted.TryGetValue(key, out var cached) && cached != null) return cached;

        Directory.CreateDirectory(MatDir);
        string path = MatDir + Sanitise(key) + ".mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { converted[key] = existing; return existing; }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Sanitise(key) };

        if (source.HasProperty("baseColorFactor"))
            mat.SetColor("_BaseColor", source.GetColor("baseColorFactor"));
        if (source.HasProperty("baseColorTexture"))
        {
            var tex = source.GetTexture("baseColorTexture");
            if (tex != null) mat.SetTexture("_BaseMap", tex);
        }
        if (source.HasProperty("metallic")) mat.SetFloat("_Metallic", source.GetFloat("metallic"));
        if (source.HasProperty("roughness"))
            mat.SetFloat("_Smoothness", 1f - source.GetFloat("roughness"));

        if (recolour != null)
            foreach (var (name, colour) in recolour)
                if (name == source.name) mat.SetColor("_BaseColor", colour);

        AssetDatabase.CreateAsset(mat, path);
        converted[key] = mat;
        return mat;
    }

    /// <summary>
    /// Turn on emission so <see cref="HitFlash"/> has something to drive. URP strips emission
    /// from the shader variant when the keyword is off, and Unity drops the keyword again on
    /// reserialize unless the GI flag says the emission is not black — both of which make the
    /// flash silently invisible.
    /// </summary>
    public static void MakeFlashable(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", Color.black);
                EditorUtility.SetDirty(mat);
            }
    }

    /// <summary>A saved URP/Lit material, created on first use. Used for floors, walls and tints.</summary>
    public static Material SolidMaterial(string name, Color colour, float smoothness = 0f,
                                         Color? emission = null)
    {
        Directory.CreateDirectory(MatDir);
        string path = MatDir + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool fresh = mat == null;
        if (fresh) mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };

        mat.SetColor("_BaseColor", colour);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emission.Value);
        }

        if (fresh) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>A material that reuses a pack's colour atlas but re-tints it (Ông Bear is a
    /// polar bear painted brown).</summary>
    public static Material TintedFrom(string name, string texturePath, Color tint)
    {
        var mat = SolidMaterial(name, tint);
        var tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        if (tex == null) Warn("texture not found: " + texturePath);
        else mat.SetTexture("_BaseMap", tex);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static string Sanitise(string s) =>
        string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));

    // --- idle animation ---------------------------------------------------------------------------

    /// <summary>
    /// Build (once) a one-state controller playing the model's Idle clip. The characters and
    /// enemies ship a full clip set but no controller, so without this they render in bind pose.
    /// </summary>
    static AnimatorController IdleControllerFor(string assetPath)
    {
        string model = Path.GetFileNameWithoutExtension(assetPath);
        Directory.CreateDirectory(AnimDir);
        string path = AnimDir + model + "_Idle.controller";

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        var clips = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__")).ToArray();
        // "Armature|Slime_Idle", "CharacterArmature|Idle" — match the tail, prefer an exact Idle.
        var clip = clips.FirstOrDefault(c => Tail(c.name).Equals("Idle", System.StringComparison.OrdinalIgnoreCase))
                ?? clips.FirstOrDefault(c => Tail(c.name).EndsWith("Idle", System.StringComparison.OrdinalIgnoreCase));
        if (clip == null) { Warn(model + ": no Idle clip to loop"); return null; }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (!settings.loopTime)
        {
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
        return controller;
    }

    static string Tail(string clipName)
    {
        int bar = clipName.LastIndexOf('|');
        return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
    }
}

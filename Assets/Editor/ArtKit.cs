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
/// <item><b>Colours the pack did not actually mean.</b> Kenney's Nature Kit is the one pack
/// here that ships <i>no</i> texture — every model is flat-shaded off its material colour —
/// and the colours baked into its FBX files are a washed-out pastel set:
/// <c>leafsGreen</c> imports as turquoise (0.44, 0.90, 0.84), <c>dirt</c> and <c>stone</c>
/// as near-white. That is why Level 1's grass and rocks rendered cyan.
/// <see cref="NaturePalette"/> repaints all 23 of them, once, into shared materials.</item>
/// </list>
/// </summary>
public static class ArtKit
{
    public const string Nature = "Assets/Models/ThirdParty/Kenney_NatureKit/";
    public const string Survival = "Assets/Models/ThirdParty/Kenney_SurvivalKit/";
    public const string Factory = "Assets/Models/ThirdParty/Kenney_FactoryKit/";
    public const string Pets = "Assets/Models/ThirdParty/Kenney_CubePets/";
    public const string Town = "Assets/Models/ThirdParty/Kenney_FantasyTownKit/";
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

    /// <summary>
    /// Instantiate a model at a plain uniform scale, keeping <b>its own pivot</b> — no
    /// grounding, no centring.
    ///
    /// <para><see cref="Spawn"/> is for swapping one greybox mesh for one model, so it centres
    /// what it places. A modular kit is the opposite: Fantasy Town's wall panel deliberately
    /// sits on the −X edge of its 1 m cell so that four of them, rotated 0/90/180/270°, enclose
    /// the cell. Centring each one would stack all four in the middle of the house.</para>
    /// </summary>
    /// <param name="cell">Offset from the parent, already in world metres.</param>
    public static GameObject SpawnModule(string assetPath, Transform parent, float scale,
                                         float rotY = 0f, Vector3 cell = default)
    {
        var source = Load(assetPath);
        if (source == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "Art_" + Path.GetFileNameWithoutExtension(assetPath);

        foreach (var light in go.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(light.gameObject.GetComponent<Light>());
        foreach (var animator in go.GetComponentsInChildren<Animator>(true))
            Object.DestroyImmediate(animator);

        Retarget(go, assetPath, null, null);

        go.transform.localPosition = cell;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = Vector3.one * scale;
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

        // Turn before measuring. Grounding and centring both read world-space bounds, and a
        // model whose pivot is not its own centre orbits that pivot as it turns — so a centring
        // done first is simply undone by the turn, leaving the art displaced by R*d - d. That is
        // how Greenie ended up 1.8 m from his own CharacterController, orbiting it as
        // PlayerController swung Visual toward travel (QA E10).
        if (!Mathf.Approximately(rotY, 0f))
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        var fitted = Measure(go);
        float dy = go.transform.position.y - fitted.min.y + lift;
        go.transform.position += new Vector3(0f, dy, 0f);
        // Centre horizontally: several Kenney models (oopi, the whole factory kit) pivot on a
        // tile corner rather than the mesh centre.
        var after = Measure(go);
        go.transform.position += new Vector3(
            go.transform.position.x - after.center.x, 0f, go.transform.position.z - after.center.z);
    }

    static float Safe(float f) => Mathf.Approximately(f, 0f) ? 1f : f;

    // --- solidity -----------------------------------------------------------------------------

    /// <summary>
    /// Give a spawned visual the collider it never had.
    ///
    /// <para><see cref="Spawn"/> and <see cref="SpawnModule"/> place a <i>visual</i> and nothing
    /// else. That is right when the visual is swapped onto a greybox prefab that already carries
    /// the gameplay collider, and wrong everywhere a generator spawns art directly: the hub yard's
    /// 25 props, Level 1's three village lanterns and the beached canoe were all ghosts you could
    /// walk through (QA C4/C7), and the mesa's ragged silhouette was traced by one axis-aligned
    /// box that stood in 6.5 m² of open ground (QA C3).</para>
    ///
    /// <para>The box is fitted to the art's real bounds and lives on <paramref name="holder"/>,
    /// not on the art, so the next art pass can replace the model without touching the physics.
    /// Returns null when the prop is too small to be worth stopping — walking through a tuft of
    /// grass is correct.</para>
    /// </summary>
    /// <remarks>
    /// A turned prop should be turned by its <b>holder</b>, not by <see cref="Spawn"/>'s
    /// <c>rotY</c>: the box is fitted in the holder's own frame, so a holder rotation gives an
    /// oriented box while a rotation baked into the art only gives its bounding rectangle. The
    /// beached canoe is a 3.4 x 1.0 m boat lying at 25°, which is a 2.3 x 3.5 m box the wrong
    /// way — twice the footprint, right where the player now walks along the bank.
    /// </remarks>
    /// <param name="grounded">Stretch the box down to y = 0 so nothing can be walked under.</param>
    /// <param name="minHeight">Props shorter than this stay walk-through.</param>
    /// <param name="maxHalfExtent">Clamp the XZ half-size (keeps an interactable inside its own trigger).</param>
    public static BoxCollider Solidify(GameObject holder, GameObject art, string layer = "Obstacle",
                                       bool grounded = true, float minHeight = 0.5f,
                                       float maxHalfExtent = 0f)
    {
        if (holder == null || art == null) return null;

        // Measure() reports world-axis-aligned bounds, so a turned holder has to be squared up
        // first or the box comes out as the prop's bounding rectangle rather than its footprint.
        var t = holder.transform;
        var turned = t.localRotation;
        bool spun = turned != Quaternion.identity;
        if (spun) t.localRotation = Quaternion.identity;

        var world = Measure(art);
        var scale = t.lossyScale;
        var centre = t.InverseTransformPoint(world.center);
        var size = new Vector3(world.size.x / Safe(scale.x),
                               world.size.y / Safe(scale.y),
                               world.size.z / Safe(scale.z));

        if (spun) t.localRotation = turned;
        if (world.size.y < minHeight) return null;

        if (maxHalfExtent > 0f)
        {
            size.x = Mathf.Min(size.x, maxHalfExtent * 2f);
            size.z = Mathf.Min(size.z, maxHalfExtent * 2f);
        }

        if (grounded)
        {
            // The holder sits on the ground, so local y = 0 is the floor: grow the box down to it.
            float top = centre.y + size.y * 0.5f;
            size.y = Mathf.Max(top, minHeight);
            centre.y = size.y * 0.5f;
        }

        int id = LayerMask.NameToLayer(layer);
        if (id >= 0) holder.layer = id; else Warn("no layer named '" + layer + "'");

        var box = holder.AddComponent<BoxCollider>();
        box.center = centre;
        box.size = size;
        return box;
    }

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
    /// Swap every glTF material for a saved URP/Lit equivalent, and repaint the Nature Kit's
    /// pastel FBX materials off <see cref="NaturePalette"/>. The other Kenney packs carry a
    /// <c>colormap</c> texture and are left alone.
    /// </summary>
    static void Retarget(GameObject go, string assetPath, string variant,
                         (string material, Color colour)[] recolour)
    {
        string model = Path.GetFileNameWithoutExtension(assetPath);
        if (!string.IsNullOrEmpty(variant)) model += "_" + variant;
        bool untextured = assetPath.StartsWith(Nature) || assetPath.StartsWith(Town);

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader.name.Contains("glTF"))
                {
                    mats[i] = ToUrp(mats[i], model, recolour);
                    changed = true;
                }
                else if (untextured)
                {
                    var repainted = Repaint(mats[i], variant, recolour);
                    if (repainted == null) continue;
                    mats[i] = repainted;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    /// <summary>
    /// The intended look of the two packs whose colour lives in materials rather than in a
    /// texture, re-authored. The Nature Kit's own values are the ones the pack's preview renders
    /// show — green foliage, brown bark and earth, grey stone — none of which survived into its
    /// FBX materials. The Fantasy Town Kit is textured except for its one untextured
    /// <c>Water</c> material, which imports near-white and turns the fountain into a metal pad.
    /// Anything not listed here is left as the pack imported it.
    /// </summary>
    static readonly Dictionary<string, (Color colour, float smoothness)> NaturePalette = new()
    {
        { "Water",        (new Color(0.22f, 0.52f, 0.68f), 0.90f) },   // Fantasy Town's fountain
        { "grass",        (new Color(0.38f, 0.58f, 0.27f), 0f) },
        { "leafsGreen",   (new Color(0.33f, 0.54f, 0.24f), 0f) },
        { "leafsDark",    (new Color(0.21f, 0.37f, 0.19f), 0f) },
        { "leafsFall",    (new Color(0.74f, 0.44f, 0.15f), 0f) },
        { "dirt",         (new Color(0.47f, 0.35f, 0.23f), 0f) },
        { "dirtDark",     (new Color(0.34f, 0.25f, 0.16f), 0f) },
        { "stone",        (new Color(0.44f, 0.45f, 0.46f), 0f) },
        { "stoneDark",    (new Color(0.32f, 0.33f, 0.35f), 0f) },
        { "wood",         (new Color(0.58f, 0.41f, 0.25f), 0f) },
        { "woodDark",     (new Color(0.40f, 0.28f, 0.17f), 0f) },
        { "woodBark",     (new Color(0.42f, 0.30f, 0.19f), 0f) },
        { "woodBarkDark", (new Color(0.28f, 0.20f, 0.14f), 0f) },
        { "woodBirch",    (new Color(0.80f, 0.77f, 0.70f), 0f) },
        { "woodInner",    (new Color(0.70f, 0.53f, 0.33f), 0f) },
        { "water",        (new Color(0.18f, 0.45f, 0.62f), 0.85f) },
        { "colorRed",     (new Color(0.70f, 0.20f, 0.17f), 0f) },
        { "colorRedDark", (new Color(0.48f, 0.12f, 0.11f), 0f) },
        { "colorPurple",  (new Color(0.47f, 0.30f, 0.63f), 0f) },
        { "colorYellow",  (new Color(0.83f, 0.68f, 0.18f), 0f) },
        { "colorTan",     (new Color(0.71f, 0.57f, 0.36f), 0f) },
        { "colorWhite",   (new Color(0.85f, 0.84f, 0.79f), 0f) },
        { "corn",         (new Color(0.80f, 0.66f, 0.26f), 0f) },
        { "_defaultMat",  (new Color(0.53f, 0.54f, 0.55f), 0f) },
    };

    /// <summary>
    /// A shared repaint of one Nature Kit material, or null to leave it alone. The cache key is
    /// the material name — <b>not</b> the model — so all 300 Nature Kit models keep sharing one
    /// "grass" and one "stone" and stay in a single batch. A caller-supplied
    /// <paramref name="recolour"/> asks for a private copy instead, keyed by variant.
    /// </summary>
    static Material Repaint(Material source, string variant,
                            (string material, Color colour)[] recolour)
    {
        Color? overridden = null;
        if (recolour != null)
            foreach (var (name, colour) in recolour)
                if (name == source.name) overridden = colour;

        if (overridden == null && !NaturePalette.ContainsKey(source.name)) return null;

        var entry = NaturePalette.TryGetValue(source.name, out var p) ? p : (colour: Color.white, smoothness: 0f);
        var final = overridden ?? entry.colour;
        // A recolour must never write through to the shared entry, or the one prop that asked
        // for a private colour repaints every other model that uses that material.
        string key = overridden == null
            ? "Nature_" + source.name
            : "Nature_" + (string.IsNullOrEmpty(variant)
                ? ColorUtility.ToHtmlStringRGB(final) : variant) + "_" + source.name;

        if (converted.TryGetValue(key, out var cached) && cached != null) return cached;
        var mat = SolidMaterial(Sanitise(key), final, entry.smoothness);
        converted[key] = mat;
        return mat;
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

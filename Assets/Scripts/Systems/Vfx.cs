using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// C4's one-shot particle bursts — the death poofs, cleaning sparkle and impact chips
/// that give a hit its weight. Every burst is <b>built from code</b> and destroys itself
/// when it stops, exactly like <see cref="UIFactory"/> builds the windows and
/// <see cref="BossHealthBar"/> builds itself: there is no VFX prefab to place, nothing to
/// wire per scene, and no generator that could throw the wiring away on its next run.
///
/// <para><b>The particles are little meshes, not billboards</b>, for two reasons. The
/// game is low-poly, so tumbling cubes read as debris where a soft round puff would look
/// borrowed from another project. And a billboard needs one of the URP <i>Particles</i>
/// shaders, which no material in this project references — <see cref="Shader.Find"/> can
/// only return what a build actually kept, so that puff would be a magenta square in the
/// submission build while looking perfect in the editor. URP/Lit is on every material in
/// the game and is therefore always there.</para>
///
/// <para>Per-particle colour comes from a <b>small material cache keyed on the colour</b>
/// rather than from <c>startColor</c>: mesh particles only carry their colour into the
/// shader if the shader reads the vertex-colour stream, and URP/Lit does not. There are a
/// handful of tints in the whole game (one per enemy, plus clean-green and chip-white), so
/// the cache stays tiny — and each material carries matching emission, which is what
/// actually reads under the fixed ¾ camera. Same lesson as
/// <see cref="HitFlash"/>: a tint swap barely registers, a lit blob pops.</para>
/// </summary>
public static class Vfx
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    static readonly Dictionary<int, Material> materials = new Dictionary<int, Material>();
    static Mesh chunkMesh;
    static Shader litShader;
    static bool shaderMissing;

    /// <summary>The green a cleansed patch of ground sparkles in (M9's "Độ Sạch" payoff).</summary>
    public static readonly Color CleanGreen = new Color(0.44f, 0.85f, 0.36f);

    // Fast Enter Play Mode does not reload the domain, so the cache would otherwise carry
    // materials destroyed with the last play session into this one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { materials.Clear(); chunkMesh = null; shaderMissing = false; }

    // --- the three bursts -------------------------------------------------------------

    /// <summary>
    /// Death poof: a body bursting into chunks that arc out and fall. <paramref name="scale"/>
    /// is a rough "how big was the thing" multiplier — 1 for an ordinary enemy, ~2 for a boss.
    /// </summary>
    public static void Poof(Vector3 at, Color tint, float scale = 1f)
    {
        int count = Mathf.RoundToInt(14f * Mathf.Max(1f, scale));
        var ps = Build("Vfx_Poof", at, tint, count);
        if (ps == null) return;

        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.65f * scale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f * scale, 4.4f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.17f * scale);
        main.gravityModifier = 1.6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f * scale;

        Finish(ps);
    }

    /// <summary>
    /// The cleaning sparkle: motes lifting off a patch of ground that has just been cleared
    /// of trash. They rise instead of falling, which is the whole read — the pollution is
    /// leaving rather than being knocked loose.
    /// </summary>
    public static void CleanBurst(Vector3 at, float radius)
    {
        var ps = Build("Vfx_Clean", at + Vector3.up * 0.1f, CleanGreen, 20);
        if (ps == null) return;

        var main = ps.main;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
        main.gravityModifier = -0.35f;   // float upward and fade

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.3f, radius * 0.8f);
        shape.radiusThickness = 1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // a disc lying on the ground plane

        Finish(ps);
    }

    /// <summary>
    /// Impact chips where a Seed lands — a handful of flecks thrown back along the shot,
    /// so a hit that kills nothing still tells the player it connected.
    /// </summary>
    public static void Impact(Vector3 at, Vector3 travelDir, Color tint)
    {
        var ps = Build("Vfx_Impact", at, tint, 7);
        if (ps == null) return;

        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.gravityModifier = 1.2f;

        travelDir.y = 0f;
        var back = travelDir.sqrMagnitude > 0.0001f ? -travelDir.normalized : Vector3.back;
        ps.transform.rotation = Quaternion.LookRotation(back + Vector3.up * 0.4f, Vector3.up);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = 0.05f;

        Finish(ps);
    }

    // --- colour ------------------------------------------------------------------------

    /// <summary>
    /// Best-effort "what colour is this thing", so an enemy poofs in its own colour without
    /// carrying a serialized tint. Reading it live means the art pass owns the answer:
    /// recolour a slime in <c>ArtPass</c> and its death poof follows on the next run, with no
    /// prefab to rebuild.
    /// </summary>
    public static Color ColorOf(GameObject source, Color fallback)
    {
        if (source == null) return fallback;
        foreach (var r in source.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            var c = MaterialTint.Read(r);
            if (c.r + c.g + c.b > 0.05f) return c;
        }
        return fallback;
    }

    // --- construction -------------------------------------------------------------------

    // Built inactive and switched on at the end: a ParticleSystem added to a live object
    // starts playing on the spot, and would emit one frame's worth with the default module
    // settings before any of ours land.
    static ParticleSystem Build(string name, Vector3 at, Color tint, int count)
    {
        if (!Application.isPlaying) return null;
        var mat = MaterialFor(tint);
        if (mat == null) return null;   // shader stripped — go without the effect, never throw

        var go = new GameObject(name);
        go.SetActive(false);
        go.transform.position = at;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startColor = tint;                                   // free if a particle shader ever lands
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.stopAction = ParticleSystemStopAction.Destroy;        // self-cleaning; no Destroy timer

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Shrink());

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = ChunkMesh();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return ps;
    }

    static void Finish(ParticleSystem ps) => ps.gameObject.SetActive(true);

    // One cube, borrowed off a throwaway primitive. Unity has no public "give me the
    // built-in cube" API, and every burst shares this one mesh.
    static Mesh ChunkMesh()
    {
        if (chunkMesh != null) return chunkMesh;
        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chunkMesh = probe.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(probe);
        return chunkMesh;
    }

    static AnimationCurve Shrink() => AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f);

    static Material MaterialFor(Color tint)
    {
        if (shaderMissing) return null;
        if (litShader == null)
        {
            litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                shaderMissing = true;
                Debug.LogWarning("[Eco-Dash] URP/Lit not found — VFX bursts are disabled.");
                return null;
            }
        }

        int key = Key(tint);
        if (materials.TryGetValue(key, out var cached) && cached != null) return cached;

        var mat = new Material(litShader) { name = "VfxChunk_" + key, hideFlags = HideFlags.DontSave };
        if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tint);
        if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, tint);
        if (mat.HasProperty(EmissionId))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionId, tint * 0.55f);
        }
        materials[key] = mat;
        return mat;
    }

    // Quantised so near-identical tints share one material instead of growing the cache.
    static int Key(Color c) =>
        (Mathf.RoundToInt(c.r * 16f) << 10) | (Mathf.RoundToInt(c.g * 16f) << 5) | Mathf.RoundToInt(c.b * 16f);
}

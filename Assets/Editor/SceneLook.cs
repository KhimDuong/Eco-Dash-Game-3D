using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// B5: the per-scene lighting and post-processing look, so each level reads as its own place —
/// Level 1 warm and hazy, Level 2 dark and industrial, the hub clean and neutral.
///
/// <para>Called by the level builders, so a scene rebuild keeps the look. The volume profile is
/// a real asset next to the scene rather than a scene-embedded one: an embedded profile turns
/// into inline YAML that Smart Merge cannot resolve.</para>
///
/// <para>Post-processing also has to be switched on <b>per camera</b> — a URP camera ignores
/// every volume in the scene unless <c>renderPostProcessing</c> is set, which is easy to miss
/// because the volume itself looks correctly configured.</para>
/// </summary>
public static class SceneLook
{
    public enum Look { Farm, Factory, Hub }

    public static string Apply(Look look, string profileDir)
    {
        var sun = Sun(look);
        Ambient(look);
        Sky(look, profileDir, sun);
        var profile = Profile(look, profileDir);

        var go = GameObject.Find("PostProcessing") ?? new GameObject("PostProcessing");
        var volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.sharedProfile = profile;

        EnableOnCamera();
        return $"look={look}, sun={sun.color} @{sun.intensity:F2}, profile={AssetDatabase.GetAssetPath(profile)}";
    }

    static Light Sun(Look look)
    {
        var existing = Object.FindFirstObjectByType<Light>();
        Light sun = existing != null && existing.type == LightType.Directional ? existing : null;
        if (sun == null)
        {
            var go = GameObject.Find("Directional Light") ?? new GameObject("Directional Light");
            sun = go.GetComponent<Light>() ?? go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        switch (look)
        {
            case Look.Farm:      // low, warm, dusty — a valley the smoke has bleached
                sun.transform.SetPositionAndRotation(new Vector3(0f, 12f, 0f), Quaternion.Euler(48f, -35f, 0f));
                sun.color = new Color(1f, 0.93f, 0.78f);
                sun.intensity = 1.15f;
                break;
            case Look.Factory:   // hard, cold, almost overhead — strip lighting in a plant
                sun.transform.SetPositionAndRotation(new Vector3(0f, 12f, 0f), Quaternion.Euler(70f, 25f, 0f));
                sun.color = new Color(0.72f, 0.78f, 0.95f);
                sun.intensity = 0.55f;
                break;
            case Look.Hub:       // bright and even; the one safe room in the game
                sun.transform.SetPositionAndRotation(new Vector3(0f, 12f, 0f), Quaternion.Euler(55f, -20f, 0f));
                sun.color = new Color(1f, 0.97f, 0.92f);
                sun.intensity = 1.25f;
                break;
        }
        sun.shadows = LightShadows.Soft;
        return sun;
    }

    static void Ambient(Look look)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        switch (look)
        {
            case Look.Farm:
                RenderSettings.ambientSkyColor = new Color(0.55f, 0.55f, 0.50f);
                RenderSettings.ambientEquatorColor = new Color(0.42f, 0.40f, 0.35f);
                RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.72f, 0.69f, 0.58f);
                RenderSettings.fogDensity = 0.012f;
                break;
            case Look.Factory:
                RenderSettings.ambientSkyColor = new Color(0.22f, 0.25f, 0.32f);
                RenderSettings.ambientEquatorColor = new Color(0.16f, 0.17f, 0.21f);
                RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.09f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.14f, 0.15f, 0.18f);
                RenderSettings.fogDensity = 0.020f;
                break;
            case Look.Hub:
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.63f, 0.66f);
                RenderSettings.ambientEquatorColor = new Color(0.48f, 0.48f, 0.50f);
                RenderSettings.ambientGroundColor = new Color(0.28f, 0.27f, 0.26f);
                RenderSettings.fog = false;
                break;
        }
    }

    /// <summary>
    /// The sky. All three scenes shipped on Unity's default skybox, which is the same blue
    /// everywhere and belongs to none of them — most visible in Level 1, where the valley now
    /// has hills to see over its walls and so has a horizon at all.
    ///
    /// <para>No asset is sourced for this: Unity's built-in <c>Skybox/Procedural</c> shader
    /// takes a tint, a ground colour, an atmosphere thickness and an exposure, which is enough
    /// to put a bleached smoggy haze over the farm and a cold overcast over the factory. The
    /// material is written next to the scene, like the volume profile, so it survives a
    /// rebuild and Smart Merge can still read the scene file.</para>
    /// </summary>
    static void Sky(Look look, string dir, Light sun)
    {
        Directory.CreateDirectory(dir);
        string path = dir + "/Sky_" + look + ".mat";

        var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, path);
        }

        switch (look)
        {
            case Look.Farm:      // smog held in the valley: yellow-grey, sun burning through it
                sky.SetColor("_SkyTint", new Color(0.62f, 0.58f, 0.45f));
                sky.SetColor("_GroundColor", new Color(0.32f, 0.28f, 0.22f));
                sky.SetFloat("_AtmosphereThickness", 1.75f);
                sky.SetFloat("_Exposure", 1.15f);
                sky.SetFloat("_SunSize", 0.06f);
                sky.SetFloat("_SunDisk", 2f);
                break;
            case Look.Factory:   // a lid of cold cloud over the plant; no sun disk to find
                sky.SetColor("_SkyTint", new Color(0.34f, 0.38f, 0.46f));
                sky.SetColor("_GroundColor", new Color(0.10f, 0.10f, 0.12f));
                sky.SetFloat("_AtmosphereThickness", 0.85f);
                sky.SetFloat("_Exposure", 0.70f);
                sky.SetFloat("_SunSize", 0.02f);
                sky.SetFloat("_SunDisk", 0f);
                break;
            case Look.Hub:       // the one clean sky in the game
                sky.SetColor("_SkyTint", new Color(0.48f, 0.62f, 0.80f));
                sky.SetColor("_GroundColor", new Color(0.36f, 0.34f, 0.30f));
                sky.SetFloat("_AtmosphereThickness", 1.05f);
                sky.SetFloat("_Exposure", 1.30f);
                sky.SetFloat("_SunSize", 0.05f);
                sky.SetFloat("_SunDisk", 2f);
                break;
        }

        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;
        // The procedural sky puts its sun disk where this light points; without it Unity picks
        // the brightest directional light in the scene, which is not guaranteed to be ours.
        RenderSettings.sun = sun;
    }

    static VolumeProfile Profile(Look look, string dir)
    {
        Directory.CreateDirectory(dir);
        string path = dir + "/Look_" + look + ".asset";
        AssetDatabase.DeleteAsset(path);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.overrideState = true;
        tone.mode.value = TonemappingMode.Neutral;

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloom.scatter.overrideState = true;

        var colour = profile.Add<ColorAdjustments>(true);
        colour.postExposure.overrideState = true;
        colour.contrast.overrideState = true;
        colour.saturation.overrideState = true;
        colour.colorFilter.overrideState = true;

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;

        switch (look)
        {
            case Look.Farm:
                // Washed out and slightly blown: the valley is sick, not pretty.
                bloom.intensity.value = 0.55f;
                bloom.threshold.value = 1.05f;
                bloom.scatter.value = 0.72f;
                colour.postExposure.value = 0.10f;
                colour.contrast.value = 6f;
                colour.saturation.value = -12f;
                colour.colorFilter.value = new Color(1f, 0.96f, 0.86f);
                vignette.intensity.value = 0.22f;
                vignette.smoothness.value = 0.45f;
                break;
            case Look.Factory:
                // Crushed blacks and hot emissives — the lasers and screens should be the
                // brightest things on screen.
                bloom.intensity.value = 1.10f;
                bloom.threshold.value = 0.85f;
                bloom.scatter.value = 0.65f;
                colour.postExposure.value = -0.10f;
                colour.contrast.value = 18f;
                colour.saturation.value = -18f;
                colour.colorFilter.value = new Color(0.88f, 0.93f, 1f);
                vignette.intensity.value = 0.38f;
                vignette.smoothness.value = 0.40f;
                break;
            case Look.Hub:
                bloom.intensity.value = 0.45f;
                bloom.threshold.value = 1.10f;
                bloom.scatter.value = 0.70f;
                colour.postExposure.value = 0.05f;
                colour.contrast.value = 4f;
                colour.saturation.value = 4f;
                colour.colorFilter.value = Color.white;
                vignette.intensity.value = 0.18f;
                vignette.smoothness.value = 0.50f;
                break;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    /// <summary>A URP camera ignores every volume in the scene until this is switched on.</summary>
    static void EnableOnCamera()
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) continue;
            // A camera on Solid Color never draws the skybox, however well the sky is authored.
            cam.clearFlags = CameraClearFlags.Skybox;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            EditorUtility.SetDirty(data);
        }
    }
}

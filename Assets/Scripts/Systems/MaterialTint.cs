using UnityEngine;

/// <summary>
/// 3D stand-in for the 2D port's `spriteRenderer.color = …` one-liners.
///
/// Sprites had a colour channel built in; meshes don't, and assigning
/// `renderer.material.color` silently clones the material per object (a leak the
/// greybox pass would hit dozens of times over). This pushes the colour through a
/// shared <see cref="MaterialPropertyBlock"/> instead, so the batch stays intact.
/// URP Lit uses `_BaseColor`; the `_Color` fallback covers anything unlit/legacy.
/// </summary>
public static class MaterialTint
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock block;

    /// <summary>Tint one renderer.</summary>
    public static void Apply(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        block ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    /// <summary>Tint a whole subtree — the usual case when a prop is several meshes.</summary>
    public static void ApplyAll(GameObject root, Color color)
    {
        if (root == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) Apply(r, color);
    }

    /// <summary>Read a renderer's current tint, falling back to the shared material's.</summary>
    public static Color Read(Renderer renderer)
    {
        if (renderer == null) return Color.white;
        block ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        var c = block.GetColor(BaseColorId);
        if (c.a > 0f || c.r > 0f || c.g > 0f || c.b > 0f) return c;
        var mat = renderer.sharedMaterial;
        if (mat == null) return Color.white;
        if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);
        return mat.HasProperty(ColorId) ? mat.GetColor(ColorId) : Color.white;
    }
}

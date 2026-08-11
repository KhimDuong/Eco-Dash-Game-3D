using System.Collections;
using UnityEngine;

/// <summary>
/// Brief "I got hit" flash for enemies — the shared piece of C1's enemy foundation,
/// reused by <see cref="PlasticSlime"/> and (later) the fly-bot and the bosses.
///
/// <para>3D port: the 2D enemies did <c>spriteRenderer.color = Color.white</c> for
/// 0.07 s and restored the tint afterwards. A mesh has no colour channel of its own,
/// and assigning <c>renderer.material.color</c> clones the material per instance, so
/// the flash is pushed through a <see cref="MaterialPropertyBlock"/> instead — same
/// approach as <see cref="MaterialTint"/> and the player's hurt flash. Emission is
/// flashed alongside the base colour, which is what actually reads under the fixed ¾
/// rig: a lit blob pops, a tint swap barely registers. Materials without an emission
/// property (or with the keyword off) simply skip that half.</para>
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Tooltip("Renderers to flash. Auto-filled from this subtree when left empty.")]
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color flashColor = Color.white;
    [Tooltip("Emission colour during the flash (only used if the material has emission).")]
    [SerializeField] Color flashEmission = new Color(0.85f, 0.95f, 0.85f);
    [Tooltip("Seconds the flash lasts. 2D value: 0.07.")]
    [SerializeField] float duration = 0.07f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");   // URP Lit / Simple Lit
    static readonly int ColorId = Shader.PropertyToID("_Color");           // unlit & legacy
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock block;
    int[] colorIds;            // 0 = this renderer has no colour property
    Color[] baseColors;
    Color[] emissionColors;
    bool[] hasEmission;
    Coroutine running;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        int n = renderers.Length;
        block = new MaterialPropertyBlock();
        colorIds = new int[n];
        baseColors = new Color[n];
        emissionColors = new Color[n];
        hasEmission = new bool[n];

        for (int i = 0; i < n; i++)
        {
            var mat = renderers[i] != null ? renderers[i].sharedMaterial : null;
            if (mat == null) continue;

            if (mat.HasProperty(BaseColorId)) colorIds[i] = BaseColorId;
            else if (mat.HasProperty(ColorId)) colorIds[i] = ColorId;
            if (colorIds[i] != 0) baseColors[i] = mat.GetColor(colorIds[i]);

            hasEmission[i] = mat.HasProperty(EmissionId);
            if (hasEmission[i]) emissionColors[i] = mat.GetColor(EmissionId);
        }
    }

    /// <summary>Flash now. Re-flashing while a flash is up just restarts it.</summary>
    public void Flash()
    {
        if (!isActiveAndEnabled || renderers == null || renderers.Length == 0) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Set(true);
        yield return new WaitForSeconds(duration);
        Set(false);
        running = null;
    }

    void Set(bool lit)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(block);
            if (colorIds[i] != 0) block.SetColor(colorIds[i], lit ? flashColor : baseColors[i]);
            if (hasEmission[i]) block.SetColor(EmissionId, lit ? flashEmission : emissionColors[i]);
            r.SetPropertyBlock(block);
        }
    }
}

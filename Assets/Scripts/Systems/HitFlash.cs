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

    /// <summary>
    /// One flashable slot = one renderer *sub-mesh*. A property block is per material slot,
    /// not per renderer: the real art models are single renderers carrying several materials
    /// (the slime is one mesh with a body and an eye material), so restoring from
    /// <c>sharedMaterial</c> alone would repaint every sub-mesh in material 0's colour and
    /// the slime's eyes would stay body-green after its first hit. The greybox meshes hid
    /// this — they were one material per renderer.
    /// </summary>
    struct Slot
    {
        public Renderer renderer;
        public int index;          // material slot on that renderer
        public int colorId;        // 0 = no colour property
        public Color baseColor;
        public bool hasEmission;
        public Color emission;
    }

    MaterialPropertyBlock block;
    Slot[] slots;
    Coroutine running;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();

        var built = new System.Collections.Generic.List<Slot>();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                var slot = new Slot { renderer = r, index = m };
                if (mat.HasProperty(BaseColorId)) slot.colorId = BaseColorId;
                else if (mat.HasProperty(ColorId)) slot.colorId = ColorId;
                if (slot.colorId != 0) slot.baseColor = mat.GetColor(slot.colorId);

                slot.hasEmission = mat.HasProperty(EmissionId);
                if (slot.hasEmission) slot.emission = mat.GetColor(EmissionId);

                built.Add(slot);
            }
        }
        slots = built.ToArray();
    }

    /// <summary>Flash now. Re-flashing while a flash is up just restarts it.</summary>
    public void Flash()
    {
        if (!isActiveAndEnabled || slots == null || slots.Length == 0) return;
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
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s.renderer == null) continue;
            s.renderer.GetPropertyBlock(block, s.index);
            if (s.colorId != 0) block.SetColor(s.colorId, lit ? flashColor : s.baseColor);
            if (s.hasEmission) block.SetColor(EmissionId, lit ? flashEmission : s.emission);
            s.renderer.SetPropertyBlock(block, s.index);
        }
    }
}

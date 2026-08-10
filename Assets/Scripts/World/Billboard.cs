using UnityEngine;

/// <summary>
/// Keeps a world-space label (the "Nhấn E" prompts) square to the camera.
///
/// New in 3D: the 2D build drew prompts as sprites that were always facing the
/// player by construction. Under the fixed ¾ rig a world-space label would
/// otherwise lie down on the ground plane and read as a smear.
/// </summary>
[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    [Tooltip("Match the camera's tilt exactly. Off = stay upright and only spin on Y.")]
    [SerializeField] bool matchCameraPitch = true;

    Transform cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            cam = main.transform;
        }

        if (matchCameraPitch)
        {
            transform.rotation = cam.rotation;
            return;
        }

        Vector3 flat = cam.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
    }
}

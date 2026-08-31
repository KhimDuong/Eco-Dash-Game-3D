using UnityEngine;

/// <summary>
/// B9: marks a collider Greenie is allowed to ant-walk up. Empty on purpose — it is a
/// permission, not a behaviour, and <see cref="WallClimber"/> is the only thing that reads it.
///
/// <para><b>Climbing has to be opt-in per surface or it breaks the game.</b> The natural rule —
/// walk into a vertical face and keep pushing — is the right feel and costs no new key (the
/// control contract in CLAUDE.md has none free). But every boundary wall, cottage, fence and
/// factory partition in the project is also a vertical face, so the unrestricted rule lets
/// Greenie climb straight out of Level 1 and stand on the skybox. One marker component turns
/// that from a rule about geometry into a rule about <i>this rock</i>.</para>
///
/// <para>It sits on the collider, not on a parent: the mesa is 18 separate per-column
/// <c>BoxCollider</c>s after QA C3, and the climber tests the collider its probe actually hit.
/// <c>TerrainKit.Column</c> adds one to each as it builds them, so the permission is generated
/// with the rock rather than dragged on afterwards (CLAUDE.md rule 5).</para>
/// </summary>
[DisallowMultipleComponent]
public class Climbable : MonoBehaviour
{
}

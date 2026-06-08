using UnityEngine;

/// <summary>
/// Attach to ground surfaces to control per-surface friction for the character controller.
/// `friction` is a gameplay value where 1 = normal, 0 = very slippery (ice), >1 = extra sticky.
/// </summary>
public class SurfaceFriction : MonoBehaviour
{
    [Tooltip("Gameplay friction multiplier. 1 = normal, 0 = very slippery, >1 = sticky.")]
    public float friction = 1f;
}

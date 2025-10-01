
using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    /// <summary>
    /// Destroys the GameObject this component is attached to after a specified duration.
    /// Unlike VFXAutoDestroy, this is not tied to a ParticleSystem's lifetime.
    /// </summary>
    public class VFXAutoDestroyWithDuration : MonoBehaviour
    {
        /// <summary>
        /// The lifetime of this GameObject in seconds.
        /// </summary>
        public float Duration = 5.0f;

        private void Start()
        {
            Destroy(gameObject, Duration);
        }
    }
}

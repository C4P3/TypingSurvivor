using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class LowHealthEffect : MonoBehaviour
    {
        [Tooltip("The overlay texture to use for the effect. The alpha channel controls the blend intensity.")]
        public Texture2D overlayTexture;
        [Range(0,1)]
        public float opacity = 0.0f;

        private Material _material;

        void OnEnable()
        {
            // Dynamically create a material with the custom shader
            Shader shader = Shader.Find("Hidden/LowHealthOverlay");
            if (shader != null)
            {
                _material = new Material(shader);
            }
            else
            {
                Debug.LogError("Could not find 'Hidden/LowHealthOverlay' shader.");
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || overlayTexture == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Pass the parameters to the shader
            _material.SetTexture("_OverlayTex", overlayTexture);
            _material.SetFloat("_Opacity", opacity);

            // Apply the shader
            Graphics.Blit(source, destination, _material);
        }

        void OnDisable()
        {
            if (_material != null)
            {
                DestroyImmediate(_material);
            }
        }

        /// <summary>
        /// Sets the opacity of the overlay effect.
        /// </summary>
        public void SetOpacity(float newOpacity)
        {
            opacity = Mathf.Clamp01(newOpacity);
        }
    }
}

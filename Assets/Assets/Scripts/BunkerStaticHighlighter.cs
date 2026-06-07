using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// BunkerStaticHighlighter controls the visual pulsation of the static 
    /// corner bracket highlighter GameObjects in the scene.
    /// </summary>
    public class BunkerStaticHighlighter : MonoBehaviour
    {
        [Header("Glow Configuration")]
        public Color OutlineColor = new Color(0f, 1f, 0.95f, 0.85f);
        public bool Blink = true;
        public float BlinkSpeed = 4.5f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private float _animationTimer = 0f;

        // Standard HDRP property IDs
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Start()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (Blink && _renderers != null && _renderers.Length > 0)
            {
                _animationTimer += Time.deltaTime;
                // Soft pulse blink using Sin
                float pulse = 0.3f + 0.7f * ((Mathf.Sin(_animationTimer * BlinkSpeed) + 1f) * 0.5f);
                Color currentGlow = OutlineColor * (3.5f * pulse);

                foreach (Renderer r in _renderers)
                {
                    if (r != null)
                    {
                        r.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(EmissiveColorId, currentGlow);
                        
                        // Also pulse base color alpha for transparent pulse
                        Color baseColor = new Color(OutlineColor.r, OutlineColor.g, OutlineColor.b, OutlineColor.a * pulse);
                        _propBlock.SetColor(BaseColorId, baseColor);
                        
                        r.SetPropertyBlock(_propBlock);
                    }
                }
            }
        }
    }
}

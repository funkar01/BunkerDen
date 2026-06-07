using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// BunkerOutlineHighlighter calculates the 3D bounds of a GameObject
    /// and renders animated, glowing, holographic corner brackets to outline its boundaries.
    /// </summary>
    public class BunkerOutlineHighlighter : MonoBehaviour
    {
        [Header("Outline Styling")]
        [Tooltip("The base color of the glowing outline.")]
        public Color OutlineColor = new Color(0f, 1f, 0.95f, 0.85f); // Vibrant tactical cyan/teal

        [Tooltip("Thickness of the outline lines.")]
        public float LineWidth = 0.025f;

        [Tooltip("Length of each perpendicular bracket arm.")]
        public float BracketLength = 0.15f;

        [Tooltip("Extra padding to expand the bounds so lines don't clip into the mesh.")]
        public float BoundsPadding = 0.04f;

        [Header("Animation Settings")]
        [Tooltip("Does the outline blink to attract attention?")]
        public bool Blink = true;

        [Tooltip("Speed/frequency of the blinking effect.")]
        public float BlinkSpeed = 5f;

        private GameObject[] _brackets = new GameObject[8];
        private Bounds _combinedBounds;
        private float _animationTimer = 0f;
        private Material _lineMaterial;

        private void Start()
        {
            // Clamp inputs to prevent huge outline lines if Unity inspector values are desynchronized/defaulted to 1
            if (LineWidth > 0.1f) LineWidth = 0.025f;
            if (BracketLength > 0.5f) BracketLength = 0.15f;

            CalculateCombinedBounds();
            CreateLineMaterial();
            BuildCornerBrackets();
        }

        private void Update()
        {
            if (Blink)
            {
                _animationTimer += Time.deltaTime;
                // Soft pulse blink using Sin
                float pulse = 0.3f + 0.7f * ((Mathf.Sin(_animationTimer * BlinkSpeed) + 1f) * 0.5f);
                Color currentColor = new Color(OutlineColor.r, OutlineColor.g, OutlineColor.b, OutlineColor.a * pulse);

                foreach (GameObject bracket in _brackets)
                {
                    if (bracket != null)
                    {
                        LineRenderer lr = bracket.GetComponent<LineRenderer>();
                        if (lr != null)
                        {
                            lr.startColor = currentColor;
                            lr.endColor = currentColor;
                        }
                    }
                }
            }
        }

        private void CalculateCombinedBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                _combinedBounds = new Bounds(transform.position, Vector3.one);
                return;
            }

            _combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                // Skip if it's a particle system or something non-mesh
                if (renderers[i] is ParticleSystemRenderer) continue;
                _combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Expand slightly to clear the visual mesh boundaries
            _combinedBounds.Expand(BoundsPadding);
        }

        private void CreateLineMaterial()
        {
            // In HDRP, Sprites/Default or Internal-Colored shaders are incompatible and cull.
            // We search for HDRP/Unlit first, and configure it as a glowing overlay.
            Shader shader = Shader.Find("HDRP/Unlit");
            bool isHDRP = (shader != null);

            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            _lineMaterial = new Material(shader);

            if (isHDRP)
            {
                _lineMaterial.SetColor("_BaseColor", OutlineColor);
                
                // Configure HDRP/Unlit for transparent overlay rendering that bypasses depth testing
                _lineMaterial.SetInt("_SurfaceType", 1); // Transparent
                _lineMaterial.SetInt("_BlendMode", 0); // Alpha
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_ZWrite", 0); // Off
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); // Draw on top
                _lineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                // Enable HDRP Emissive glow
                _lineMaterial.EnableKeyword("_EMISSION");
                _lineMaterial.SetColor("_EmissiveColor", OutlineColor * 3.5f); // 3.5x multiplier for vivid neon glow
            }
            else
            {
                _lineMaterial.color = OutlineColor;
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            
            _lineMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
        }

        private void BuildCornerBrackets()
        {
            Vector3 min = _combinedBounds.min;
            Vector3 max = _combinedBounds.max;

            // Coordinates for the 8 corners of the bounding box
            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), // 0: Bottom-Left-Back
                new Vector3(max.x, min.y, min.z), // 1: Bottom-Right-Back
                new Vector3(max.x, max.y, min.z), // 2: Top-Right-Back
                new Vector3(min.x, max.y, min.z), // 3: Top-Left-Back
                new Vector3(min.x, min.y, max.z), // 4: Bottom-Left-Front
                new Vector3(max.x, min.y, max.z), // 5: Bottom-Right-Front
                new Vector3(max.x, max.y, max.z), // 6: Top-Right-Front
                new Vector3(min.x, max.y, max.z)  // 7: Top-Left-Front
            };

            for (int i = 0; i < 8; i++)
            {
                GameObject bracketGo = new GameObject($"Bracket_Corner_{i}");
                
                // Do NOT set parent to 'transform' to avoid inheriting parent scale (e.g. 0.01 scale).
                // Instead, keep it as a root GameObject. It uses world space positions anyway.
                bracketGo.transform.parent = null;
                bracketGo.transform.localScale = Vector3.one;

                LineRenderer lr = bracketGo.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.startWidth = LineWidth;
                lr.endWidth = LineWidth;
                lr.material = _lineMaterial;
                lr.startColor = OutlineColor;
                lr.endColor = OutlineColor;
                
                // We use 5 points to draw 3 perpendicular arms meeting at a single corner c:
                // Point 0: c + DX (arm 1)
                // Point 1: c (center)
                // Point 2: c + DY (arm 2)
                // Point 3: c (center)
                // Point 4: c + DZ (arm 3)
                lr.positionCount = 5;

                Vector3 corner = corners[i];

                // Determine directions pointing inwards along the edges of the box
                float dirX = (corner.x <= _combinedBounds.center.x) ? 1f : -1f;
                float dirY = (corner.y <= _combinedBounds.center.y) ? 1f : -1f;
                float dirZ = (corner.z <= _combinedBounds.center.z) ? 1f : -1f;

                Vector3 dx = new Vector3(dirX * BracketLength, 0f, 0f);
                Vector3 dy = new Vector3(0f, dirY * BracketLength, 0f);
                Vector3 dz = new Vector3(0f, 0f, dirZ * BracketLength);

                lr.SetPosition(0, corner + dx);
                lr.SetPosition(1, corner);
                lr.SetPosition(2, corner + dy);
                lr.SetPosition(3, corner);
                lr.SetPosition(4, corner + dz);

                _brackets[i] = bracketGo;
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject bracket in _brackets)
            {
                if (bracket != null) Destroy(bracket);
            }
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }
    }
}

using UnityEngine;

namespace BunkerTools
{
    [ExecuteAlways]
    public class BunkerDustManager : MonoBehaviour
    {
        [Header("Dust System Configuration")]
        [Tooltip("The material to use for the dust particles. If left empty, a default unlit particle material will be created.")]
        public Material dustMaterial;

        [Tooltip("If true, the system will automatically follow the camera. If false, it acts as a static volume in the scene.")]
        public bool followCamera = true;

        [Tooltip("Optionally select a target GameObject (e.g. Walls) to automatically position and scale the dust system to cover its entire bounds when Follow Camera is false.")]
        public GameObject fitToTarget;

        [Range(0f, 1000f)]
        [Tooltip("Maximum number of particles active at once.")]
        public float density = 150f;

        [Range(0.005f, 0.5f)]
        [Tooltip("Minimum particle size.")]
        public float minSize = 0.01f;

        [Range(0.005f, 0.5f)]
        [Tooltip("Maximum particle size.")]
        public float maxSize = 0.03f;

        [Header("Wind & Fluidity Settings")]
        [Tooltip("Direction of the wind drift.")]
        public Vector3 windDirection = new Vector3(0.2f, -0.05f, 0.1f);

        [Tooltip("Speed/intensity of the wind drift.")]
        public float windSpeed = 0.1f;

        [Range(0f, 1f)]
        [Tooltip("Opacity of the dust particles.")]
        public float opacity = 0.45f;

        [Tooltip("The ash color of the dust and smoke particles.")]
        public Color particleColor = new Color(0.68f, 0.68f, 0.7f, 1f);

        [Range(0.05f, 2.5f)]
        [Tooltip("Frequency of turbulence noise (higher means more rapid/volatile direction changes).")]
        public float noiseFrequency = 0.65f;

        [Range(0.05f, 3.0f)]
        [Tooltip("Strength multiplier for air turbulence volatility.")]
        public float noiseStrengthMultiplier = 1.4f;

        private ParticleSystem particleSys;
        private ParticleSystemRenderer psRenderer;
        private Transform playerCamera;
        private Texture2D proceduralTexture;

        private void Start()
        {
            InitializeSystem();
        }

        private void OnEnable()
        {
            InitializeSystem();
        }

        private void OnDisable()
        {
            if (proceduralTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(proceduralTexture);
                else
                    DestroyImmediate(proceduralTexture);
                proceduralTexture = null;
            }
        }

        private void Update()
        {
            if (particleSys == null)
            {
                InitializeSystem();
            }

            if (particleSys != null)
            {
                if (followCamera)
                {
                    // Update position of the particle system GameObject to track the camera
                    if (playerCamera != null)
                    {
                        particleSys.transform.position = playerCamera.position;
                    }
                    else if (Camera.main != null)
                    {
                        playerCamera = Camera.main.transform;
                        particleSys.transform.position = playerCamera.position;
                    }
                }
                else
                {
                    // Keep the particle system child aligned with the manager's position
                    particleSys.transform.localPosition = Vector3.zero;

                    // Automatically update bounds fit in Editor when not playing
                    if (fitToTarget != null && !Application.isPlaying)
                    {
                        FitToTargetBounds();
                    }
                }

                // Apply parameters dynamically so changes in inspector are reflected instantly
                ApplyParameters();
            }
        }

        private void OnValidate()
        {
            // Ensure min size doesn't exceed max size
            if (minSize > maxSize)
            {
                minSize = maxSize;
            }
            ApplyParameters();
        }

        private void InitializeSystem()
        {
            // Try to find main camera
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            // Look for existing child dust system
            Transform childDust = transform.Find("BunkerDustParticles");
            GameObject dustGo;
            if (childDust == null)
            {
                dustGo = new GameObject("BunkerDustParticles");
                dustGo.transform.SetParent(transform);
                dustGo.transform.localPosition = Vector3.zero;
                dustGo.transform.localRotation = Quaternion.identity;
            }
            else
            {
                dustGo = childDust.gameObject;
            }

            // Get or add particle system
            particleSys = dustGo.GetComponent<ParticleSystem>();
            if (particleSys == null)
            {
                particleSys = dustGo.AddComponent<ParticleSystem>();
            }

            psRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
            if (psRenderer == null)
            {
                psRenderer = dustGo.AddComponent<ParticleSystemRenderer>();
            }

            // Initialize default Particle System Modules
            var main = particleSys.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // Emitter moves, particles drift in world
            main.scalingMode = ParticleSystemScalingMode.Shape; // Scale shape box without scaling individual particles
            main.loop = true;
            main.prewarm = true; // Particles exist immediately when scene starts
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); // Natural random lifetime
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f); // Random rotation for natural look

            // Configure shape as a box
            var shape = particleSys.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            
            if (followCamera)
            {
                shape.position = new Vector3(0f, 0f, 3.5f); // Offset forward so particles are in view
                shape.scale = new Vector3(8f, 6f, 8f); // Concentrated volume
            }
            else
            {
                shape.position = Vector3.zero;
                if (fitToTarget != null)
                {
                    FitToTargetBounds();
                }
                else
                {
                    shape.scale = Vector3.one;
                }
            }

            // Configure noise module for realistic air turbulence
            var noise = particleSys.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low; // Fast performance
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.15f;
            noise.octaveCount = 1;

            // Configure Velocity Over Lifetime for constant wind drift
            var vel = particleSys.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;

            // Configure Rotation Over Lifetime for floating rotation
            var rot = particleSys.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-30f, 30f); // Random slow spinning

            // Configure Color Over Lifetime to fade particles in and out smoothly
            var colorOverLifetime = particleSys.colorOverLifetime;
            colorOverLifetime.enabled = true;

            // Configure Renderer
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Assign material
            if (dustMaterial == null)
            {
                // Fallback unlit material if not set
                Shader unlitShader = Shader.Find("HDRP/ParticlesUnlit");
                if (unlitShader == null)
                {
                    unlitShader = Shader.Find("Particles/Standard Unlit");
                }
                if (unlitShader != null)
                {
                    dustMaterial = new Material(unlitShader);
                    dustMaterial.name = "RuntimeFallbackDustMaterial";
                    // Configure transparency
                    if (dustMaterial.HasProperty("_SurfaceType"))
                        dustMaterial.SetFloat("_SurfaceType", 1); // Transparent
                    if (dustMaterial.HasProperty("_BlendMode"))
                        dustMaterial.SetFloat("_BlendMode", 0); // Alpha
                    dustMaterial.renderQueue = 3000; // Transparent
                }
            }

            if (dustMaterial != null)
            {
                // Ensure a circular texture is used if the material has no texture assigned
                Texture assignedTex = dustMaterial.HasProperty("_BaseColorMap") ? dustMaterial.GetTexture("_BaseColorMap") : null;
                if (assignedTex == null && dustMaterial.HasProperty("_MainTex"))
                {
                    assignedTex = dustMaterial.GetTexture("_MainTex");
                }

                if (assignedTex == null)
                {
                    Texture2D circleTex = GetProceduralCircleTexture();
                    if (dustMaterial.HasProperty("_BaseColorMap"))
                        dustMaterial.SetTexture("_BaseColorMap", circleTex);
                    if (dustMaterial.HasProperty("_MainTex"))
                        dustMaterial.SetTexture("_MainTex", circleTex);
                }

                psRenderer.material = dustMaterial;
            }

            ApplyParameters();

            if (!particleSys.isPlaying)
            {
                particleSys.Play();
            }
        }

        private void ApplyParameters()
        {
            if (particleSys == null) return;

            var main = particleSys.main;
            main.maxParticles = Mathf.CeilToInt(density * 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);

            var emission = particleSys.emission;
            emission.rateOverTime = density; // Spawns density particles per second

            // Noise parameters
            var noise = particleSys.noise;
            noise.strength = new ParticleSystem.MinMaxCurve(windSpeed * 0.15f * noiseStrengthMultiplier, windSpeed * 0.35f * noiseStrengthMultiplier);
            noise.frequency = noiseFrequency;

            // Wind drift velocity
            var vel = particleSys.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(windDirection.x * windSpeed);
            vel.y = new ParticleSystem.MinMaxCurve(windDirection.y * windSpeed);
            vel.z = new ParticleSystem.MinMaxCurve(windDirection.z * windSpeed);

            // Opacity and color fade (Smooth fade in and out)
            var colorOverLifetime = particleSys.colorOverLifetime;
            Gradient grad = new Gradient();
            
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(particleColor, 0.0f), 
                    new GradientColorKey(particleColor, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f), 
                    new GradientAlphaKey(opacity, 0.15f), // Fade in faster
                    new GradientAlphaKey(opacity, 0.8f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = grad;

            if (dustMaterial != null && psRenderer != null && psRenderer.sharedMaterial != dustMaterial)
            {
                psRenderer.material = dustMaterial;
            }
        }

        [ContextMenu("Fit to Target Bounds")]
        public void FitToTargetBounds()
        {
            if (fitToTarget == null) return;

            Renderer[] renderers = fitToTarget.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[BunkerDustManager] No renderers found in target '{fitToTarget.name}' or its children.", this);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Set our position to the center of the bounds
            transform.position = bounds.center;

            // Reset parent scale to (1,1,1) to avoid double-scaling the particle system shape
            transform.localScale = Vector3.one;

            // Set the particle system shape scale to cover the bounds size
            if (particleSys != null)
            {
                var shape = particleSys.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.position = Vector3.zero; // Local center
                shape.scale = bounds.size;
            }
        }

        private Texture2D GetProceduralCircleTexture()
        {
            if (proceduralTexture != null) return proceduralTexture;

            int size = 32;
            proceduralTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            proceduralTexture.name = "ProceduralSoftCircle";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) / 2f;
                    float dy = y - (size - 1) / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - dist);
                    // Gaussian decay for soft blurry edges
                    alpha = Mathf.Exp(-dist * dist * 4f) * alpha;
                    proceduralTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            proceduralTexture.Apply();
            return proceduralTexture;
        }
    }
}

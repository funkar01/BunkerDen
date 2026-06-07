using UnityEngine;
using UnityEngine.VFX;

namespace BunkerTools
{
    public class CinematicDustTrigger : MonoBehaviour
    {
        public enum CullingMode
        {
            PlayerRelativeHalo, // Moves with the player, generating a bubble of dust around them
            TriggerVolumeCheck  // Enables/Disables this emitter when the player enters/leaves a trigger collider
        }

        [Header("System Configuration")]
        [Tooltip("How this dust system optimizes performance.")]
        public CullingMode mode = CullingMode.PlayerRelativeHalo;

        [Tooltip("The Visual Effect component to control.")]
        public VisualEffect vfxComponent;

        [Header("Dust Settings")]
        [Range(0f, 2000f)]
        [Tooltip("Controls the 'Spawn Rate' exposed parameter in the VFX Graph.")]
        public float density = 100f;

        [Range(0.005f, 0.5f)]
        [Tooltip("Controls the 'Min Dust Size' exposed parameter in the VFX Graph.")]
        public float minSize = 0.01f;

        [Range(0.005f, 0.5f)]
        [Tooltip("Controls the 'Max Dust Size' exposed parameter in the VFX Graph.")]
        public float maxSize = 0.04f;

        [Header("Fluidity & Wind Settings (Optional)")]
        [Tooltip("The direction the wind flows (maps to 'WindDirection' parameter if exposed in VFX Graph).")]
        public Vector3 windDirection = new Vector3(0.2f, -0.05f, 0.1f);
        
        [Tooltip("The speed/intensity of the wind (maps to 'WindSpeed' or 'Turbulence' parameter if exposed).")]
        public float windSpeed = 0.3f;

        [Header("Smoke Settings (Optional)")]
        [Range(0f, 1f)]
        [Tooltip("Opacity of the dust particles (maps to 'Opacity' or 'Alpha' parameter if exposed).")]
        public float opacity = 0.6f;

        [Header("Player Relative Halo Settings")]
        [Tooltip("The camera or player transform to track.")]
        public Transform playerCamera;
        
        [Tooltip("Offset relative to the camera position (e.g. slightly in front).")]
        public Vector3 positionOffset = new Vector3(0, 0, 5);
        
        [Tooltip("Smooth speed for the emitter box to follow the camera.")]
        public float followSmoothTime = 0.5f;

        [Header("Trigger Volume Settings")]
        [Tooltip("The tag of the object that triggers the volume (usually 'Player').")]
        public string playerTag = "Player";

        private Vector3 velocity = Vector3.zero;
        private bool isPlayerInside = false;

        private void Start()
        {
            if (vfxComponent == null)
            {
                vfxComponent = GetComponent<VisualEffect>();
            }

            ApplyParameters();

            if (mode == CullingMode.PlayerRelativeHalo)
            {
                if (playerCamera == null && Camera.main != null)
                {
                    playerCamera = Camera.main.transform;
                }
                
                if (vfxComponent != null)
                {
                    vfxComponent.Play();
                }
            }
            else if (mode == CullingMode.TriggerVolumeCheck)
            {
                // Start disabled until player enters
                if (vfxComponent != null)
                {
                    vfxComponent.Stop();
                }
            }
        }

        private void Update()
        {
            // Apply updates in case values are animated or changed at runtime
            ApplyParameters();

            if (mode == CullingMode.PlayerRelativeHalo && playerCamera != null && vfxComponent != null)
            {
                // Smoothly move the emitter box to follow the player camera
                Vector3 targetPosition = playerCamera.position + (playerCamera.forward * positionOffset.z) + (playerCamera.up * positionOffset.y) + (playerCamera.right * positionOffset.x);
                vfxComponent.transform.position = Vector3.SmoothDamp(vfxComponent.transform.position, targetPosition, ref velocity, followSmoothTime);
            }
        }

        private void OnValidate()
        {
            if (vfxComponent == null)
            {
                vfxComponent = GetComponent<VisualEffect>();
            }
            ApplyParameters();
        }

        private void ApplyParameters()
        {
            if (vfxComponent == null) return;

            // Apply standard parameters (Exposed in the default FloatingDust.vfx graph)
            if (vfxComponent.HasFloat("Spawn Rate"))
                vfxComponent.SetFloat("Spawn Rate", density);
                
            if (vfxComponent.HasFloat("Min Dust Size"))
                vfxComponent.SetFloat("Min Dust Size", minSize);
                
            if (vfxComponent.HasFloat("Max Dust Size"))
                vfxComponent.SetFloat("Max Dust Size", maxSize);

            // Apply optional parameters (will only write if you expose these variables inside the VFX Graph Editor)
            if (vfxComponent.HasVector3("WindDirection"))
                vfxComponent.SetVector3("WindDirection", windDirection.normalized);

            if (vfxComponent.HasFloat("WindSpeed"))
                vfxComponent.SetFloat("WindSpeed", windSpeed);

            if (vfxComponent.HasFloat("Opacity"))
                vfxComponent.SetFloat("Opacity", opacity);
                
            if (vfxComponent.HasFloat("Alpha"))
                vfxComponent.SetFloat("Alpha", opacity);
        }

        // Trigger detection for TriggerVolumeCheck mode
        private void OnTriggerEnter(Collider other)
        {
            if (mode == CullingMode.TriggerVolumeCheck && other.CompareTag(playerTag))
            {
                isPlayerInside = true;
                if (vfxComponent != null)
                {
                    vfxComponent.Play();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (mode == CullingMode.TriggerVolumeCheck && other.CompareTag(playerTag))
            {
                isPlayerInside = false;
                if (vfxComponent != null)
                {
                    vfxComponent.Stop();
                }
            }
        }
    }
}

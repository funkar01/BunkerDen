using UnityEngine;

namespace BunkerTools
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Background Ambience")]
        [Tooltip("Ambient noise / music to loop continuously in the background.")]
        public AudioClip BackgroundNoiseClip;

        [Range(0f, 1f)]
        [Tooltip("Volume of the background ambience.")]
        public float BackgroundVolume = 0.5f;

        private AudioSource _backgroundAudioSource;

        private void Awake()
        {
            // Singleton pattern setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Create and configure the AudioSource for background ambient noise
            _backgroundAudioSource = gameObject.AddComponent<AudioSource>();
            _backgroundAudioSource.loop = true;
            _backgroundAudioSource.playOnAwake = false;
            _backgroundAudioSource.spatialBlend = 0f; // 2D sound for global presence
            _backgroundAudioSource.volume = BackgroundVolume;
        }

        private void Start()
        {
            // Start playing background noise automatically if a clip is assigned
            if (BackgroundNoiseClip != null)
            {
                PlayBackground(BackgroundNoiseClip, BackgroundVolume);
            }
        }

        private void Update()
        {
            // Sync background volume live with the inspector setting
            if (_backgroundAudioSource != null && _backgroundAudioSource.volume != BackgroundVolume)
            {
                _backgroundAudioSource.volume = BackgroundVolume;
            }
        }

        /// <summary>
        /// Plays or changes the active background music/noise loop.
        /// </summary>
        public void PlayBackground(AudioClip clip, float volume = 0.5f)
        {
            if (_backgroundAudioSource == null) return;

            BackgroundNoiseClip = clip;
            BackgroundVolume = volume;

            _backgroundAudioSource.clip = clip;
            _backgroundAudioSource.volume = volume;
            _backgroundAudioSource.Play();
            Debug.Log($"[AudioManager] Playing background noise: {clip.name} (volume={volume})");
        }

        /// <summary>
        /// Pauses the looping background noise.
        /// </summary>
        public void PauseBackground()
        {
            if (_backgroundAudioSource != null && _backgroundAudioSource.isPlaying)
            {
                _backgroundAudioSource.Pause();
                Debug.Log("[AudioManager] Background noise paused.");
            }
        }

        /// <summary>
        /// Resumes the looping background noise.
        /// </summary>
        public void ResumeBackground()
        {
            if (_backgroundAudioSource != null && !_backgroundAudioSource.isPlaying)
            {
                _backgroundAudioSource.UnPause();
                if (!_backgroundAudioSource.isPlaying)
                {
                    _backgroundAudioSource.Play();
                }
                Debug.Log("[AudioManager] Background noise resumed.");
            }
        }

        /// <summary>
        /// Updates the background volume dynamically.
        /// </summary>
        public void SetBackgroundVolume(float volume)
        {
            BackgroundVolume = Mathf.Clamp01(volume);
            if (_backgroundAudioSource != null)
            {
                _backgroundAudioSource.volume = BackgroundVolume;
            }
        }

        /// <summary>
        /// Plays a one-shot 2D sound effect.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null) return;
            // Create a temporary AudioSource or use PlayClipAtPoint with a camera position to play 2D
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
        }

        /// <summary>
        /// Plays a one-shot 3D sound effect at a specific location.
        /// </summary>
        public void Play3DSFX(AudioClip clip, Vector3 position, float volume = 1.0f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}

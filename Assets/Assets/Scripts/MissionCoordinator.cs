using System;
using System.Collections;
using UnityEngine;

namespace BunkerTools
{
    /// <summary>
    /// MissionCoordinator manages the lady coordinator's voice dialogues, radio static transitions,
    /// and updates the bottom-right HUD to objective states after the fade-in completes.
    /// </summary>
    public class MissionCoordinator : MonoBehaviour
    {
        public static MissionCoordinator Instance { get; private set; }

        [Header("Dialogue Content Configuration")]
        [Tooltip("The first dialogue spoken by the coordinator.")]
        public string Dialogue1Text = "Welcome to mission agent, I am your mission coordinator.";

        [Tooltip("The second dialogue spoken by the coordinator.")]
        public string Dialogue2Text = "There should be a generator and electric switch, power on the Bunker!";

        [Tooltip("The final objective text shown in the HUD after dialogues conclude.")]
        public string ObjectiveHUDText = "Locate the generator and power on the Bunker!";

        [Header("Scene 4 Dialogue Configuration")]
        [Tooltip("First Dialogue text for Scene 4.")]
        public string Scene4Dialogue1Text = "Mind the poisonous gases trapped inside, you need to hurry and find the evidence before your breath runs out";
        
        [Tooltip("Second Dialogue text for Scene 4.")]
        public string Scene4Dialogue2Text = "look for the main command room";

        [Tooltip("Objective text for Scene 4.")]
        public string Scene4ObjectiveText = "Look for the main command room";
        
        [Tooltip("Optional Voice clip for Scene 4 Dialogue 1.")]
        public AudioClip Scene4Dialogue1Clip;

        [Tooltip("Optional Voice clip for Scene 4 Dialogue 2.")]
        public AudioClip Scene4Dialogue2Clip;

        [Header("Scene 5 Dialogue Configuration")]
        [Tooltip("First Dialogue text for Scene 5.")]
        public string Scene5Dialogue1Text = "Great, now search for the evidence, it should be the biometric key which looks like a hard drive.";
        
        [Tooltip("Second Dialogue text for Scene 5.")]
        public string Scene5Dialogue2Text = "";

        [Tooltip("Objective text for Scene 5.")]
        public string Scene5ObjectiveText = "Search for the biometric key!";
        
        [Tooltip("Optional Voice clip for Scene 5 Dialogue 1.")]
        public AudioClip Scene5Dialogue1Clip;

        [Tooltip("Optional Voice clip for Scene 5 Dialogue 2.")]
        public AudioClip Scene5Dialogue2Clip;

        [Header("Scene 6 Dialogue Configuration")]
        [Tooltip("First Dialogue text for Scene 6.")]
        public string Scene6Dialogue1Text = "The secret code is 'heart of India'.";

        [Tooltip("Objective text for Scene 6.")]
        public string Scene6ObjectiveText = "Check for the 'Heart of India'!";

        [Tooltip("Optional Voice clip for Scene 6 Dialogue 1.")]
        public AudioClip Scene6Dialogue1Clip;

        [Header("Scene 7 Dialogue Configuration")]
        [Tooltip("First Dialogue text for Scene 7.")]
        public string Scene7Dialogue1Text = "Well done, you found the secret button at the Heart of India of Indian map!";

        [Tooltip("Objective text for Scene 7.")]
        public string Scene7ObjectiveText = "Explore the unlocked locker!";

        [Tooltip("Optional Voice clip for Scene 7 Dialogue 1.")]
        public AudioClip Scene7Dialogue1Clip;

        [Header("Scene 8 Dialogue Configuration")]
        [Tooltip("First Dialogue text for Scene 8.")]
        public string Scene8Dialogue1Text = "Bravo, you have found the Key, now exit the bunker ASAP";

        [Tooltip("Objective text for Scene 8.")]
        public string Scene8ObjectiveText = "Exit the bunker quickly!";

        [Tooltip("Optional Voice clip for Scene 8 Dialogue 1.")]
        public AudioClip Scene8Dialogue1Clip;

        [Header("Voice Clips (Optional - Synthesized if empty)")]
        [Tooltip("Audio clip for Dialogue 1.")]
        public AudioClip Dialogue1Clip;

        [Tooltip("Audio clip for Dialogue 2.")]
        public AudioClip Dialogue2Clip;

        [Header("Typing Settings")]
        [Tooltip("Delay in seconds between printed subtitle characters in the HUD.")]
        public float HUDTypewriterSpeed = 0.035f;

        private AudioSource _audioSource;
        private AudioClip _radioStartSFX;
        private AudioClip _radioEndSFX;
        private AudioClip _voiceChirpSFX;
        private bool _sequenceStarted = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Create AudioSource for radio static and dialogues
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D voice presence

            // Procedurally synthesize static sound clips to run natively out-of-the-box
            InitializeProceduralAudio();
        }

        /// <summary>
        /// Initiates the mission coordinator dialogue sequence. 
        /// Called automatically when the Scene 1 fade-in finishes.
        /// </summary>
        public void StartCoordinatorSequence()
        {
            if (_sequenceStarted) return;
            _sequenceStarted = true;

            StartCoroutine(CoordinatorDialogueSequenceRoutine());
        }

        private IEnumerator CoordinatorDialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating coordinator dialogues.");

            // Give a short 0.5s pause after the scene has fully faded in to establish presence
            yield return new WaitForSeconds(0.5f);

            // Ensure HUD is visible
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(0f);
                MissionCoordinatorHUD.Instance.FadeInHUD(0.5f);
                yield return new WaitForSeconds(0.5f);
            }

            // ============================================
            // DIALOGUE 1 SEQUENCE
            // ============================================
            // 1. Play radio click-on squelch static
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            // 2. Display sub-text in HUD and type it
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            // 3. Play actual dialogue audio (if provided)
            float dialogue1Duration = 3.5f;
            if (Dialogue1Clip != null)
            {
                _audioSource.clip = Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Dialogue1Clip.length;
            }
            else
            {
                // Play a brief high-tech beep as fallback vocal cue
                PlaySpeechBeep();
            }

            // Wait for dialogue printing and voice audio to finish
            float d1TextTime = Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            // 4. Play radio click-off static
            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // TRANSMISSION PAUSE (2.0s)
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                // Set HUD header color to orange to signify radio standby/pause
                MissionCoordinatorHUD.Instance.ShowTransmission("TRANSMISSION: STANDBY", Dialogue1Text, 0.001f, null);
            }
            yield return new WaitForSeconds(2.0f);

            // ============================================
            // DIALOGUE 2 SEQUENCE
            // ============================================
            // 1. Play radio click-on static
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            // 2. Display and print Dialogue 2
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Dialogue2Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            // 3. Play actual dialogue audio (if provided)
            float dialogue2Duration = 4.2f;
            if (Dialogue2Clip != null)
            {
                _audioSource.clip = Dialogue2Clip;
                _audioSource.Play();
                dialogue2Duration = Dialogue2Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            // Wait for dialogue printing and voice audio to finish
            float d2TextTime = Dialogue2Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue2Duration, d2TextTime));

            // 4. Play radio click-off static
            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // CONVERT TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    ObjectiveHUDText
                );
            }
            Debug.Log("[MissionCoordinator] Dialogue sequence completed. Objective HUD active.");
        }

        /// <summary>
        /// Initiates the Scene 4 (Poisonous Gas warning) dialogue sequence.
        /// </summary>
        public void StartScene4DialogueSequence()
        {
            StartCoroutine(Scene4DialogueSequenceRoutine());
        }

        private IEnumerator Scene4DialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating Scene 4 dialogues.");

            // Ensure HUD is visible and faded in
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(1f);
            }

            // ============================================
            // SCENE 4 DIALOGUE 1 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene4Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue1Duration = 5.0f;
            if (Scene4Dialogue1Clip != null)
            {
                _audioSource.clip = Scene4Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Scene4Dialogue1Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d1TextTime = Scene4Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // TRANSMISSION STANDBY (1.5s)
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission("TRANSMISSION: STANDBY", Scene4Dialogue1Text, 0.001f, null);
            }
            yield return new WaitForSeconds(1.5f);

            // ============================================
            // SCENE 4 DIALOGUE 2 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene4Dialogue2Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue2Duration = 3.0f;
            if (Scene4Dialogue2Clip != null)
            {
                _audioSource.clip = Scene4Dialogue2Clip;
                _audioSource.Play();
                dialogue2Duration = Scene4Dialogue2Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d2TextTime = Scene4Dialogue2Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue2Duration, d2TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // CONVERT TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    Scene4ObjectiveText
                );
            }
            Debug.Log("[MissionCoordinator] Scene 4 dialogue sequence completed.");
        }

        private bool _scene5SequenceStarted = false;

        /// <summary>
        /// Initiates the Scene 5 (Command Room reached) dialogue sequence.
        /// </summary>
        public void StartScene5DialogueSequence()
        {
            if (_scene5SequenceStarted) return;
            _scene5SequenceStarted = true;
            StartCoroutine(Scene5DialogueSequenceRoutine());
        }

        private IEnumerator Scene5DialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating Scene 5 dialogues.");

            // Wait a couple of seconds of gap after trigger
            yield return new WaitForSeconds(2.0f);

            // Ensure HUD is visible and faded in
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(1f);
            }

            // ============================================
            // SCENE 5 DIALOGUE 1 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene5Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue1Duration = 5.0f;
            if (Scene5Dialogue1Clip != null)
            {
                _audioSource.clip = Scene5Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Scene5Dialogue1Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d1TextTime = Scene5Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // CONVERT TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    Scene5ObjectiveText
                );
            }
            Debug.Log("[MissionCoordinator] Scene 5 dialogue sequence completed.");

            // Transition to Scene 6 after 10 seconds of exploring the arena
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                StartScene6DialogueSequence();
            }
            else
            #endif
            {
                StartCoroutine(TriggerScene6AfterDelayRoutine());
            }
        }

        private IEnumerator TriggerScene6AfterDelayRoutine()
        {
            yield return new WaitForSeconds(10.0f);
            StartScene6DialogueSequence();
        }

        private bool _scene6SequenceStarted = false;

        /// <summary>
        /// Initiates the Scene 6 (The hint) dialogue sequence.
        /// </summary>
        public void StartScene6DialogueSequence()
        {
            if (_scene6SequenceStarted) return;
            _scene6SequenceStarted = true;
            StartCoroutine(Scene6DialogueSequenceRoutine());
        }

        private IEnumerator Scene6DialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating Scene 6 dialogues.");

            // Ensure HUD is visible and faded in
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(1f);
            }

            // ============================================
            // SCENE 6 DIALOGUE 1 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene6Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue1Duration = 4.0f;
            if (Scene6Dialogue1Clip != null)
            {
                _audioSource.clip = Scene6Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Scene6Dialogue1Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d1TextTime = Scene6Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // RETURN TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    Scene6ObjectiveText
                );
            }
            Debug.Log("[MissionCoordinator] Scene 6 dialogue sequence completed.");
        }

        private bool _scene7SequenceStarted = false;

        /// <summary>
        /// Gets whether Scene 6 is currently active (Scene 6 started but Scene 7 has not).
        /// </summary>
        public bool IsScene6Active => _scene6SequenceStarted && !_scene7SequenceStarted;

        /// <summary>
        /// Gets whether Scene 7 is currently active (Scene 7 started but Scene 8 has not).
        /// </summary>
        public bool IsScene7Active => _scene7SequenceStarted && !_scene8SequenceStarted;

        private bool _scene8SequenceStarted = false;


        /// <summary>
        /// Initiates the Scene 7 (Emergency Button hint) dialogue sequence.
        /// </summary>
        public void StartScene7DialogueSequence()
        {
            if (_scene7SequenceStarted) return;
            _scene7SequenceStarted = true;
            StartCoroutine(Scene7DialogueSequenceRoutine());
        }

        private IEnumerator Scene7DialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating Scene 7 dialogues.");

            // Ensure HUD is visible and faded in
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(1f);
            }

            // ============================================
            // SCENE 7 DIALOGUE 1 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene7Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue1Duration = 4.0f;
            if (Scene7Dialogue1Clip != null)
            {
                _audioSource.clip = Scene7Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Scene7Dialogue1Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d1TextTime = Scene7Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // CONVERT TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    Scene7ObjectiveText
                );
            }
            Debug.Log("[MissionCoordinator] Scene 7 dialogue sequence completed.");
        }

        /// <summary>
        /// Initiates the Scene 8 (Exit Bunker ASAP) dialogue sequence.
        /// </summary>
        public void StartScene8DialogueSequence()
        {
            if (_scene8SequenceStarted) return;
            _scene8SequenceStarted = true;
            StartCoroutine(Scene8DialogueSequenceRoutine());
        }

        private IEnumerator Scene8DialogueSequenceRoutine()
        {
            Debug.Log("[MissionCoordinator] Initiating Scene 8 dialogues.");

            // Ensure HUD is visible and faded in
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.SetHUDAlpha(1f);
            }

            // ============================================
            // SCENE 8 DIALOGUE 1 SEQUENCE
            // ============================================
            PlaySound(_radioStartSFX, 0.45f);
            yield return new WaitForSeconds(_radioStartSFX.length - 0.05f);

            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowTransmission(
                    "TRANSMISSION: ACTIVE", 
                    Scene8Dialogue1Text, 
                    HUDTypewriterSpeed, 
                    _voiceChirpSFX
                );
            }

            float dialogue1Duration = 4.0f;
            if (Scene8Dialogue1Clip != null)
            {
                _audioSource.clip = Scene8Dialogue1Clip;
                _audioSource.Play();
                dialogue1Duration = Scene8Dialogue1Clip.length;
            }
            else
            {
                PlaySpeechBeep();
            }

            float d1TextTime = Scene8Dialogue1Text.Length * HUDTypewriterSpeed;
            yield return new WaitForSeconds(Mathf.Max(dialogue1Duration, d1TextTime));

            PlaySound(_radioEndSFX, 0.45f);
            yield return new WaitForSeconds(0.45f);

            // ============================================
            // CONVERT TO OBJECTIVE TRACKING
            // ============================================
            if (MissionCoordinatorHUD.Instance != null)
            {
                MissionCoordinatorHUD.Instance.ShowObjective(
                    "PRIORITY OBJECTIVE", 
                    Scene8ObjectiveText
                );
            }
            Debug.Log("[MissionCoordinator] Scene 8 dialogue sequence completed.");
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }

        private void PlaySpeechBeep()
        {
            // Plays a procedural transmission tone signifying start of audio
            AudioClip beep = CreateSineToneClip(420f, 0.18f, 0.35f);
            PlaySound(beep, 0.4f);
        }

        #region Procedural Audio Synthesis
        private void InitializeProceduralAudio()
        {
            _radioStartSFX = CreateRadioStartClip();
            _radioEndSFX = CreateRadioEndClip();
            _voiceChirpSFX = CreateVoiceChirpClip();
        }

        private AudioClip CreateRadioStartClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;

                // Sharp pop static click at start
                float click = 0f;
                if (t < 0.04f)
                {
                    click = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-130f * t) * 0.75f;
                }

                // Squelch white noise
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.2f;
                float noiseEnv = Mathf.Clamp01(1f - (t / duration));

                sampleArray[i] = (click + noise * noiseEnv) * 0.35f;
            }

            AudioClip clip = AudioClip.Create("RadioStart", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateRadioEndClip()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            System.Random rand = new System.Random();

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;

                // White noise burst fading out
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.3f;
                float noiseEnv = Mathf.Exp(-9f * t);

                // Squelch click off at the end
                float click = 0f;
                float clickTime = duration - 0.04f;
                if (t > clickTime)
                {
                    float ct = t - clickTime;
                    click = Mathf.Sin(2f * Mathf.PI * 180f * ct) * Mathf.Exp(-140f * ct) * 0.7f;
                }

                sampleArray[i] = (noise * noiseEnv + click) * 0.35f;
            }

            AudioClip clip = AudioClip.Create("RadioEnd", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateVoiceChirpClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration)); // Smooth curve
                float wave = Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * 780f * t) * 0.35f;
                sampleArray[i] = wave * env * 0.15f;
            }

            AudioClip clip = AudioClip.Create("VoiceChirp", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateSineToneClip(float frequency, float duration, float volumeMult)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                sampleArray[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volumeMult;
            }

            AudioClip clip = AudioClip.Create($"Sine_{frequency}Hz", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }
        #endregion
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BunkerTools
{
    /// <summary>
    /// MissionIntroUI manages the cinematic tactical briefing overlay displayed at the start of the game.
    /// It halts the player controller, locks input, reveals text with a typewriter effect,
    /// and transitions smoothly into the gameplay using ScreenFadeManager.
    /// </summary>
    public class MissionIntroUI : MonoBehaviour
    {
        [Header("Mission Text Configuration")]
        [Tooltip("Header Title displayed on the briefing box.")]
        public string HeaderTitle = "TACTICAL BRIEFING";

        [Tooltip("Sub-header security warning label.")]
        public string SubHeader = "SECURITY LEVEL 5 // EYES ONLY";

        [TextArea(5, 10)]
        [Tooltip("The core context text printed on the screen.")]
        public string BriefingText = "You are a secret agent serving for the nation, your mission is to explore the old abandoned bunker and retrieve the confidential data which is hidden inside.";

        [Tooltip("Speed of the typewriter effect (delay in seconds between characters).")]
        public float TypewriterSpeed = 0.04f;

        [Header("Audio Customization (Optional)")]
        [Tooltip("The sound effect played repeatedly while typing. Synthesized if left empty.")]
        public AudioClip TypewriterTickSound;

        [Tooltip("The sound effect played when hovering over the button. Synthesized if left empty.")]
        public AudioClip HoverSound;

        [Tooltip("The sound effect played when clicking the enter button. Synthesized if left empty.")]
        public AudioClip ClickSound;

        [Tooltip("The cinematic chime/swoosh played as the scene fades in. Synthesized if left empty.")]
        public AudioClip TransitionChime;

        [Header("Visual References (Optional - Built dynamically if empty)")]
        public CanvasGroup IntroCanvasGroup;
        public Text TitleTextComponent;
        public Text SubHeaderComponent;
        public Text BodyTextComponent;
        public Image EnterButtonImage;
        public Text EnterButtonText;

        public static MissionIntroUI Instance { get; private set; }

        // State trackers
        private bool _typewriterDone = false;
        private bool _transitionStarted = false;
        private bool _introFinished = false;
        private bool _isEndScreen = false;
        private Coroutine _typewriterCoroutine;

        // UI references for new controls
        private GameObject _helpPanelBorderGo;
        private GameObject _helpButtonBorderGo;
        private GameObject _quitButtonBorderGo;
        private GameObject _closeXButtonBorderGo;
        private Text _closeHelpButtonText;
        private bool _enteredFromMissionButton = false;

        // Generated audio clips
        private AudioClip _syntheticTypewriterTick;
        private AudioClip _syntheticHover;
        private AudioClip _syntheticClick;
        private AudioClip _syntheticChime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            // Synthesize audio clips if none are provided
            InitializeSyntheticAudio();
        }

        private void Start()
        {
            // Lock controls immediately
            SetPlayerControlsEnabled(false);

            // Ensure EventSystem exists so clicks register
            CreateEventSystemIfMissing();

            // Dynamically construct UI if reference components are missing
            if (IntroCanvasGroup == null)
            {
                ConstructUI();
            }

            // Screen should start completely black (ScreenFadeManager handles this via StartAlpha = 1)
            if (ScreenFadeManager.Instance != null)
            {
                ScreenFadeManager.Instance.SetAlpha(1f);
            }

            // Start typing briefing
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        private void Update()
        {
            // Ensure player controls remain locked during UI visibility (just in case)
            if (!_transitionStarted)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// True if the typewriter effect has finished printing the briefing text.
        /// </summary>
        public bool IsTypewriterDone()
        {
            return _typewriterDone;
        }

        /// <summary>
        /// Called when the player clicks the "ENTER THE MISSION" button.
        /// </summary>
        public void OnButtonClicked()
        {
            if (_isEndScreen)
            {
                PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
                return;
            }

            if (!_typewriterDone)
            {
                // Instant skip: fill out the text immediately
                if (_typewriterCoroutine != null)
                {
                    StopCoroutine(_typewriterCoroutine);
                }
                _typewriterDone = true;
                BodyTextComponent.text = BriefingText;
                PlaySound(_syntheticClick, 0.4f);
                return;
            }

            if (_transitionStarted) return;

            // Show Help Panel first so player doesn't miss it
            _enteredFromMissionButton = true;
            if (_closeHelpButtonText != null)
            {
                _closeHelpButtonText.text = "START MISSION";
            }
            
            PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);
            if (_helpPanelBorderGo != null)
            {
                _helpPanelBorderGo.SetActive(true);
            }
        }

        public void OnCloseButtonClicked()
        {
            PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void OnHelpButtonClicked()
        {
            _enteredFromMissionButton = false;
            if (_closeHelpButtonText != null)
            {
                _closeHelpButtonText.text = "CLOSE HELP";
            }
            PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);
            if (_helpPanelBorderGo != null)
            {
                _helpPanelBorderGo.SetActive(true);
            }
        }

        public void OnCloseHelpButtonClicked()
        {
            PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);
            if (_helpPanelBorderGo != null)
            {
                _helpPanelBorderGo.SetActive(false);
            }

            // If we came from the Enter Mission flow, trigger the game start transition
            if (_enteredFromMissionButton)
            {
                _enteredFromMissionButton = false;
                if (_transitionStarted) return;
                _transitionStarted = true;
                StartCoroutine(TransitionSequence());
            }
        }

        private Sprite LoadHelpSprite()
        {
            Texture2D tex = Resources.Load<Texture2D>("MouseControlsHelp");
            if (tex == null)
            {
                Debug.LogWarning("[MissionIntroUI] Could not load MouseControlsHelp via Resources.Load. Trying direct file load from path.");
                try
                {
                    string fullPath = System.IO.Path.Combine(Application.dataPath, "Resources/MouseControlsHelp.png");
                    if (System.IO.File.Exists(fullPath))
                    {
                        byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                        tex = new Texture2D(2, 2);
                        tex.LoadImage(fileData);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[MissionIntroUI] Error loading fallback image: " + ex.Message);
                }
            }

            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("[MissionIntroUI] MouseControlsHelp texture could not be loaded!");
                return null;
            }
        }

        public void PlayHoverSound()
        {
            PlaySound(HoverSound != null ? HoverSound : _syntheticHover, 0.25f);
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(clip, volume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
                }
            }
        }

        private IEnumerator TypewriterRoutine()
        {
            BodyTextComponent.text = "";
            string currentText = "";
            int charIndex = 0;
            int tickCounter = 0;

            while (charIndex < BriefingText.Length)
            {
                currentText += BriefingText[charIndex];
                BodyTextComponent.text = currentText + " █"; // Cyber block typing cursor
                charIndex++;

                // Tick sound every other character to avoid audio clutter
                tickCounter++;
                if (tickCounter % 2 == 0)
                {
                    PlaySound(TypewriterTickSound != null ? TypewriterTickSound : _syntheticTypewriterTick, 0.12f);
                }

                yield return new WaitForSeconds(TypewriterSpeed);
            }

            _typewriterDone = true;

            // Flashing cursor effect once writing is completed
            bool cursorOn = true;
            while (!_transitionStarted)
            {
                BodyTextComponent.text = BriefingText + (cursorOn ? " █" : "");
                cursorOn = !cursorOn;
                yield return new WaitForSeconds(0.45f);
            }
        }

        private IEnumerator TransitionSequence()
        {
            _introFinished = true;
            PlaySound(ClickSound != null ? ClickSound : _syntheticClick, 0.5f);

            // 1. Fade out the Briefing Canvas UI smoothly
            float fadeElapsed = 0f;
            float fadeDuration = 0.5f;
            float startUIAlpha = IntroCanvasGroup.alpha;

            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                IntroCanvasGroup.alpha = Mathf.Lerp(startUIAlpha, 0f, fadeElapsed / fadeDuration);
                yield return null;
            }
            IntroCanvasGroup.gameObject.SetActive(false); // Hide the UI canvas child (keeps this GameObject active so coroutine continues)

            // 2. Play transition chime/swoosh during the pitch-black moment
            PlaySound(TransitionChime != null ? TransitionChime : _syntheticChime, 0.6f);

            // 3. Keep screen solid black for cinematic tension (0.5s)
            yield return new WaitForSeconds(0.6f);

            // 4. Fade in the screen (fade OUT the black overlay) to reveal the player starting point
            if (ScreenFadeManager.Instance != null)
            {
                ScreenFadeManager.Instance.FadeIn(1.8f, () =>
                {
                    // 5. Unlock controls once transition completes
                    SetPlayerControlsEnabled(true);
                    Debug.Log("[BunkerDen] Mission started. Player controls enabled.");

                    // 6. Trigger coordinator dialogues
                    if (MissionCoordinator.Instance != null)
                    {
                        MissionCoordinator.Instance.StartCoordinatorSequence();
                    }
                });
            }
            else
            {
                // Fallback if fade manager is missing
                SetPlayerControlsEnabled(true);

                if (MissionCoordinator.Instance != null)
                {
                    MissionCoordinator.Instance.StartCoordinatorSequence();
                }
            }
        }

        private void SetPlayerControlsEnabled(bool enabledState)
        {
            // Toggle StarterAssets FirstPersonController if present
            var fpc = FindAnyObjectByType<StarterAssets.FirstPersonController>();
            if (fpc != null)
            {
                fpc.enabled = enabledState;

                var cc = fpc.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = enabledState;

                var inputs = fpc.GetComponent<StarterAssets.StarterAssetsInputs>();
                if (inputs != null)
                {
                    inputs.cursorLocked = enabledState;
                    inputs.cursorInputForLook = enabledState;
                    inputs.move = Vector2.zero;
                    inputs.look = Vector2.zero;
                    inputs.jump = false;
                    inputs.sprint = false;
                }

                var playerInput = fpc.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    if (enabledState) playerInput.ActivateInput();
                    else playerInput.DeactivateInput();
                }
            }

            // Toggle StarterAssets ThirdPersonController if present
            var tpc = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
            if (tpc != null)
            {
                tpc.enabled = enabledState;

                var cc = tpc.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = enabledState;

                var inputs = tpc.GetComponent<StarterAssets.StarterAssetsInputs>();
                if (inputs != null)
                {
                    inputs.cursorLocked = enabledState;
                    inputs.cursorInputForLook = enabledState;
                    inputs.move = Vector2.zero;
                    inputs.look = Vector2.zero;
                    inputs.jump = false;
                    inputs.sprint = false;
                }

                var playerInput = tpc.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    if (enabledState) playerInput.ActivateInput();
                    else playerInput.DeactivateInput();
                }
            }

            // Sync cursor state
            if (enabledState)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void CreateEventSystemIfMissing()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();

                // Try to add InputSystemUIInputModule first (new Input System), fallback to StandaloneInputModule
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemGo.AddComponent(inputModuleType);
                    Debug.Log("[MissionIntroUI] Created EventSystem with InputSystemUIInputModule.");
                }
                else
                {
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("[MissionIntroUI] Created EventSystem with StandaloneInputModule.");
                }
            }
        }

        #region Runtime UI Construction
        /// <summary>
        /// Programmatically constructs a high-quality, glassmorphic briefing UI screen overlays.
        /// </summary>
        private GameObject CreateUIButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, string buttonType, out Image outButtonImage, out Text outButtonText)
        {
            GameObject btnBorderGo = new GameObject(name + "Border");
            btnBorderGo.transform.SetParent(parent, false);
            Image btnBorderImage = btnBorderGo.AddComponent<Image>();
            btnBorderImage.color = new Color(0.4f, 1f, 0.98f, 0.35f); // Soft cyan border for button

            RectTransform btnBorderRect = btnBorderImage.GetComponent<RectTransform>();
            btnBorderRect.anchorMin = new Vector2(0.5f, 0f);
            btnBorderRect.anchorMax = new Vector2(0.5f, 0f);
            btnBorderRect.pivot = new Vector2(0.5f, 0f);
            btnBorderRect.anchoredPosition = anchoredPosition;
            btnBorderRect.sizeDelta = size;

            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(btnBorderGo.transform, false);
            outButtonImage = btnGo.AddComponent<Image>();
            outButtonImage.color = new Color(0.07f, 0.08f, 0.09f, 0.9f);

            RectTransform btnRect = outButtonImage.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = new Vector2(-4f, -4f); // 2px border offset

            GameObject btnTextGo = new GameObject(name + "Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            outButtonText = btnTextGo.AddComponent<Text>();
            outButtonText.text = text;
            outButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            outButtonText.fontSize = 18;
            outButtonText.fontStyle = FontStyle.Bold;
            outButtonText.color = new Color(0.4f, 1f, 0.98f, 1f);
            outButtonText.alignment = TextAnchor.MiddleCenter;
            outButtonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            outButtonText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform btnTxtRect = btnTextGo.GetComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            btnTxtRect.sizeDelta = Vector2.zero;

            MissionButtonEffects effects = btnGo.AddComponent<MissionButtonEffects>();
            effects.IntroUI = this;
            effects.ButtonImage = outButtonImage;
            effects.ButtonText = outButtonText;
            effects.ButtonType = buttonType;

            return btnGo;
        }

        private void ConstructUI()
        {
            Debug.Log("[MissionIntroUI] Dynamic UI Canvas construction initiated.");

            // 1. Root Canvas setup
            GameObject canvasGo = new GameObject("IntroCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Above ScreenFadeManager (999) so it displays on top of the black fade overlay

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            IntroCanvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // 2. Full screen solid dark blocker background
            GameObject bgGo = new GameObject("SolidBlocker");
            bgGo.transform.SetParent(canvasGo.transform, false);
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.03f, 0.03f, 0.04f, 1f); // Dark space color

            RectTransform bgRect = bgImage.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 3. Central Glassmorphic Briefing Panel
            GameObject panelBorderGo = new GameObject("BriefingPanelBorder");
            panelBorderGo.transform.SetParent(canvasGo.transform, false);
            
            // Subtle glowing teal outline border
            Image borderImage = panelBorderGo.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 1f, 0.98f, 0.18f); // Soft cyan border

            RectTransform borderRect = borderImage.GetComponent<RectTransform>();
            borderRect.sizeDelta = new Vector3(804f, 524f); // 2px border offset

            GameObject panelGo = new GameObject("BriefingPanel");
            panelGo.transform.SetParent(panelBorderGo.transform, false);
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.05f, 0.06f, 0.92f); // Glassmorphism backdrop

            RectTransform panelRect = panelImage.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = new Vector2(-4f, -4f); // Keep border visible

            // Add UI Corner Brackets (Stylistic tactical HUD accents)
            AddCornerBrackets(panelGo.transform, 800f, 520f);

            // 4. Header title text
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(panelGo.transform, false);
            TitleTextComponent = titleGo.AddComponent<Text>();
            TitleTextComponent.text = HeaderTitle;
            TitleTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TitleTextComponent.fontSize = 32;
            TitleTextComponent.fontStyle = FontStyle.Bold;
            TitleTextComponent.color = new Color(0.4f, 1f, 0.98f, 1f); // Accent Teal
            TitleTextComponent.alignment = TextAnchor.MiddleCenter;
            TitleTextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            TitleTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -25f);
            titleRect.sizeDelta = new Vector2(-60f, 40f);

            // 5. Subheader warning text
            GameObject subGo = new GameObject("SubTitleText");
            subGo.transform.SetParent(panelGo.transform, false);
            SubHeaderComponent = subGo.AddComponent<Text>();
            SubHeaderComponent.text = SubHeader;
            SubHeaderComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SubHeaderComponent.fontSize = 17; // Enlarged from 13
            SubHeaderComponent.fontStyle = FontStyle.Bold;
            SubHeaderComponent.color = new Color(1f, 0.35f, 0.35f, 1f); // Vibrant bright red
            SubHeaderComponent.alignment = TextAnchor.MiddleCenter;
            SubHeaderComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            SubHeaderComponent.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -70f);
            subRect.sizeDelta = new Vector2(-60f, 30f);

            // Horizontal Separator Line
            GameObject lineGo = new GameObject("DividerLine");
            lineGo.transform.SetParent(panelGo.transform, false);
            Image lineImage = lineGo.AddComponent<Image>();
            lineImage.color = new Color(0.4f, 1f, 0.98f, 0.15f);

            RectTransform lineRect = lineImage.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(0f, -110f); // Shifted down slightly
            lineRect.sizeDelta = new Vector2(-100f, 2f);

            // 6. Body briefing text
            GameObject bodyGo = new GameObject("BodyText");
            bodyGo.transform.SetParent(panelGo.transform, false);
            BodyTextComponent = bodyGo.AddComponent<Text>();
            BodyTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BodyTextComponent.fontSize = 20;
            BodyTextComponent.color = new Color(0.88f, 0.9f, 0.92f, 1f); // Crisp off-white
            BodyTextComponent.alignment = TextAnchor.UpperLeft;
            BodyTextComponent.lineSpacing = 1.35f;
            BodyTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            BodyTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -135f);
            bodyRect.sizeDelta = new Vector2(-100f, 210f);

            // 7. Interactive Action Buttons (Symmetrical Layout)
            _helpButtonBorderGo = CreateUIButton(panelGo.transform, "HelpButton", "HELP", new Vector2(-260f, 40f), new Vector2(200f, 60f), "Help", out _, out _).transform.parent.gameObject;
            CreateUIButton(panelGo.transform, "EnterButton", "ENTER THE MISSION", new Vector2(0f, 40f), new Vector2(280f, 60f), "Enter", out EnterButtonImage, out EnterButtonText);
            _quitButtonBorderGo = CreateUIButton(panelGo.transform, "QuitButton", "QUIT GAME", new Vector2(260f, 40f), new Vector2(200f, 60f), "Close", out _, out _).transform.parent.gameObject;

            // Top-right corner close button for tactical HUD
            GameObject closeBtnBorderGo = new GameObject("CloseXButtonBorder");
            closeBtnBorderGo.transform.SetParent(panelGo.transform, false);
            Image closeBtnBorderImage = closeBtnBorderGo.AddComponent<Image>();
            closeBtnBorderImage.color = new Color(1f, 0.35f, 0.35f, 0.35f); // Reddish border for close

            RectTransform closeBtnBorderRect = closeBtnBorderImage.GetComponent<RectTransform>();
            closeBtnBorderRect.anchorMin = new Vector2(1f, 1f);
            closeBtnBorderRect.anchorMax = new Vector2(1f, 1f);
            closeBtnBorderRect.pivot = new Vector2(1f, 1f);
            closeBtnBorderRect.anchoredPosition = new Vector2(-15f, -15f);
            closeBtnBorderRect.sizeDelta = new Vector2(36f, 36f);

            GameObject closeBtnGo = new GameObject("CloseXButton");
            closeBtnGo.transform.SetParent(closeBtnBorderGo.transform, false);
            Image closeBtnImage = closeBtnGo.AddComponent<Image>();
            closeBtnImage.color = new Color(0.07f, 0.08f, 0.09f, 0.9f);

            RectTransform closeBtnRect = closeBtnImage.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = Vector2.zero;
            closeBtnRect.anchorMax = Vector2.one;
            closeBtnRect.sizeDelta = new Vector2(-4f, -4f);

            GameObject closeBtnTextGo = new GameObject("CloseXButtonText");
            closeBtnTextGo.transform.SetParent(closeBtnGo.transform, false);
            Text closeBtnText = closeBtnTextGo.AddComponent<Text>();
            closeBtnText.text = "X";
            closeBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeBtnText.fontSize = 18;
            closeBtnText.fontStyle = FontStyle.Bold;
            closeBtnText.color = new Color(1f, 0.35f, 0.35f, 1f); // Red text
            closeBtnText.alignment = TextAnchor.MiddleCenter;

            RectTransform closeBtnTxtRect = closeBtnTextGo.GetComponent<RectTransform>();
            closeBtnTxtRect.anchorMin = Vector2.zero;
            closeBtnTxtRect.anchorMax = Vector2.one;
            closeBtnTxtRect.sizeDelta = Vector2.zero;

            MissionButtonEffects closeEffects = closeBtnGo.AddComponent<MissionButtonEffects>();
            closeEffects.IntroUI = this;
            closeEffects.ButtonImage = closeBtnImage;
            closeEffects.ButtonText = closeBtnText;
            closeEffects.ButtonType = "Close";
            closeEffects.NormalBgColor = new Color(0.07f, 0.08f, 0.09f, 0.9f);
            closeEffects.HoverBgColor = new Color(1f, 0.35f, 0.35f, 1.0f); // Bright red on hover
            closeEffects.NormalTextColor = new Color(1f, 0.35f, 0.35f, 1f);
            closeEffects.HoverTextColor = new Color(0.04f, 0.05f, 0.06f, 1.0f);

            _closeXButtonBorderGo = closeBtnBorderGo;

            // 8. Help Panel Overlay (initially inactive)
            _helpPanelBorderGo = new GameObject("HelpPanelBorder");
            _helpPanelBorderGo.transform.SetParent(canvasGo.transform, false);
            _helpPanelBorderGo.SetActive(false);

            Image helpBorderImage = _helpPanelBorderGo.AddComponent<Image>();
            helpBorderImage.color = new Color(0.4f, 1f, 0.98f, 0.25f); // Soft cyan border

            RectTransform helpBorderRect = helpBorderImage.GetComponent<RectTransform>();
            helpBorderRect.sizeDelta = new Vector2(804f, 524f);

            GameObject helpPanelGo = new GameObject("HelpPanel");
            helpPanelGo.transform.SetParent(_helpPanelBorderGo.transform, false);
            Image helpPanelImage = helpPanelGo.AddComponent<Image>();
            helpPanelImage.color = new Color(0.04f, 0.05f, 0.06f, 0.98f); // High-opacity glass background

            RectTransform helpPanelRect = helpPanelImage.GetComponent<RectTransform>();
            helpPanelRect.anchorMin = Vector2.zero;
            helpPanelRect.anchorMax = Vector2.one;
            helpPanelRect.sizeDelta = new Vector2(-4f, -4f);

            // Add UI Corner Brackets
            AddCornerBrackets(helpPanelGo.transform, 800f, 520f);

            // Title: CONTROLS GUIDE
            GameObject helpTitleGo = new GameObject("HelpTitleText");
            helpTitleGo.transform.SetParent(helpPanelGo.transform, false);
            Text helpTitleText = helpTitleGo.AddComponent<Text>();
            helpTitleText.text = "CONTROLS GUIDE";
            helpTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            helpTitleText.fontSize = 28;
            helpTitleText.fontStyle = FontStyle.Bold;
            helpTitleText.color = new Color(0.4f, 1f, 0.98f, 1f);
            helpTitleText.alignment = TextAnchor.MiddleCenter;

            RectTransform helpTitleRect = helpTitleGo.GetComponent<RectTransform>();
            helpTitleRect.anchorMin = new Vector2(0f, 1f);
            helpTitleRect.anchorMax = new Vector2(1f, 1f);
            helpTitleRect.pivot = new Vector2(0.5f, 1f);
            helpTitleRect.anchoredPosition = new Vector2(0f, -25f);
            helpTitleRect.sizeDelta = new Vector2(-60f, 40f);

            // Subtitle
            GameObject helpSubGo = new GameObject("HelpSubTitleText");
            helpSubGo.transform.SetParent(helpPanelGo.transform, false);
            Text helpSubText = helpSubGo.AddComponent<Text>();
            helpSubText.text = "MOUSE BINDINGS FOR MOVEMENT";
            helpSubText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            helpSubText.fontSize = 14;
            helpSubText.fontStyle = FontStyle.Bold;
            helpSubText.color = new Color(1f, 0.35f, 0.35f, 1f); // Warning red accent
            helpSubText.alignment = TextAnchor.MiddleCenter;

            RectTransform helpSubRect = helpSubGo.GetComponent<RectTransform>();
            helpSubRect.anchorMin = new Vector2(0f, 1f);
            helpSubRect.anchorMax = new Vector2(1f, 1f);
            helpSubRect.pivot = new Vector2(0.5f, 1f);
            helpSubRect.anchoredPosition = new Vector2(0f, -65f);
            helpSubRect.sizeDelta = new Vector2(-60f, 30f);

            // Divider Line
            GameObject helpLineGo = new GameObject("HelpDividerLine");
            helpLineGo.transform.SetParent(helpPanelGo.transform, false);
            Image helpLineImage = helpLineGo.AddComponent<Image>();
            helpLineImage.color = new Color(0.4f, 1f, 0.98f, 0.15f);

            RectTransform helpLineRect = helpLineImage.GetComponent<RectTransform>();
            helpLineRect.anchorMin = new Vector2(0f, 1f);
            helpLineRect.anchorMax = new Vector2(1f, 1f);
            helpLineRect.pivot = new Vector2(0.5f, 1f);
            helpLineRect.anchoredPosition = new Vector2(0f, -95f);
            helpLineRect.sizeDelta = new Vector2(-100f, 2f);

            // Mouse Graphic Image
            GameObject mouseGraphicGo = new GameObject("MouseGraphic");
            mouseGraphicGo.transform.SetParent(helpPanelGo.transform, false);
            Image mouseGraphicImage = mouseGraphicGo.AddComponent<Image>();
            
            Sprite loadedSprite = LoadHelpSprite();
            if (loadedSprite != null)
            {
                mouseGraphicImage.sprite = loadedSprite;
            }
            else
            {
                mouseGraphicImage.color = Color.clear;
            }
            mouseGraphicImage.preserveAspect = true;

            RectTransform mouseGraphicRect = mouseGraphicImage.GetComponent<RectTransform>();
            mouseGraphicRect.anchorMin = new Vector2(0.5f, 0.5f);
            mouseGraphicRect.anchorMax = new Vector2(0.5f, 0.5f);
            mouseGraphicRect.pivot = new Vector2(0.5f, 0.5f);
            mouseGraphicRect.anchoredPosition = new Vector2(0f, 10f); // Centered, adjusted to avoid header overlap
            mouseGraphicRect.sizeDelta = new Vector2(350f, 280f);
            mouseGraphicRect.localScale = new Vector3(1f, 1.5f, 1f);

            // Close Help Button
            CreateUIButton(helpPanelGo.transform, "CloseHelpButton", "CLOSE HELP", new Vector2(0f, 40f), new Vector2(280f, 60f), "CloseHelp", out _, out _closeHelpButtonText);
        }

        private void AddCornerBrackets(Transform parent, float width, float height)
        {
            Color bracketColor = new Color(0.4f, 1f, 0.98f, 0.75f);
            float length = 18f;
            float thickness = 3f;

            // Top-Left Corner
            CreateBracketCorner(parent, new Vector2(-width/2, height/2), new Vector2(1, -1), length, thickness, bracketColor);
            // Top-Right Corner
            CreateBracketCorner(parent, new Vector2(width/2, height/2), new Vector2(-1, -1), length, thickness, bracketColor);
            // Bottom-Left Corner
            CreateBracketCorner(parent, new Vector2(-width/2, -height/2), new Vector2(1, 1), length, thickness, bracketColor);
            // Bottom-Right Corner
            CreateBracketCorner(parent, new Vector2(width/2, -height/2), new Vector2(-1, 1), length, thickness, bracketColor);
        }

        private void CreateBracketCorner(Transform parent, Vector2 pos, Vector2 direction, float len, float thick, Color color)
        {
            // Vertical bar
            GameObject vGo = new GameObject("BracketV");
            vGo.transform.SetParent(parent, false);
            Image vImg = vGo.AddComponent<Image>();
            vImg.color = color;
            RectTransform vRect = vImg.GetComponent<RectTransform>();
            vRect.sizeDelta = new Vector2(thick, len);
            vRect.anchoredPosition = pos + new Vector2(direction.x * thick / 2, direction.y * len / 2);

            // Horizontal bar
            GameObject hGo = new GameObject("BracketH");
            hGo.transform.SetParent(parent, false);
            Image hImg = hGo.AddComponent<Image>();
            hImg.color = color;
            RectTransform hRect = hImg.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(len, thick);
            hRect.anchoredPosition = pos + new Vector2(direction.x * len / 2, direction.y * thick / 2);
        }
        #endregion

        #region Dynamic Synthetic Audio Synthesis
        /// <summary>
        /// Synthesizes appropriate high-tech audio sounds procedurally to ensure audio exists without setup.
        /// </summary>
        private void InitializeSyntheticAudio()
        {
            _syntheticTypewriterTick = CreateSineToneClip(850f, 0.012f, 0.08f);
            _syntheticHover = CreateSineToneClip(1100f, 0.03f, 0.12f);
            _syntheticClick = CreateDoubleSineToneClip(650f, 1300f, 0.05f, 0.25f);
            _syntheticChime = CreateSyntheticChimeClip();
        }

        private AudioClip CreateSineToneClip(float frequency, float duration, float volumeMult)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration)); // Linear decay
                sampleArray[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volumeMult;
            }

            AudioClip clip = AudioClip.Create($"Sine_{frequency}Hz", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateDoubleSineToneClip(float f1, float f2, float duration, float volumeMult)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration)); // Linear decay
                float wave = (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t)) * 0.5f;
                sampleArray[i] = wave * envelope * volumeMult;
            }

            AudioClip clip = AudioClip.Create("ClickSine", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        private AudioClip CreateSyntheticChimeClip()
        {
            int sampleRate = 44100;
            float duration = 2.0f; // Long chime
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] sampleArray = new float[samplesCount];

            // Harmonic complex wave to emulate a metal cinematic chime transition
            float[] frequencies = { 180f, 360f, 540f, 720f, 900f, 1200f };
            float[] amplitudes = { 0.3f, 0.25f, 0.2f, 0.15f, 0.08f, 0.05f };

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                // Slow exponential decay
                float envelope = Mathf.Exp(-3.2f * t);

                float wave = 0f;
                for (int j = 0; j < frequencies.Length; j++)
                {
                    wave += Mathf.Sin(2f * Mathf.PI * frequencies[j] * t) * amplitudes[j];
                }

                sampleArray[i] = wave * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("CinematicChime", samplesCount, 1, sampleRate, false);
            clip.SetData(sampleArray, 0);
            return clip;
        }

        /// <summary>
        /// Displays the glassmorphic Mission Complete UI screen overlay and locks controls.
        /// </summary>
        public void ShowMissionEndUI()
        {
            _isEndScreen = true;
            _typewriterDone = true;

            // Disable controls and show cursor
            SetPlayerControlsEnabled(false);

            // Configure End UI Text
            HeaderTitle = "MISSION COMPLETE";
            SubHeader = "OPERATION SUCCESSFUL";
            BriefingText = "Congratulations agent, you have successfully retrieved the confidential key and escaped the bunker safely. Return to headquarters immediately.";

            if (TitleTextComponent != null) TitleTextComponent.text = HeaderTitle;
            if (SubHeaderComponent != null)
            {
                SubHeaderComponent.text = SubHeader;
                SubHeaderComponent.color = new Color(0.4f, 1f, 0.98f, 1f); // Make subheader teal/green instead of red
            }
            if (BodyTextComponent != null) BodyTextComponent.text = BriefingText;
            if (EnterButtonText != null) EnterButtonText.text = "CLOSE GAME";

            // Hide secondary buttons for the final screen
            if (_helpButtonBorderGo != null) _helpButtonBorderGo.SetActive(false);
            if (_quitButtonBorderGo != null) _quitButtonBorderGo.SetActive(false);
            if (_closeXButtonBorderGo != null) _closeXButtonBorderGo.SetActive(false);

            // Reset button visuals in case it was left hovered
            if (EnterButtonImage != null)
            {
                var effects = EnterButtonImage.GetComponent<MissionButtonEffects>();
                if (effects != null)
                {
                    effects.ResetButtonState();
                }
            }

            // Reactivate the UI panel
            if (IntroCanvasGroup != null)
            {
                IntroCanvasGroup.gameObject.SetActive(true);
                IntroCanvasGroup.alpha = 0f;
                StartCoroutine(FadeInEndCanvasRoutine());
            }
        }

        private IEnumerator FadeInEndCanvasRoutine()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                IntroCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            IntroCanvasGroup.alpha = 1f;
        }
        #endregion
    }

    /// <summary>
    /// Companion helper script to handle button hover scaling, hover glow colors, clicks, and pulsing animations.
    /// </summary>
    public class MissionButtonEffects : MonoBehaviour, 
        UnityEngine.EventSystems.IPointerEnterHandler, 
        UnityEngine.EventSystems.IPointerExitHandler, 
        UnityEngine.EventSystems.IPointerClickHandler
    {
        public MissionIntroUI IntroUI;
        public Image ButtonImage;
        public Text ButtonText;

        public Color NormalBgColor = new Color(0.07f, 0.08f, 0.09f, 0.9f);
        public Color HoverBgColor = new Color(0.4f, 1f, 0.98f, 1.0f); // Bright Cyan
        public Color NormalTextColor = new Color(0.4f, 1f, 0.98f, 1.0f);
        public Color HoverTextColor = new Color(0.04f, 0.05f, 0.06f, 1.0f); // Black text on Cyan bg

        public string ButtonType = "Enter"; // "Enter", "Help", "Close", "CloseHelp"

        private Vector3 _originalScale;
        private bool _isHovered = false;

        private void Start()
        {
            _originalScale = transform.localScale;
            ResetButtonState();
        }

        public void ResetButtonState()
        {
            if (ButtonImage != null) ButtonImage.color = NormalBgColor;
            if (ButtonText != null) ButtonText.color = NormalTextColor;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            _isHovered = true;
            if (ButtonImage != null) ButtonImage.color = HoverBgColor;
            if (ButtonText != null) ButtonText.color = HoverTextColor;
            transform.localScale = _originalScale * 1.05f;

            if (IntroUI != null)
            {
                IntroUI.PlayHoverSound();
            }
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            _isHovered = false;
            if (ButtonImage != null) ButtonImage.color = NormalBgColor;
            if (ButtonText != null) ButtonText.color = NormalTextColor;
            transform.localScale = _originalScale;
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            TriggerClick();
        }

        private void Update()
        {
            // Pulse only the primary action button to focus attention
            if (ButtonType == "Enter" && IntroUI != null && IntroUI.IsTypewriterDone() && !_isHovered)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 3.5f) * 0.022f;
                transform.localScale = _originalScale * pulse;
            }
        }

        private void TriggerClick()
        {
            if (IntroUI != null)
            {
                if (ButtonType == "Enter")
                {
                    IntroUI.OnButtonClicked();
                }
                else if (ButtonType == "Help")
                {
                    IntroUI.OnHelpButtonClicked();
                }
                else if (ButtonType == "Close")
                {
                    IntroUI.OnCloseButtonClicked();
                }
                else if (ButtonType == "CloseHelp")
                {
                    IntroUI.OnCloseHelpButtonClicked();
                }
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BunkerTools
{
    /// <summary>
    /// MissionCoordinatorHUD dynamically constructs and manages the tactical HUD in the bottom-right corner of the screen.
    /// It displays active transmissions and permanent objectives.
    /// </summary>
    public class MissionCoordinatorHUD : MonoBehaviour
    {
        public static MissionCoordinatorHUD Instance { get; private set; }

        [Header("Visual Colors")]
        public Color CyanAccent = new Color(0.4f, 1f, 0.98f, 1f);
        public Color TextColor = new Color(0.88f, 0.9f, 0.92f, 1f);
        public Color BlockerBgColor = new Color(0.04f, 0.05f, 0.06f, 0.88f);
        public Color TransmissionDotColor = new Color(0.18f, 0.8f, 0.44f, 1f); // Green
        public Color ObjectiveDotColor = new Color(0.95f, 0.6f, 0.07f, 1f);    // Orange/Amber

        [Header("Components")]
        public CanvasGroup HUDCanvasGroup;
        public Text HeaderTextComponent;
        public Text BodyTextComponent;
        public Image StatusDotComponent;

        private bool _isTransmissionActive = false;
        private Coroutine _typewriterCoroutine;
        private string _targetBodyText = "";
        private float _pulseTimer = 0f;

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

            ConstructHUD();
            SetHUDAlpha(0f); // Hidden initially
        }

        private void Update()
        {
            // Handle indicator dot pulsing
            if (StatusDotComponent != null && HUDCanvasGroup.alpha > 0.01f)
            {
                _pulseTimer += Time.deltaTime;
                if (_isTransmissionActive)
                {
                    // Rapid pulse for active signal
                    float alpha = 0.3f + Mathf.PingPong(_pulseTimer * 5.5f, 0.7f);
                    StatusDotComponent.color = new Color(TransmissionDotColor.r, TransmissionDotColor.g, TransmissionDotColor.b, alpha);
                }
                else
                {
                    // Slow breathing pulse for objective standby
                    float alpha = 0.5f + Mathf.PingPong(_pulseTimer * 1.8f, 0.5f);
                    StatusDotComponent.color = new Color(ObjectiveDotColor.r, ObjectiveDotColor.g, ObjectiveDotColor.b, alpha);
                }
            }
        }

        public void SetHUDAlpha(float alpha)
        {
            if (HUDCanvasGroup != null)
            {
                HUDCanvasGroup.alpha = alpha;
                HUDCanvasGroup.blocksRaycasts = alpha > 0.01f;
            }
        }

        /// <summary>
        /// Displays an active coordinator transmission on the HUD with typewriter text.
        /// </summary>
        public void ShowTransmission(string header, string text, float typewriterSpeed, AudioClip voiceBeepClip)
        {
            _isTransmissionActive = true;
            if (StatusDotComponent != null) StatusDotComponent.color = TransmissionDotColor;
            HeaderTextComponent.text = header.ToUpper();
            HeaderTextComponent.color = CyanAccent;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterHUDText(text, typewriterSpeed, voiceBeepClip));
        }

        /// <summary>
        /// Switches the HUD to static objective tracking state.
        /// </summary>
        public void ShowObjective(string header, string text)
        {
            _isTransmissionActive = false;
            if (StatusDotComponent != null) StatusDotComponent.color = ObjectiveDotColor;
            HeaderTextComponent.text = header.ToUpper();
            HeaderTextComponent.color = ObjectiveDotColor;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            BodyTextComponent.text = text;
        }

        private IEnumerator TypewriterHUDText(string text, float speed, AudioClip beepClip)
        {
            BodyTextComponent.text = "";
            string current = "";
            int charIndex = 0;

            while (charIndex < text.Length)
            {
                current += text[charIndex];
                BodyTextComponent.text = current + " █";
                charIndex++;

                if (charIndex % 3 == 0 && beepClip != null)
                {
                    // Procedural/voice chirping audio
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(beepClip, 0.15f);
                    else
                        AudioSource.PlayClipAtPoint(beepClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.15f);
                }

                yield return new WaitForSeconds(speed);
            }

            BodyTextComponent.text = text;
            _typewriterCoroutine = null;
        }

        public void FadeInHUD(float duration)
        {
            StartCoroutine(FadeHUDRoutine(1f, duration));
        }

        public void FadeOutHUD(float duration)
        {
            StartCoroutine(FadeHUDRoutine(0f, duration));
        }

        private IEnumerator FadeHUDRoutine(float targetAlpha, float duration)
        {
            if (HUDCanvasGroup == null) yield break;

            float startAlpha = HUDCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                HUDCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            HUDCanvasGroup.alpha = targetAlpha;
        }

        #region Runtime HUD Dynamic Creation
        private void ConstructHUD()
        {
            Debug.Log("[MissionCoordinatorHUD] Dynamically building bottom-right HUD.");

            // 1. Root Canvas
            GameObject canvasGo = new GameObject("CoordinatorHUDCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800; // Above standard UI but below transition overlay

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            HUDCanvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // 2. HUD Panel container (anchored Bottom-Right)
            GameObject panelBorderGo = new GameObject("HUDPanelBorder");
            panelBorderGo.transform.SetParent(canvasGo.transform, false);

            Image borderImage = panelBorderGo.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 1f, 0.98f, 0.15f); // Subtle outline

            RectTransform borderRect = borderImage.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(1f, 0f);
            borderRect.anchorMax = new Vector2(1f, 0f);
            borderRect.pivot = new Vector2(1f, 0f);
            borderRect.anchoredPosition = new Vector2(-40f, 40f); // 40px padding from right/bottom
            borderRect.sizeDelta = new Vector2(404f, 184f); // Bounding dimensions

            GameObject panelGo = new GameObject("HUDPanel");
            panelGo.transform.SetParent(panelBorderGo.transform, false);

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = BlockerBgColor;

            RectTransform panelRect = panelImage.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = new Vector2(-4f, -4f); // 2px border gap

            // Add brackets at the corners of HUD panel
            AddHUDCornerBrackets(panelGo.transform, 400f, 180f);

            // 3. Status indicator dot (Flashing green/orange)
            GameObject dotGo = new GameObject("StatusDot");
            dotGo.transform.SetParent(panelGo.transform, false);
            StatusDotComponent = dotGo.AddComponent<Image>();
            StatusDotComponent.color = TransmissionDotColor;

            // Make it circular
            Texture2D circleTex = CreateDotTexture(16);
            StatusDotComponent.sprite = Sprite.Create(circleTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));

            RectTransform dotRect = StatusDotComponent.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 1f);
            dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 1f);
            dotRect.anchoredPosition = new Vector2(25f, -22f);
            dotRect.sizeDelta = new Vector2(12f, 12f);

            // 4. Header status text
            GameObject headerGo = new GameObject("HeaderText");
            headerGo.transform.SetParent(panelGo.transform, false);
            HeaderTextComponent = headerGo.AddComponent<Text>();
            HeaderTextComponent.text = "RADIO TRANSMISSION // ACTIVE";
            HeaderTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            HeaderTextComponent.fontSize = 15;
            HeaderTextComponent.fontStyle = FontStyle.Bold;
            HeaderTextComponent.color = CyanAccent;
            HeaderTextComponent.alignment = TextAnchor.MiddleLeft;
            HeaderTextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            HeaderTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(45f, -18f); // Positioned to the right of dot
            headerRect.sizeDelta = new Vector2(-70f, 22f);

            // Sub-divider line
            GameObject lineGo = new GameObject("HUDDivider");
            lineGo.transform.SetParent(panelGo.transform, false);
            Image lineImage = lineGo.AddComponent<Image>();
            lineImage.color = new Color(0.4f, 1f, 0.98f, 0.12f);

            RectTransform lineRect = lineImage.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(0f, -42f);
            lineRect.sizeDelta = new Vector2(-40f, 1f);

            // 5. Dialogue/Objective description text
            GameObject bodyGo = new GameObject("HUDBodyText");
            bodyGo.transform.SetParent(panelGo.transform, false);
            BodyTextComponent = bodyGo.AddComponent<Text>();
            BodyTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BodyTextComponent.fontSize = 15;
            BodyTextComponent.color = TextColor;
            BodyTextComponent.alignment = TextAnchor.UpperLeft;
            BodyTextComponent.lineSpacing = 1.3f;
            BodyTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            BodyTextComponent.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, -38f);
            bodyRect.sizeDelta = new Vector2(-40f, -90f);
        }

        private void AddHUDCornerBrackets(Transform parent, float width, float height)
        {
            Color color = new Color(0.4f, 1f, 0.98f, 0.6f);
            float length = 12f;
            float thickness = 2f;

            // TL
            CreateHUDBracketCorner(parent, new Vector2(-width/2, height/2), new Vector2(1, -1), length, thickness, color);
            // TR
            CreateHUDBracketCorner(parent, new Vector2(width/2, height/2), new Vector2(-1, -1), length, thickness, color);
            // BL
            CreateHUDBracketCorner(parent, new Vector2(-width/2, -height/2), new Vector2(1, 1), length, thickness, color);
            // BR
            CreateHUDBracketCorner(parent, new Vector2(width/2, -height/2), new Vector2(-1, 1), length, thickness, color);
        }

        private void CreateHUDBracketCorner(Transform parent, Vector2 pos, Vector2 direction, float len, float thick, Color color)
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

        private Texture2D CreateDotTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius - 1f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else if (dist < radius)
                    {
                        // Anti-aliased border
                        float alpha = 1f - (dist - (radius - 1f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
        #endregion
    }
}

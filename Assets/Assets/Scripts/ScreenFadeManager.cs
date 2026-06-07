using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BunkerTools
{
    /// <summary>
    /// ScreenFadeManager handles smooth transition fades (fade to black / fade from black).
    /// It dynamically constructs a full-screen overlay Canvas if one is not configured in the scene.
    /// </summary>
    public class ScreenFadeManager : MonoBehaviour
    {
        public static ScreenFadeManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("The CanvasGroup to fade. If left null, a default one will be created dynamically at runtime.")]
        public CanvasGroup FadeCanvasGroup;

        [Tooltip("Initial opacity when the scene starts.")]
        [Range(0f, 1f)]
        public float StartAlpha = 1f;

        private Coroutine _fadeCoroutine;

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

            InitializeFadeUI();
        }

        private void InitializeFadeUI()
        {
            if (FadeCanvasGroup == null)
            {
                // Check if a CanvasGroup is already present in children
                FadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
            }

            if (FadeCanvasGroup == null)
            {
                Debug.Log("[ScreenFadeManager] No pre-defined FadeCanvasGroup found. Generating full-screen black overlay dynamically.");

                // Create overlay Canvas
                GameObject fadeCanvasGo = new GameObject("RuntimeFadeCanvas");
                fadeCanvasGo.transform.SetParent(transform);

                Canvas canvas = fadeCanvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; // Always draw on top of everything

                CanvasScaler scaler = fadeCanvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                fadeCanvasGo.AddComponent<GraphicRaycaster>();

                // Add CanvasGroup for fading
                FadeCanvasGroup = fadeCanvasGo.AddComponent<CanvasGroup>();
                FadeCanvasGroup.alpha = StartAlpha;
                FadeCanvasGroup.blocksRaycasts = StartAlpha > 0.01f;
                FadeCanvasGroup.interactable = StartAlpha > 0.01f;

                // Add full screen black Image panel
                GameObject blackPanelGo = new GameObject("BlackOverlay");
                blackPanelGo.transform.SetParent(fadeCanvasGo.transform, false);

                Image image = blackPanelGo.AddComponent<Image>();
                image.color = Color.black;

                RectTransform rectTransform = blackPanelGo.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Apply initial alpha
                FadeCanvasGroup.alpha = StartAlpha;
                FadeCanvasGroup.blocksRaycasts = StartAlpha > 0.01f;
                FadeCanvasGroup.interactable = StartAlpha > 0.01f;
            }
        }

        /// <summary>
        /// Instantly sets the screen opacity.
        /// </summary>
        /// <param name="alpha">Opacity level (0 to 1)</param>
        public void SetAlpha(float alpha)
        {
            if (FadeCanvasGroup == null) return;
            FadeCanvasGroup.alpha = alpha;
            FadeCanvasGroup.blocksRaycasts = alpha > 0.01f;
            FadeCanvasGroup.interactable = alpha > 0.01f;
        }

        /// <summary>
        /// Fades the screen from black to transparent.
        /// </summary>
        public void FadeIn(float duration, Action onComplete = null)
        {
            StartFade(0f, duration, onComplete);
        }

        /// <summary>
        /// Fades the screen from transparent to black.
        /// </summary>
        public void FadeOut(float duration, Action onComplete = null)
        {
            StartFade(1f, duration, onComplete);
        }

        private void StartFade(float targetAlpha, float duration, Action onComplete)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            if (FadeCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = FadeCanvasGroup.alpha;
            float elapsed = 0f;

            // Block interactions during active fade transitions
            FadeCanvasGroup.blocksRaycasts = true;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                FadeCanvasGroup.alpha = currentAlpha;
                yield return null;
            }

            FadeCanvasGroup.alpha = targetAlpha;
            FadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;
            FadeCanvasGroup.interactable = targetAlpha > 0.01f;

            onComplete?.Invoke();
            _fadeCoroutine = null;
        }
    }
}

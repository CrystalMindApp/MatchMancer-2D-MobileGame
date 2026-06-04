using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class TileVisualController : MonoBehaviour
    {
        #region Variables

        [Header("Destroy Animation")]
        [SerializeField, Min(0f)] private float destroyAnimationDuration = 0.1f;
        [SerializeField, Range(0f, 1f)] private float destroyScaleTarget = 0f;
        [SerializeField] private bool destroyFadeEnabled = true;

        [Header("Special Spawn Animation")]
        [SerializeField, Min(0f)] private float specialSpawnDuration = 0.16f;
        [SerializeField, Min(1f)] private float specialSpawnOvershootScale = 1.15f;

        [Header("Selection Pulse")]
        [SerializeField, Range(1f, 1.5f)] private float selectedPulseScale = 1.08f;
        [SerializeField, Min(0f)] private float selectedPulseSpeed = 2.5f;

        [Header("Special Glow")]
        [SerializeField] private bool enableSpecialGlow = true;
        [SerializeField] private Color specialGlowColor = Color.white;
        [SerializeField, Range(0f, 2f)] private float specialGlowIntensity = 0.25f;
        [SerializeField, Min(0f)] private float specialGlowPulseSpeed = 1.8f;

        [Header("Curse Debug Visual")]
        [Tooltip("Temporary debug tint multiplied with the tile base color while the tile has a curse.")]
        [SerializeField] private Color curseDebugTint = new Color(0.35f, 0.35f, 0.35f, 1f);
        [Tooltip("Optional material restored on the tile body when the tile is not cursed. If empty, the original body material is cached at runtime.")]
        [SerializeField] private Material defaultBodyMaterial;
        [Tooltip("Optional material applied only to the tile body while cursed. Frame and icon renderers are not modified.")]
        [SerializeField] private Material curseBodyMaterial;

        [Header("Curse Icon")]
        [SerializeField] private SpriteRenderer curseIconRenderer;
        [SerializeField, Min(0f)] private float curseIconIntroDuration = 0.2f;
        [SerializeField, Min(1f)] private float curseIconIntroStartScale = 1.5f;
        [SerializeField, Min(0f)] private float curseIconIntroTargetScale = 0.5f;
        [SerializeField, Min(0f)] private float curseIconIntroDelay;

        [Header("Curse Application VFX")]
        [SerializeField] private GameObject curseApplicationVfxPrefab;
        [SerializeField, Min(0f)] private float curseApplicationVfxLifetime = 0.75f;
        [SerializeField] private Transform curseApplicationVfxParent;

        [Header("Enhanced Frame")]
        [Tooltip("Optional overlay/frame shown only while this tile is both special and Enhanced. Curse tint remains on the tile body.")]
        [SerializeField] private GameObject enhancedFrameRoot;

        [Header("References")]
        [SerializeField] private SpriteRenderer targetRenderer;

        // Cache
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;
        private Vector3 defaultScale = Vector3.one;
        private Vector3 curseIconDefaultScale = Vector3.one;
        private Color curseIconDefaultColor = Color.white;
        private Material cachedDefaultBodyMaterial;
        private Coroutine selectedPulseRoutine;
        private Coroutine specialSpawnRoutine;
        private Coroutine curseIconIntroRoutine;

        // State
        private bool isSelected;
        private bool isSpecial;
        private bool isEnhancedSpecial;
        private bool isCursed;
        private bool supportsMaterialColor;
        private CurseEffectData currentCurseEffect;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            CaptureDefaults();
            SetEnhancedFrameVisible(false);
        }

        private void Update()
        {
            UpdateSpecialGlow();
        }

        #endregion

        #region Public Methods

        public void ResetVisualState()
        {
            StopRunningVisualRoutines();
            isSelected = false;
            isSpecial = false;
            isEnhancedSpecial = false;
            isCursed = false;
            currentCurseEffect = null;
            SetEnhancedFrameVisible(false);
            SetCurseIconVisible(false);

            if (targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(null);
                RestoreDefaultBodyMaterial();
                targetRenderer.color = baseColor;
            }

            transform.localScale = defaultScale;
        }

        public void SetTargetRenderer(SpriteRenderer renderer)
        {
            if (renderer == null || targetRenderer == renderer)
            {
                return;
            }

            targetRenderer = renderer;
            supportsMaterialColor = targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty(ColorPropertyId);
            CaptureCurrentRendererColor();
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected)
            {
                return;
            }

            isSelected = selected;

            if (selectedPulseRoutine != null)
            {
                StopCoroutine(selectedPulseRoutine);
                selectedPulseRoutine = null;
            }

            if (selected)
            {
                selectedPulseRoutine = StartCoroutine(SelectedPulseRoutine());
                return;
            }

            transform.localScale = defaultScale;
        }

        public void SetSpecialState(bool special, bool enhanced, bool playSpawnAnimation)
        {
            bool becameSpecial = special && !isSpecial;
            isSpecial = special;
            isEnhancedSpecial = special && enhanced;
            UpdateEnhancedFrame();

            if (becameSpecial && playSpawnAnimation)
            {
                PlaySpecialSpawnAnimation();
            }

            if (!isSpecial)
            {
                ClearSpecialGlow();
                return;
            }

            ApplyCurrentRendererColor();
        }

        public void SetSpecialState(bool special, bool playSpawnAnimation)
        {
            SetSpecialState(special, false, playSpawnAnimation);
        }

        public void SetCurseState(bool cursed)
        {
            SetCurseState(cursed ? currentCurseEffect : null, false);
        }

        public void SetCurseState(CurseEffectData curseEffect)
        {
            SetCurseState(curseEffect, true);
        }

        public void SetCurseState(CurseEffectData curseEffect, bool playIntroAnimation)
        {
            bool cursed = curseEffect != null;
            bool curseChanged = currentCurseEffect != curseEffect;

            if (isCursed == cursed && !curseChanged)
            {
                return;
            }

            isCursed = cursed;
            currentCurseEffect = curseEffect;
            ApplyCurrentRendererColor();
            ApplyCurseIcon(curseEffect, playIntroAnimation && cursed && (!IsCurseIconVisible() || curseChanged));

            if (cursed && (!IsCurseIconVisible() || curseChanged))
            {
                SpawnCurseApplicationVfx();
            }
        }

        public IEnumerator PlayDestroyAnimation()
        {
            StopRunningVisualRoutines();
            isSelected = false;
            isSpecial = false;
            isEnhancedSpecial = false;
            SetEnhancedFrameVisible(false);
            ClearSpecialGlow();

            float safeDuration = Mathf.Max(0f, destroyAnimationDuration);
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = defaultScale * destroyScaleTarget;
            Color startColor = targetRenderer != null ? targetRenderer.color : baseColor;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            if (safeDuration <= 0f)
            {
                ApplyDestroyVisual(targetScale, targetColor);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                ApplyDestroyVisual(Vector3.Lerp(startScale, targetScale, time), Color.Lerp(startColor, targetColor, time));
                yield return null;
            }

            ApplyDestroyVisual(targetScale, targetColor);
        }

        public IEnumerator PlayIntroAnimation(float duration, float startScale, float overshootScale, float endScale, float startDelay, bool useUnscaledTime)
        {
            StopRunningVisualRoutines();
            isSelected = false;

            float safeDelay = Mathf.Max(0f, startDelay);
            while (safeDelay > 0f)
            {
                safeDelay -= GetDeltaTime(useUnscaledTime);
                yield return null;
            }

            float safeDuration = Mathf.Max(0f, duration);
            transform.localScale = defaultScale * Mathf.Max(0f, startScale);

            if (safeDuration <= 0f)
            {
                transform.localScale = defaultScale * Mathf.Max(0f, endScale);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += GetDeltaTime(useUnscaledTime);
                float time = Mathf.Clamp01(elapsed / safeDuration);
                transform.localScale = defaultScale * EvaluateIntroScale(time, startScale, overshootScale, endScale);
                yield return null;
            }

            transform.localScale = defaultScale * Mathf.Max(0f, endScale);
        }

        public void CaptureCurrentRendererColor()
        {
            if (targetRenderer != null)
            {
                baseColor = targetRenderer.color;
                ApplyCurrentRendererColor();
            }
        }

        public void SetScaleMultiplier(float scaleMultiplier)
        {
            transform.localScale = defaultScale * Mathf.Max(0f, scaleMultiplier);
        }

        #endregion

        #region Private Methods

        private void CacheReferences()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            supportsMaterialColor = targetRenderer != null &&
                targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty(ColorPropertyId);
        }

        private void CaptureDefaults()
        {
            defaultScale = transform.localScale;

            if (targetRenderer != null)
            {
                baseColor = targetRenderer.color;
                CacheDefaultBodyMaterialIfNeeded();
            }

            if (curseIconRenderer != null)
            {
                curseIconDefaultScale = curseIconRenderer.transform.localScale;
                curseIconDefaultColor = curseIconRenderer.color;
                SetCurseIconVisible(false);
            }
        }

        private void StopRunningVisualRoutines()
        {
            if (selectedPulseRoutine != null)
            {
                StopCoroutine(selectedPulseRoutine);
                selectedPulseRoutine = null;
            }

            if (specialSpawnRoutine != null)
            {
                StopCoroutine(specialSpawnRoutine);
                specialSpawnRoutine = null;
            }

            if (curseIconIntroRoutine != null)
            {
                StopCoroutine(curseIconIntroRoutine);
                curseIconIntroRoutine = null;
            }
        }

        private IEnumerator SelectedPulseRoutine()
        {
            while (isSelected)
            {
                float pulse = (Mathf.Sin(Time.time * selectedPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(1f, selectedPulseScale, pulse);
                transform.localScale = defaultScale * scale;
                yield return null;
            }
        }

        private void PlaySpecialSpawnAnimation()
        {
            transform.localScale = defaultScale * 0.2f;

            if (specialSpawnRoutine != null)
            {
                StopCoroutine(specialSpawnRoutine);
            }

            specialSpawnRoutine = StartCoroutine(SpecialSpawnRoutine());
        }

        private IEnumerator SpecialSpawnRoutine()
        {
            float safeDuration = Mathf.Max(0f, specialSpawnDuration);

            if (safeDuration <= 0f)
            {
                transform.localScale = defaultScale;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);

                if (time < 0.65f)
                {
                    float firstPhase = Mathf.Clamp01(time / 0.65f);
                    transform.localScale = defaultScale * Mathf.Lerp(0.2f, specialSpawnOvershootScale, firstPhase);
                }
                else
                {
                    float secondPhase = Mathf.Clamp01((time - 0.65f) / 0.35f);
                    transform.localScale = defaultScale * Mathf.Lerp(specialSpawnOvershootScale, 1f, secondPhase);
                }

                yield return null;
            }

            transform.localScale = defaultScale;
            specialSpawnRoutine = null;
        }

        private void UpdateSpecialGlow()
        {
            if (!enableSpecialGlow || !isSpecial || targetRenderer == null)
            {
                return;
            }

            float pulse = (Mathf.Sin(Time.time * specialGlowPulseSpeed) + 1f) * 0.5f;
            float intensity = specialGlowIntensity * pulse;
            Color glowColor = Color.Lerp(GetDisplayBaseColor(), specialGlowColor, intensity);

            if (supportsMaterialColor)
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorPropertyId, glowColor);
                targetRenderer.SetPropertyBlock(propertyBlock);
                return;
            }

            targetRenderer.color = glowColor;
        }

        private void ClearSpecialGlow()
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.SetPropertyBlock(null);
            ApplyCurrentRendererColor();
        }

        private void ApplyCurrentRendererColor()
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.SetPropertyBlock(null);
            ApplyBodyMaterial();
            targetRenderer.color = GetDisplayBaseColor();
        }

        private void ApplyBodyMaterial()
        {
            if (targetRenderer == null)
            {
                return;
            }

            CacheDefaultBodyMaterialIfNeeded();
            Material targetMaterial = isCursed && curseBodyMaterial != null
                ? curseBodyMaterial
                : GetDefaultBodyMaterial();

            if (targetMaterial != null && targetRenderer.sharedMaterial != targetMaterial)
            {
                targetRenderer.sharedMaterial = targetMaterial;
            }

            supportsMaterialColor = targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty(ColorPropertyId);
        }

        private void RestoreDefaultBodyMaterial()
        {
            if (targetRenderer == null)
            {
                return;
            }

            Material targetMaterial = GetDefaultBodyMaterial();

            if (targetMaterial != null && targetRenderer.sharedMaterial != targetMaterial)
            {
                targetRenderer.sharedMaterial = targetMaterial;
            }

            supportsMaterialColor = targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty(ColorPropertyId);
        }

        private void CacheDefaultBodyMaterialIfNeeded()
        {
            if (targetRenderer == null || cachedDefaultBodyMaterial != null)
            {
                return;
            }

            cachedDefaultBodyMaterial = defaultBodyMaterial != null
                ? defaultBodyMaterial
                : targetRenderer.sharedMaterial;
        }

        private Material GetDefaultBodyMaterial()
        {
            return defaultBodyMaterial != null ? defaultBodyMaterial : cachedDefaultBodyMaterial;
        }

        private void ApplyCurseIcon(CurseEffectData curseEffect, bool playIntroAnimation)
        {
            if (curseIconRenderer == null)
            {
                return;
            }

            if (curseEffect == null)
            {
                SetCurseIconVisible(false);
                return;
            }

            if (curseEffect.Icon != null)
            {
                curseIconRenderer.sprite = curseEffect.Icon;
            }

            SetCurseIconVisible(true);

            if (playIntroAnimation)
            {
                PlayCurseIconIntro();
                return;
            }

            curseIconRenderer.transform.localScale = GetCurseIconTargetScale();
            curseIconRenderer.color = curseIconDefaultColor;
        }

        private void SetCurseIconVisible(bool visible)
        {
            if (curseIconRenderer == null)
            {
                return;
            }

            curseIconRenderer.enabled = visible;

            if (!visible)
            {
                if (curseIconIntroRoutine != null)
                {
                    StopCoroutine(curseIconIntroRoutine);
                    curseIconIntroRoutine = null;
                }

                curseIconRenderer.transform.localScale = curseIconDefaultScale;
                curseIconRenderer.color = new Color(curseIconDefaultColor.r, curseIconDefaultColor.g, curseIconDefaultColor.b, 0f);
            }
        }

        private bool IsCurseIconVisible()
        {
            return curseIconRenderer != null && curseIconRenderer.enabled;
        }

        private void PlayCurseIconIntro()
        {
            if (curseIconRenderer == null)
            {
                return;
            }

            if (curseIconIntroRoutine != null)
            {
                StopCoroutine(curseIconIntroRoutine);
            }

            curseIconIntroRoutine = StartCoroutine(CurseIconIntroRoutine());
        }

        private IEnumerator CurseIconIntroRoutine()
        {
            float safeDelay = Mathf.Max(0f, curseIconIntroDelay);

            while (safeDelay > 0f)
            {
                safeDelay -= Time.deltaTime;
                yield return null;
            }

            float safeDuration = Mathf.Max(0f, curseIconIntroDuration);
            Color startColor = new Color(curseIconDefaultColor.r, curseIconDefaultColor.g, curseIconDefaultColor.b, 0f);
            Color endColor = new Color(curseIconDefaultColor.r, curseIconDefaultColor.g, curseIconDefaultColor.b, curseIconDefaultColor.a);
            Vector3 startScale = curseIconDefaultScale * Mathf.Max(0f, curseIconIntroStartScale);
            Vector3 targetScale = GetCurseIconTargetScale();
            curseIconRenderer.transform.localScale = startScale;
            curseIconRenderer.color = startColor;

            if (safeDuration <= 0f)
            {
                curseIconRenderer.transform.localScale = targetScale;
                curseIconRenderer.color = endColor;
                curseIconIntroRoutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / safeDuration);
                curseIconRenderer.transform.localScale = Vector3.Lerp(startScale, targetScale, time);
                curseIconRenderer.color = Color.Lerp(startColor, endColor, time);
                yield return null;
            }

            curseIconRenderer.transform.localScale = targetScale;
            curseIconRenderer.color = endColor;
            curseIconIntroRoutine = null;
        }

        private Vector3 GetCurseIconTargetScale()
        {
            return curseIconDefaultScale * Mathf.Max(0f, curseIconIntroTargetScale);
        }

        private void SpawnCurseApplicationVfx()
        {
            if (curseApplicationVfxPrefab == null)
            {
                return;
            }

            GameObject effectInstance = Instantiate(
                curseApplicationVfxPrefab,
                transform.position,
                Quaternion.identity,
                curseApplicationVfxParent);

            if (curseApplicationVfxLifetime > 0f)
            {
                Destroy(effectInstance, curseApplicationVfxLifetime);
            }
        }

        private void UpdateEnhancedFrame()
        {
            SetEnhancedFrameVisible(isSpecial && isEnhancedSpecial);
        }

        private void SetEnhancedFrameVisible(bool visible)
        {
            if (enhancedFrameRoot != null)
            {
                enhancedFrameRoot.SetActive(visible);
            }
        }

        private Color GetDisplayBaseColor()
        {
            Color tintColor = Color.white;

            if (isCursed)
            {
                tintColor = curseDebugTint;
            }

            Color tintedColor = baseColor * tintColor;
            tintedColor.a = baseColor.a;
            return tintedColor;
        }

        private void ApplyDestroyVisual(Vector3 scale, Color fadeColor)
        {
            transform.localScale = scale;

            if (destroyFadeEnabled && targetRenderer != null)
            {
                targetRenderer.color = fadeColor;
            }
        }

        private float EvaluateIntroScale(float time, float startScale, float overshootScale, float endScale)
        {
            if (time < 0.65f)
            {
                float firstPhase = Mathf.Clamp01(time / 0.65f);
                return Mathf.Lerp(Mathf.Max(0f, startScale), Mathf.Max(0f, overshootScale), firstPhase);
            }

            float secondPhase = Mathf.Clamp01((time - 0.65f) / 0.35f);
            return Mathf.Lerp(Mathf.Max(0f, overshootScale), Mathf.Max(0f, endScale), secondPhase);
        }

        private float GetDeltaTime(bool useUnscaledTime)
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        #endregion
    }
}

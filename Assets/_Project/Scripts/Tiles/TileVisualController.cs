using System.Collections;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class TileVisualController : MonoBehaviour
    {
        #region Variables

        [Header("Destroy Animation")]
        [SerializeField] private ParticleSystem destroyParticlePrefab;
        [SerializeField, Min(0f)] private float destroyAnimationDuration = 0.1f;
        [SerializeField, Range(0f, 1f)] private float destroyScaleTarget = 0f;
        [SerializeField] private bool destroyFadeEnabled = true;

        [Header("Special Spawn Animation")]
        [SerializeField] private ParticleSystem specialSpawnParticlePrefab;
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

        [Header("References")]
        [SerializeField] private SpriteRenderer targetRenderer;

        // Cache
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;
        private Vector3 defaultScale = Vector3.one;
        private Coroutine selectedPulseRoutine;
        private Coroutine specialSpawnRoutine;

        // State
        private bool isSelected;
        private bool isSpecial;
        private bool isCursed;
        private bool supportsMaterialColor;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            CacheReferences();
            CaptureDefaults();
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
            isCursed = false;

            if (targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(null);
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

        public void SetSpecialState(bool special, bool playSpawnAnimation)
        {
            bool becameSpecial = special && !isSpecial;
            isSpecial = special;

            if (becameSpecial && playSpawnAnimation)
            {
                PlaySpecialSpawnAnimation();
            }

            if (!isSpecial)
            {
                ClearSpecialGlow();
            }
        }

        public void SetCurseState(bool cursed)
        {
            if (isCursed == cursed)
            {
                return;
            }

            isCursed = cursed;
            ApplyCurrentRendererColor();
        }

        public IEnumerator PlayDestroyAnimation()
        {
            StopRunningVisualRoutines();
            isSelected = false;
            isSpecial = false;
            ClearSpecialGlow();
            SpawnParticle(destroyParticlePrefab);

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
            SpawnParticle(specialSpawnParticlePrefab);
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
            targetRenderer.color = GetDisplayBaseColor();
        }

        private Color GetDisplayBaseColor()
        {
            if (!isCursed)
            {
                return baseColor;
            }

            Color tintedColor = baseColor * curseDebugTint;
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

        private void SpawnParticle(ParticleSystem particlePrefab)
        {
            if (particlePrefab == null)
            {
                return;
            }

            ParticleSystem particleInstance = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            particleInstance.Play();

            float lifetime = particleInstance.main.duration + particleInstance.main.startLifetime.constantMax;
            Destroy(particleInstance.gameObject, lifetime);
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

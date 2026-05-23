using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class VfxMaterialTintApplier : MonoBehaviour
    {
        #region Variables

        [Header("Settings")]
        [SerializeField, Min(0f)] private float defaultEmissionIntensity = 1f;
        [SerializeField] private bool applySpriteRendererColor = true;

        [Header("References")]
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        // Cache
        private readonly List<Material> createdMaterials = new List<Material>();
        private readonly Dictionary<SpriteRenderer, Material> materialInstances = new Dictionary<SpriteRenderer, Material>();
        private bool hasCachedRenderers;

        #endregion

        #region Unity Methods

        private void OnDestroy()
        {
            for (int i = 0; i < createdMaterials.Count; i++)
            {
                if (createdMaterials[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(createdMaterials[i]);
                }
                else
                {
                    DestroyImmediate(createdMaterials[i]);
                }
            }

            createdMaterials.Clear();
            materialInstances.Clear();
        }

        #endregion

        #region Public Methods

        public void ApplyTint(Color tintColor)
        {
            ApplyTint(tintColor, defaultEmissionIntensity);
        }

        public void ApplyTint(Color tintColor, float emissionIntensity)
        {
            CacheRenderersIfNeeded();

            if (spriteRenderers == null)
            {
                return;
            }

            float safeEmissionIntensity = Mathf.Max(0f, emissionIntensity);

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer == null)
                {
                    continue;
                }

                if (applySpriteRendererColor)
                {
                    spriteRenderer.color = tintColor;
                }

                Material materialInstance = GetOrCreateMaterialInstance(spriteRenderer);
                ApplyTintToMaterial(materialInstance, tintColor, safeEmissionIntensity);
            }
        }

        #endregion

        #region Private Methods

        private void CacheRenderersIfNeeded()
        {
            if (hasCachedRenderers)
            {
                return;
            }

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            hasCachedRenderers = true;
        }

        private Material GetOrCreateMaterialInstance(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
            {
                return null;
            }

            if (materialInstances.TryGetValue(spriteRenderer, out Material existingMaterial))
            {
                return existingMaterial;
            }

            Material sharedMaterial = spriteRenderer.sharedMaterial;

            if (sharedMaterial == null)
            {
                return null;
            }

            Material materialInstance = Instantiate(sharedMaterial);
            spriteRenderer.material = materialInstance;
            createdMaterials.Add(materialInstance);
            materialInstances[spriteRenderer] = materialInstance;
            return materialInstance;
        }

        private void ApplyTintToMaterial(Material material, Color tintColor, float emissionIntensity)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tintColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tintColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                Color emissionColor = tintColor * emissionIntensity;
                emissionColor.a = tintColor.a;
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        #endregion
    }
}

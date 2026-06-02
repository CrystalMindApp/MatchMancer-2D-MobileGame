using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    [CreateAssetMenu(fileName = "SpecialTileSpawnConfig", menuName = "MatchMancer/Board/Special Tile Spawn Config")]
    public class SpecialTileSpawnConfig : ScriptableObject
    {
        #region Variables

        [Header("Settings")]
        [SerializeField] private bool useWeightedPool = true;
        [SerializeField] private bool fallbackToLegacyMapping = true;

        [Header("Pool")]
        [SerializeField] private List<SpecialTileSpawnEntry> entries = new List<SpecialTileSpawnEntry>();

        #endregion

        #region Public Methods

        public bool TrySelectSpecialTileType(out SpecialTileType selectedType)
        {
            selectedType = SpecialTileType.None;

            if (!useWeightedPool)
            {
                return false;
            }

            int totalWeight = GetTotalWeight();

            if (totalWeight <= 0)
            {
                return false;
            }

            return TrySelectFromWeightedPool(totalWeight, out selectedType);
        }

        public bool TrySelectSpecialTileType(SpecialTileType legacyType, out SpecialTileType selectedType)
        {
            selectedType = SpecialTileType.None;

            if (!useWeightedPool)
            {
                return TryUseLegacyMapping(legacyType, out selectedType);
            }

            int totalWeight = GetTotalWeight();

            if (totalWeight <= 0)
            {
                return TryUseLegacyMapping(legacyType, out selectedType);
            }

            if (TrySelectFromWeightedPool(totalWeight, out selectedType))
            {
                return true;
            }

            return TryUseLegacyMapping(legacyType, out selectedType);
        }

        public bool HasValidPool()
        {
            return useWeightedPool && GetTotalWeight() > 0;
        }

        public bool ValidatePool(out string validationMessage)
        {
            if (!useWeightedPool)
            {
                validationMessage = "Weighted pool is disabled. Legacy mapping will be used.";
                return true;
            }

            if (HasValidPool())
            {
                validationMessage = "Weighted pool has valid entries.";
                return true;
            }

            if (fallbackToLegacyMapping)
            {
                validationMessage = "Weighted pool has no valid entries. Legacy mapping fallback is enabled.";
                return true;
            }

            validationMessage = "Weighted pool has no valid entries and legacy mapping fallback is disabled.";
            return false;
        }

        public int GetTotalWeight()
        {
            int totalWeight = 0;

            if (entries == null)
            {
                return totalWeight;
            }

            foreach (SpecialTileSpawnEntry entry in entries)
            {
                if (entry.IsValid)
                {
                    totalWeight += entry.Weight;
                }
            }

            return totalWeight;
        }

        #endregion

        #region Private Methods

        private bool TrySelectFromWeightedPool(int totalWeight, out SpecialTileType selectedType)
        {
            selectedType = SpecialTileType.None;

            if (totalWeight <= 0)
            {
                return false;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (SpecialTileSpawnEntry entry in entries)
            {
                if (!entry.IsValid)
                {
                    continue;
                }

                currentWeight += entry.Weight;

                if (roll >= currentWeight)
                {
                    continue;
                }

                selectedType = entry.SpecialType;
                return selectedType != SpecialTileType.None;
            }

            return false;
        }

        private bool TryUseLegacyMapping(SpecialTileType legacyType, out SpecialTileType selectedType)
        {
            selectedType = fallbackToLegacyMapping ? legacyType : SpecialTileType.None;
            return selectedType != SpecialTileType.None;
        }

        #endregion

        [Serializable]
        public struct SpecialTileSpawnEntry
        {
            #region Variables

            [SerializeField] private SpecialTileType specialType;
            [SerializeField, Min(0)] private int weight;

            #endregion

            #region Properties

            public SpecialTileType SpecialType => specialType;
            public int Weight => Mathf.Max(0, weight);
            public bool IsValid => specialType != SpecialTileType.None && Weight > 0;

            #endregion
        }
    }
}

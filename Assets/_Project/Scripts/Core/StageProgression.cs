using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public static class StageProgression
    {
        #region Variables

        // State
        private static readonly HashSet<int> unlockedStages = new HashSet<int> { 0 };
        private static readonly HashSet<int> clearedStages = new HashSet<int>();
        private static readonly Dictionary<int, int> stageStars = new Dictionary<int, int>();

        #endregion

        #region Public Methods

        public static bool IsStageUnlocked(int stageIndex)
        {
            return stageIndex >= 0 && unlockedStages.Contains(stageIndex);
        }

        public static bool IsStageCleared(int stageIndex)
        {
            return stageIndex >= 0 && clearedStages.Contains(stageIndex);
        }

        public static void UnlockStage(int stageIndex)
        {
            if (stageIndex < 0)
            {
                Debug.LogWarning($"StageProgression: Cannot unlock invalid stage index {stageIndex}.");
                return;
            }

            if (unlockedStages.Add(stageIndex))
            {
                Debug.Log($"Stage unlocked: {stageIndex}");
            }
        }

        public static void MarkStageCleared(int stageIndex)
        {
            if (stageIndex < 0)
            {
                Debug.LogWarning($"StageProgression: Cannot clear invalid stage index {stageIndex}.");
                return;
            }

            clearedStages.Add(stageIndex);
            Debug.Log($"Stage cleared: {stageIndex}");
        }

        public static void SetStageStars(int stageIndex, int stars)
        {
            if (stageIndex < 0)
            {
                Debug.LogWarning($"StageProgression: Cannot set stars for invalid stage index {stageIndex}.");
                return;
            }

            int clampedStars = Mathf.Clamp(stars, 0, 3);
            int currentStars = GetStageStars(stageIndex);

            if (clampedStars <= currentStars)
            {
                Debug.Log($"Stage stars unchanged: {stageIndex} kept {currentStars} star(s).");
                return;
            }

            stageStars[stageIndex] = clampedStars;
            Debug.Log($"Stage stars updated: {stageIndex} = {clampedStars}");
        }

        public static int GetStageStars(int stageIndex)
        {
            if (stageIndex < 0)
            {
                return 0;
            }

            return stageStars.TryGetValue(stageIndex, out int stars)
                ? Mathf.Clamp(stars, 0, 3)
                : 0;
        }

        public static void LogProgressionState()
        {
            Debug.Log($"StageProgression: Unlocked [{BuildSetLog(unlockedStages)}], Cleared [{BuildSetLog(clearedStages)}], Stars [{BuildStarsLog()}]");
        }

        public static void ResetProgression()
        {
            unlockedStages.Clear();
            clearedStages.Clear();
            stageStars.Clear();
            unlockedStages.Add(0);
            StageSession.ClearPendingStageClearVisual();
            Debug.Log("Stage progression reset");
            LogProgressionState();
        }

        #endregion

        #region Private Methods

        private static string BuildSetLog(HashSet<int> stages)
        {
            if (stages == null || stages.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", stages);
        }

        private static string BuildStarsLog()
        {
            if (stageStars.Count == 0)
            {
                return string.Empty;
            }

            List<string> starEntries = new List<string>();

            foreach (KeyValuePair<int, int> stageStar in stageStars)
            {
                starEntries.Add($"{stageStar.Key}:{stageStar.Value}");
            }

            return string.Join(", ", starEntries);
        }

        #endregion
    }
}

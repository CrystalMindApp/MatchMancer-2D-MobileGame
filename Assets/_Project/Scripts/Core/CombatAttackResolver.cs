using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public static class CombatAttackResolver
    {
        #region Public Methods

        public static AttackResult Resolve(
            int baseDamage,
            float damageMultiplier,
            float baseHitChance,
            float hitChancePenalty,
            float critChance,
            float critMultiplier)
        {
            float finalHitChance = Mathf.Clamp01(baseHitChance - hitChancePenalty);

            if (Random.value > finalHitChance)
            {
                return AttackResult.Miss();
            }

            bool isCritical = Random.value < Mathf.Clamp01(critChance);
            float finalMultiplier = Mathf.Max(0f, damageMultiplier);

            if (isCritical)
            {
                finalMultiplier *= Mathf.Max(1f, critMultiplier);
            }

            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseDamage) * finalMultiplier));
            return AttackResult.Hit(finalDamage, isCritical);
        }

        #endregion
    }
}

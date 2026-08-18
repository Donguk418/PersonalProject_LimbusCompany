using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public static class DamageCalculator
    {
        public static int CalculateDamage(
            int skillResultValue,
            AttackType attackType,
            SinAttribute sinAttribute,
            CharacterStat attackerStat,
            CharacterStat defenderStat,
            AttackResistances defenderResistances,
            bool isCritical = false,
            int resonanceStack = 0,
            int clashCount = 0,
            int staggerState = 0,
            float damageMultiplierSum = 0f,
            int fixedDamageOffset = 0)
        {
            float physicalRes = GetPhysicalResistance(defenderResistances, attackType);

            float sinRes = GetSinResistance(sinAttribute);
            float staggerMult = (staggerState > 0) ? (staggerState == 1 ? 2.0f : 3.0f) : 1.0f;

            int levelDiff = (attackerStat != null && defenderStat != null)
                ? (attackerStat.OffenseLevel - defenderStat.DefenseLevel)
                : 0;

            float levelCorrection = (levelDiff >= 0)
                ? (float)levelDiff / (levelDiff + 25f)
                : (float)levelDiff / (-levelDiff + 25f);

            float critMult = isCritical ? 1.2f : 1.0f;
            float resonanceMult = 1.0f + (Mathf.Max(0, resonanceStack - 1) * 0.1f);
            float clashCountMult = 1.0f + (clashCount * 0.03f);
            float finalSumMult = 1.0f + damageMultiplierSum;

            float rawDamage = skillResultValue
                            * physicalRes
                            * sinRes
                            * staggerMult
                            * (1.0f + levelCorrection)
                            * critMult
                            * resonanceMult
                            * clashCountMult
                            * finalSumMult
                            + fixedDamageOffset;

            return Mathf.Max(1, Mathf.FloorToInt(rawDamage));
        }

        private static float GetPhysicalResistance(AttackResistances resistances, AttackType type)
        {
            if (resistances == null) return 1.0f;

            return type switch
            {
                AttackType.Slash => resistances.Slash,
                AttackType.Pierce => resistances.Pierce,
                AttackType.Blunt => resistances.Blunt,
                _ => 1.0f
            };
        }

        private static float GetSinResistance(SinAttribute attribute)
        {
            return attribute switch
            {
                _ => 1.0f     // 기본 내성 : 1.0
            };
        }
    }
}
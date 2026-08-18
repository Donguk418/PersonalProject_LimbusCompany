using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public static class ClashResolver
    {
        public static bool RollCoin(int sanity, float bonusProbability = 0f, bool hasBonus = false)
        {
            float baseProb = 50f + sanity;
            float totalProb = hasBonus ? baseProb + bonusProbability : baseProb;
            float finalProb = Mathf.Clamp(totalProb, 5f, 95f);
            return Random.Range(0f, 100f) < finalProb;
        }

        public static int CalculateSkillPower(SkillData skill, int currentCoins, int sanity, int offenseLevelDiff, float bonusProbability = 0f)
        {
            if (skill == null) return 0;

            int power = skill.BasePower;
            power += Mathf.FloorToInt(offenseLevelDiff / 3f);

            bool hasBonus = bonusProbability != 0f;
            int rollCount = Mathf.Min(currentCoins, skill.Coins.Count);

            for (int i = 0; i < rollCount; i++)
            {
                if (RollCoin(sanity, bonusProbability, hasBonus))
                {
                    power += skill.Coins[i].CoinPower;
                }
            }

            return power;
        }

        public static int CalculateSkillPower(SkillData skill, int currentCoins, CharacterStat attackerStat, CharacterStat defenderStat)
        {
            if (attackerStat == null || defenderStat == null) return 0;

            int offenseLevelDiff = attackerStat.OffenseLevel - defenderStat.OffenseLevel;
            return CalculateSkillPower(skill, currentCoins, attackerStat.CurrentSanity, offenseLevelDiff, attackerStat.BonusCoinProbability);
        }
    }
}
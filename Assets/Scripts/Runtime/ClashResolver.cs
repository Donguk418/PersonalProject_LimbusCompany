using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public static class ClashResolver
    {
        public static bool RollCoin(int sanity, float bonusProbability = 0f, bool hasPassiveBonus = false)
        {
            float baseProbability = 50f + (sanity * 1.0f);
            float minCap = hasPassiveBonus ? 0f : 5f;
            float maxCap = hasPassiveBonus ? 100f : 95f;

            float finalProbability = Mathf.Clamp(baseProbability + bonusProbability, minCap, maxCap);

            return Random.Range(0f, 100f) < finalProbability;
        }

        public static int CalculateSkillPower(SkillData skill, int currentCoinCount, int sanity, int offenseLevelDiff, float bonusCoinProb = 0f)
        {
            int finalPower = skill.BasePower;

            finalPower += offenseLevelDiff / 3;

            bool hasBonus = bonusCoinProb != 0f;

            for (int i = 0; i < currentCoinCount; i++)
            {
                if (i < skill.Coins.Count)
                {
                    bool isHeads = RollCoin(sanity, bonusCoinProb, hasBonus);
                    if (isHeads)
                    {
                        finalPower += skill.Coins[i].CoinPower;
                    }
                }
            }

            return Mathf.Max(0, finalPower);
        }
    }
}
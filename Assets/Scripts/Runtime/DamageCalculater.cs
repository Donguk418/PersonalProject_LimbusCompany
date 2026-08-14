using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public static class DamageCalculator
    {
        public static float GetSinResonanceBonus(int resonanceStack)
        {
            if (resonanceStack <= 0) return 0f;

            return resonanceStack switch
            {
                1 => 0.04f,
                2 => 0.06f,
                3 => 0.08f,
                4 => 0.11f,
                5 => 0.14f,
                6 => 0.18f,
                7 => 0.22f,
                8 => 0.27f,
                9 => 0.33f,
                10 => 0.41f,
                _ => 0.50f
            };
        }

        public static float CalculateLevelModifier(int attackerOffenseLevel, int defenderDefenseLevel)
        {
            float diff = attackerOffenseLevel - defenderDefenseLevel;

            if (diff > 0)
            {
                // 공격자 레벨 우위: diff / (diff + 25) -> 양수(+)
                return diff / (diff + 25f);
            }
            else if (diff < 0)
            {
                // 피격자 레벨 우위: diff / (-diff + 25) -> 음수(-)
                return diff / (-diff + 25f);
            }

            return 0f;
        }

        public static int CalculateDamage(
            int skillResultValue,
            AttackType attackType,
            SinAttribute sinAttribute,
            CharacterRuntime attacker,
            CharacterRuntime defender,
            bool isCritical = false,
            int resonanceStack = 0,
            int clashCount = 0,
            int staggerState = 0,
            float damageMultiplierSum = 0f,
            int fixedDamageOffset = 0
        )
        {
            // 기초 스킬 결과값
            float baseValue = skillResultValue + fixedDamageOffset;

            // 참관타 내성 증감 계수
            float physicalResistModifier;
            if (staggerState > 0)
            {
                physicalResistModifier = staggerState switch
                {
                    1 => 1.0f,  // 흐트러짐
                    2 => 1.5f,  // 흐트러짐+
                    3 => 2.0f,  // 흐트러짐++
                    _ => 1.0f   // 통상
                };
            }
            else
            {
                float rawResist = (defender.CharacterData != null && defender.CharacterData.AttackResistances != null)
                    ? defender.CharacterData.AttackResistances.GetResistance(attackType)
                    : 1.0f;

                physicalResistModifier = rawResist - 1.0f;
            }

            _ = sinAttribute;
            float sinResistModifier = 0f;

            // 공격자 우위 시 +levelModifier 가산
            float levelModifier = CalculateLevelModifier(attacker.OffenseLevel, defender.DefenseLevel);

            float critModifier = isCritical ? 0.2f : 0f;
            float sinResonanceModifier = GetSinResonanceBonus(resonanceStack);
            float clashModifier = clashCount * 0.01f;

            // 중간 계수 : (1 + 참관타 + 죄악 + 공방레벨보정 + 치명타 + 죄악공명 + 합횟수)
            float compositeResistanceTerm = 1f + physicalResistModifier + sinResistModifier + levelModifier + critModifier + sinResonanceModifier + clashModifier;
            compositeResistanceTerm = Mathf.Max(0.05f, compositeResistanceTerm);

            // 피해량 증감 계수
            float finalMultiplierTerm = 1f + damageMultiplierSum;
            finalMultiplierTerm = Mathf.Max(0f, finalMultiplierTerm);

            // 최종 대미지 연산
            float totalDamage = (baseValue * compositeResistanceTerm * finalMultiplierTerm) + fixedDamageOffset;

            return Mathf.Max(1, Mathf.RoundToInt(totalDamage));
        }
    }
}
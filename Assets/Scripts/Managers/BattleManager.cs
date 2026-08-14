using System.Collections;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class BattleManager : MonoBehaviour
    {
        [Header("전투 편성 캐릭터")]
        [SerializeField] private CharacterRuntime _playerCharacter;
        [SerializeField] private CharacterRuntime _enemyCharacter;

        [ContextMenu("단일 합 전투 테스트용")]
        public void TestSingleClashBattle()
        {
            if (_playerCharacter == null || _enemyCharacter == null)
            {
                Debug.LogError("플레이어와 적 캐릭터를 인스펙터에 연결해야 함");
                return;
            }

            StartCoroutine(ProcessClashRoutine(_playerCharacter, _enemyCharacter));
        }

        private IEnumerator ProcessClashRoutine(CharacterRuntime charA, CharacterRuntime charB)
        {
            Debug.Log("전투 시작.");

            SkillData skillA = charA.EquippedSkills.Count > 0 ? charA.EquippedSkills[0] : null;
            SkillData skillB = charB.EquippedSkills.Count > 0 ? charB.EquippedSkills[0] : null;

            if (skillA == null || skillB == null)
            {
                Debug.LogError("스킬 데이터 없음.");
                yield break;
            }

            int coinsA = skillA.Coins.Count;
            int coinsB = skillB.Coins.Count;
            int clashCount = 0;

            while (coinsA > 0 && coinsB > 0 && clashCount < 20)
            {
                clashCount++;
                yield return new WaitForSeconds(0.5f);

                int offenseDiffA = charA.OffenseLevel - charB.OffenseLevel;
                int offenseDiffB = charB.OffenseLevel - charA.OffenseLevel;

                int powerA = ClashResolver.CalculateSkillPower(skillA, coinsA, charA.CurrentSanity, offenseDiffA, charA.BonusCoinProbability);
                int powerB = ClashResolver.CalculateSkillPower(skillB, coinsB, charB.CurrentSanity, offenseDiffB, charB.BonusCoinProbability);

                Debug.Log($"{clashCount}번째 합 실행, {charA.name} - 위력: {powerA}, 남은 코인: {coinsA} vs {charB.name} - 위력: {powerB}, 남은 코인: {coinsB}");

                if (powerA > powerB)
                {
                    coinsB--;
                    Debug.Log($"-> {charA.name} 합 승리. ({charB.name} 코인 파괴, 남은 코인: {coinsB})");
                }
                else if (powerB > powerA)
                {
                    coinsA--;
                    Debug.Log($"-> {charB.name} 합 승리. ({charA.name} 코인 파괴, 남은 코인: {coinsA})");
                }
                else
                {
                    Debug.Log("-> 무승부. 코인 파괴 없이 다시 합 진행.");
                }
            }

            CharacterRuntime winner = coinsA > 0 ? charA : charB;
            CharacterRuntime loser = coinsA > 0 ? charB : charA;
            SkillData winningSkill = coinsA > 0 ? skillA : skillB;
            int remainingCoins = coinsA > 0 ? coinsA : coinsB;

            Debug.Log($"합 종료. {winner.name}의 최종 잔여 코인 사용.");

            // 합 승리 및 패배 시 정신력 증감
            if (winner.CharacterData != null && winner.CharacterData.SanityCondition != null)
            {
                winner.ModifySanity(winner.CharacterData.SanityCondition.ClashWinSpGain);
            }
            if (loser.CharacterData != null && loser.CharacterData.SanityCondition != null)
            {
                loser.ModifySanity(-loser.CharacterData.SanityCondition.ClashLoseSpLoss);
            }

            int totalDamage = 0;
            bool hasBonus = winner.BonusCoinProbability != 0f;

            for (int i = 0; i < remainingCoins; i++)
            {
                int coinResultPower = winningSkill.BasePower;

                bool isHeads = ClashResolver.RollCoin(winner.CurrentSanity, winner.BonusCoinProbability, hasBonus);
                if (isHeads && i < winningSkill.Coins.Count)
                {
                    coinResultPower += winningSkill.Coins[i].CoinPower;
                }

                int singleCoinDamage = DamageCalculator.CalculateDamage(
                    skillResultValue: coinResultPower,
                    attackType: winningSkill.AttackType,
                    sinAttribute: winningSkill.SinAttribute,
                    attacker: winner,
                    defender: loser,
                    isCritical: false,
                    resonanceStack: 0,
                    clashCount: clashCount,
                    staggerState: 0,
                    damageMultiplierSum: 0f,
                    fixedDamageOffset: 0
                );

                totalDamage += singleCoinDamage;
                Debug.Log($"[{winner.name}] {i + 1}번째 코인 타격 (앞면: {isHeads}, 스킬값: {coinResultPower}) -> {singleCoinDamage} 피해 적용.");
            }

            loser.TakeDamage(totalDamage);
            Debug.Log($"{winner.name}의 잔여 코인 사용 완료. {remainingCoins}개로 총 {totalDamage} 피해를 입힘.");
        }
    }
}
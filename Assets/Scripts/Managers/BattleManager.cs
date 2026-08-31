using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class BattleManager : MonoBehaviour
    {
        [Header("매니저 참조")]
        [SerializeField] private TurnManager _turnManager;

        [Header("전투 규칙 설정")]
        [SerializeField] private int _maxClashCount = 99;                  // 최대 합 반복 제한 횟수

        [Header("연출 딜레이 및 타이밍 설정 (초)")]
        [SerializeField] private float _clashStepDuration = 1.0f;          // 합 1회당 힘겨루기 대치 시간
        [SerializeField] private float _clashStepInterval = 0.2f;          // 다음 합 진행 전 인터벌
        [SerializeField] private float _postClashFreezeDuration = 0.5f;    // 최종 합 종료 후 정적(Freeze) 시간
        [SerializeField] private float _attackInterval = 0.8f;             // 일방 공격 코인 타격 간격
        [SerializeField] private float _actionInterval = 0.5f;             // 슬롯 간 다음 액션 진행 전 인터벌
        [SerializeField] private float _turnEndDelay = 0.8f;               // 턴 종료 후 유닛 재배치 전 대기 시간

        [Header("이동 속도 및 배율 설정")]
        [SerializeField] private float _moveSpeed = 10f;                   // 기본 이동 속도
        [SerializeField] private float _attackDashSpeedMultiplier = 1.5f;  // 일방 공격 진입 돌진 속도 배율
        [SerializeField] private float _advanceSpeedMultiplier = 1.2f;     // 승자 파고들기 전진 속도 배율
        [SerializeField] private float _drawRushSpeedMultiplier = 1.5f;    // 무승부 후 재돌진 속도 배율

        [Header("거리 및 위치 오프셋 설정")]
        [SerializeField] private float _clashDistance = 1.2f;              // 합 맞부딪힐 때의 간격
        [SerializeField] private float _drawRecoilDistance = 1.2f;         // 무승부 시 서로 튕겨 나가는 반동 거리
        [SerializeField] private float _knockbackDistance = 1.0f;          // 일반 합 패배 시 밀려나는 거리
        [SerializeField] private float _finalKnockbackMultiplier = 1.8f;   // 최종 합 패배 시 크게 밀려나는 계수
        [SerializeField] private float _oneSidedAttackOffset = 0.8f;       // 일방 공격 시 타깃과의 간격

        [Header("보간 시간 및 연출 세기 (초/강도)")]
        [SerializeField] private float _knockbackDuration = 0.15f;         // 일반 합 넉백 보간 시간
        [SerializeField] private float _finalKnockbackDuration = 0.2f;     // 최종 합 대형 넉백 보간 시간
        [SerializeField] private float _drawRecoilDuration = 0.12f;        // 무승부 반동 보간 시간
        [SerializeField] private float _drawPauseDuration = 0.1f;          // 무승부 반동 후 재돌진 전 멈춤 시간
        [SerializeField] private float _shakeDuration = 0.15f;             // 피격 시 셰이크 지속 시간
        [SerializeField] private float _shakeMagnitude = 0.15f;            // 피격 시 셰이크 강도

        private readonly HashSet<BattleInfo> _actedSlots = new();          // 이번 턴에 이미 행동한 슬롯 체크

        [ContextMenu("집중 전투 실행")]
        public void RunFullTurn()
        {
            if (_turnManager == null)
            {
                Debug.LogError("TurnManager가 연결되지 않았습니다.");
                return;
            }

            RunFullTurnFlowAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTask RunFullTurnFlowAsync(CancellationToken ct)
        {
            _actedSlots.Clear();

            List<BattleInfo> allSlots = new();
            allSlots.AddRange(_turnManager.PlayerSlots);
            allSlots.AddRange(_turnManager.EnemySlots);
            allSlots.RemoveAll(slot => slot.Owner == null || slot.Owner.IsDead);
            allSlots.Sort((a, b) => b.Speed.CompareTo(a.Speed));

            Debug.Log($"턴 시작 (총 {allSlots.Count}개 슬롯)");

            foreach (var currentSlot in allSlots)
            {
                if (ct.IsCancellationRequested) return;

                if (_actedSlots.Contains(currentSlot) || currentSlot.Owner.IsDead) continue;                // 이미 합을 진행했거나 유닛이 사망했으면 스킵

                var targetSlot = currentSlot.TargetSlot;
                if (targetSlot == null || targetSlot.Owner == null || targetSlot.Owner.IsDead)
                {
                    Debug.Log($"[{currentSlot.Owner.name}] 대상 슬롯이 없거나 사망하여 행동을 스킵합니다.");
                    _actedSlots.Add(currentSlot);
                    continue;
                }

                // 합 성립 조건 : 타겟 슬롯이 아직 행동 전이고, 서로를 타겟하고 있거나 합을 지정하려는 아군의 속도가 빠를 경우
                bool isClash = !_actedSlots.Contains(targetSlot) && (targetSlot.TargetSlot == currentSlot || currentSlot.Speed > targetSlot.Speed);

                if (isClash)
                {
                    _actedSlots.Add(currentSlot);
                    _actedSlots.Add(targetSlot);

                    currentSlot.Owner.DeckHandler.ConsumeSkillAtSlot(currentSlot.SelectedSkillIndex);
                    targetSlot.Owner.DeckHandler.ConsumeSkillAtSlot(targetSlot.SelectedSkillIndex);

                    await ProcessClashAsync(currentSlot.Owner, currentSlot.GetSelectedSkill(), targetSlot.Owner, targetSlot.GetSelectedSkill(), ct);
                }
                else
                {
                    Debug.Log($"일방 공격 - {currentSlot.Owner.name}(속도 {currentSlot.Speed}) -> {targetSlot.Owner.name}");
                    _actedSlots.Add(currentSlot);

                    currentSlot.Owner.DeckHandler.ConsumeSkillAtSlot(currentSlot.SelectedSkillIndex);

                    await ProcessOneSidedAttackAsync(currentSlot.Owner, currentSlot.GetSelectedSkill(), targetSlot.Owner, ct);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_actionInterval), cancellationToken: ct);
            }
            Debug.Log($"턴 종료.");
            await UniTask.Delay(TimeSpan.FromSeconds(_turnEndDelay), cancellationToken: ct);
            _turnManager.PrepareNewTurn();
        }

        public async UniTask ProcessClashAsync(CharacterRuntime charA, SkillData skillA, CharacterRuntime charB, SkillData skillB, CancellationToken ct)
        {
            if (skillA == null || skillB == null)
            {
                Debug.LogError($"스킬 누락 발생 : {charA.name} 스킬: {(skillA != null ? skillA.SkillName : "null")}, {charB.name} 스킬: {(skillB != null ? skillB.SkillName : "null")}");
                return;
            }

            Vector3 centerPos = (charA.transform.position + charB.transform.position) * 0.5f;
            bool aIsLeft = charA.transform.position.x <= charB.transform.position.x;

            Vector3 clashTargetA = centerPos + (aIsLeft ? Vector3.left : Vector3.right) * (_clashDistance * 0.5f);
            Vector3 clashTargetB = centerPos + (aIsLeft ? Vector3.right : Vector3.left) * (_clashDistance * 0.5f);

            await MoveToPositionsAsync(charA, clashTargetA, charB, clashTargetB, _moveSpeed, ct);

            int coinsA = skillA.Coins.Count;
            int coinsB = skillB.Coins.Count;
            int clashCount = 0;

            Debug.Log($"합 발생 - {charA.name}({skillA.SkillName}, 코인 {coinsA}개) VS {charB.name}({skillB.SkillName}, 코인 {coinsB}개)");

            while (coinsA > 0 && coinsB > 0 && clashCount < _maxClashCount)
            {
                clashCount++;

                int offenseDiffA = charA.OffenseLevel - charB.OffenseLevel;
                int offenseDiffB = charB.OffenseLevel - charA.OffenseLevel;

                int powerA = ClashResolver.CalculateSkillPower(skillA, coinsA, charA.CurrentSanity, offenseDiffA, charA.BonusCoinProbability);
                int powerB = ClashResolver.CalculateSkillPower(skillB, coinsB, charB.CurrentSanity, offenseDiffB, charB.BonusCoinProbability);

                Debug.Log($"{charA.name} 최종 위력 : {powerA} (코인 {coinsA}개) VS {charB.name} 최종 위력 : {powerB} (코인 {coinsB}개)");

                await UniTask.Delay(TimeSpan.FromSeconds(_clashStepDuration), cancellationToken: ct);

                if (powerA > powerB)
                {
                    coinsB--;
                    Debug.Log($"{charA.name} 합 승리. {charB.name} 코인 파괴 - 남은 코인: {coinsB}개");

                    Vector3 pushDir = aIsLeft ? Vector3.right : Vector3.left;
                    if (coinsB > 0)
                    {
                        await ApplyClashAdvanceAsync(charA, charB, pushDir * _knockbackDistance, _clashDistance, _moveSpeed * _advanceSpeedMultiplier, ct);
                    }
                }
                else if (powerB > powerA)
                {
                    coinsA--;
                    Debug.Log($"{charB.name} 합 승리. {charA.name} 코인 파괴 - 남은 코인: {coinsB}개");

                    Vector3 pushDir = aIsLeft ? Vector3.left : Vector3.right;
                    if (coinsA > 0)
                    {
                        await ApplyClashAdvanceAsync(charB, charA, pushDir * _knockbackDistance, _clashDistance, _moveSpeed * _advanceSpeedMultiplier, ct);
                    }
                }
                else
                {
                    Debug.Log($"무승부. 합 재실행.");
                    await ApplyClashDrawAsync(charA, charB, _drawRecoilDistance, _moveSpeed * _drawRushSpeedMultiplier, ct);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_clashStepInterval), cancellationToken: ct);
            }

            CharacterRuntime winner = coinsA > 0 ? charA : charB;
            CharacterRuntime loser = coinsA > 0 ? charB : charA;
            SkillData winningSkill = coinsA > 0 ? skillA : skillB;
            int remainingCoins = coinsA > 0 ? coinsA : coinsB;

            Debug.Log($"합 최종 승리: {winner.name}. 스킬의 남은 코인 {remainingCoins}개로 {loser.name}에게 일방 공격.");

            Vector3 finalBlowDir = (loser.transform.position.x >= winner.transform.position.x) ? Vector3.right : Vector3.left;
            await ApplyBigKnockbackAsync(loser, finalBlowDir * (_knockbackDistance * _finalKnockbackMultiplier), ct);

            if (winner.CharacterData?.SanityCondition != null)
                winner.ModifySanity(winner.CharacterData.SanityCondition.ClashWinSpGain);
            if (loser.CharacterData?.SanityCondition != null)
                loser.ModifySanity(-loser.CharacterData.SanityCondition.ClashLoseSpLoss);

            await UniTask.Delay(TimeSpan.FromSeconds(_postClashFreezeDuration), cancellationToken: ct);

            await ExecuteAttackCoinsAsync(winner, winningSkill, loser, remainingCoins, clashCount, ct);      // 합 승리 시 남은 코인으로 일방공격
        }

        public async UniTask ProcessOneSidedAttackAsync(CharacterRuntime attacker, SkillData skill, CharacterRuntime defender, CancellationToken ct)    // 일방 공격 매커니즘
        {
            if (skill == null || defender == null || defender.IsDead)
            {
                Debug.LogError($"일방공격 스킬 누락/대상 사망. 공격자: {attacker.name}, 스킬: {(skill != null ? skill.SkillName : "null")}");
                return;
            }

            Vector3 attackApproachDir = attacker.transform.position.x < defender.transform.position.x ? Vector3.left : Vector3.right;
            Vector3 targetPos = defender.transform.position + attackApproachDir * _oneSidedAttackOffset;

            await MoveSingleAsync(attacker, targetPos, _moveSpeed * _attackDashSpeedMultiplier, ct);

            Debug.Log($"{attacker.name} - {skill.SkillName}로 일방 공격.");
            await ExecuteAttackCoinsAsync(attacker, skill, defender, skill.Coins.Count, 0, ct);
        }

        private async UniTask ExecuteAttackCoinsAsync(CharacterRuntime attacker, SkillData skill, CharacterRuntime defender, int coinCount, int clashCount, CancellationToken ct)   // 코인 별 타격 및 데미지 적용 루틴
        {
            int currentRunningPower = skill.BasePower;
            bool hasBonus = attacker.BonusCoinProbability != 0f;
            int totalDamage = 0;

            for (int i = 0; i < coinCount; i++)
            {
                if (defender.IsDead)
                {
                    Debug.Log($"스킬 공격 대상 사망으로 공격 종료.");
                    break;
                }

                bool isHeads = ClashResolver.RollCoin(attacker.CurrentSanity, attacker.BonusCoinProbability, hasBonus);

                if (isHeads && i < skill.Coins.Count)
                {
                    currentRunningPower += skill.Coins[i].CoinPower;
                }

                int singleDamage = DamageCalculator.CalculateDamage(
                    skillResultValue: currentRunningPower,
                    attackType: skill.AttackType,
                    sinAttribute: skill.SinAttribute,
                    attackerStat: attacker.Stat,
                    defenderStat: defender.Stat,
                    defenderResistances: defender.CharacterData != null ? defender.CharacterData.AttackResistances : null,
                    isCritical: false,
                    resonanceStack: 0,
                    clashCount: clashCount,
                    staggerState: 0,
                    damageMultiplierSum: 0f,
                    fixedDamageOffset: 0
                );

                defender.TakeDamage(singleDamage);
                totalDamage += singleDamage;
                string coinStr = isHeads ? "앞면" : "뒷면";
                Debug.Log($"코인 {i + 1}타 적중. {coinStr} 현재 위력 : {currentRunningPower} | 피해량 : {singleDamage} | {defender.name} 잔여 HP : {defender.CurrentHp}/{defender.MaxHp}");
                ShakeAsync(defender.transform, _shakeDuration, _shakeMagnitude, ct).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(_attackInterval), cancellationToken: ct);
            }
            Debug.Log($"공격 종료. (누적 피해: {totalDamage})");
        }


        private async UniTask MoveToPositionsAsync(CharacterRuntime a, Vector3 targetA, CharacterRuntime b, Vector3 targetB, float speed, CancellationToken ct)    // 위치 보간 및 넉백
        {
            while (Vector3.Distance(a.transform.position, targetA) > 0.05f || Vector3.Distance(b.transform.position, targetB) > 0.05f)
            {
                a.transform.position = Vector3.MoveTowards(a.transform.position, targetA, speed * Time.deltaTime);
                b.transform.position = Vector3.MoveTowards(b.transform.position, targetB, speed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            a.transform.position = targetA;
            b.transform.position = targetB;
        }

        private async UniTask MoveSingleAsync(CharacterRuntime target, Vector3 dest, float speed, CancellationToken ct)
        {
            while (Vector3.Distance(target.transform.position, dest) > 0.05f)
            {
                target.transform.position = Vector3.MoveTowards(target.transform.position, dest, speed * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            target.transform.position = dest;
        }

        private async UniTask ApplyClashAdvanceAsync(CharacterRuntime winner, CharacterRuntime loser, Vector3 pushVector, float clashDistance, float speed, CancellationToken ct)
        {
            Vector3 pushTarget = loser.transform.position + pushVector;
            float t = 0f;

            while (t < _knockbackDuration)
            {
                t += Time.deltaTime;
                loser.transform.position = Vector3.Lerp(loser.transform.position, pushTarget, t / _knockbackDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            loser.transform.position = pushTarget;

            Vector3 winnerAdvanceDir = (loser.transform.position - winner.transform.position).normalized;
            Vector3 winnerTarget = loser.transform.position - winnerAdvanceDir * clashDistance;
            await MoveSingleAsync(winner, winnerTarget, speed, ct);
        }

        private async UniTask ApplyBigKnockbackAsync(CharacterRuntime loser, Vector3 pushVector, CancellationToken ct)
        {
            Vector3 pushTarget = loser.transform.position + pushVector;
            float t = 0f;
            while (t < _finalKnockbackDuration)
            {
                t += Time.deltaTime;
                loser.transform.position = Vector3.Lerp(loser.transform.position, pushTarget, t / _finalKnockbackDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            loser.transform.position = pushTarget;
        }

        private async UniTask ApplyClashDrawAsync(CharacterRuntime a, CharacterRuntime b, float recoilDist, float speed, CancellationToken ct)
        {
            Vector3 pushA = a.transform.position.x <= b.transform.position.x ? Vector3.left : Vector3.right;
            Vector3 pushB = -pushA;

            Vector3 prevA = a.transform.position;
            Vector3 prevB = b.transform.position;

            Vector3 recoilTargetA = prevA + pushA * recoilDist;
            Vector3 recoilTargetB = prevB + pushB * recoilDist;

            float t = 0f;
            while (t < _drawRecoilDuration)
            {
                t += Time.deltaTime;
                a.transform.position = Vector3.Lerp(prevA, recoilTargetA, t / _drawRecoilDuration);
                b.transform.position = Vector3.Lerp(prevB, recoilTargetB, t / _drawRecoilDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            a.transform.position = recoilTargetA;
            b.transform.position = recoilTargetB;

            await UniTask.Delay(TimeSpan.FromSeconds(_drawPauseDuration), cancellationToken: ct);

            await MoveToPositionsAsync(a, prevA, b, prevB, speed, ct);
        }

        private async UniTask ShakeAsync(Transform target, float duration, float magnitude, CancellationToken ct)
        {
            float elapsed = 0.0f;
            Vector3 lastOffset = Vector3.zero;

            while (elapsed < duration)
            {
                target.position -= lastOffset;

                Vector3 newOffset = new(
                    UnityEngine.Random.Range(-1f, 1f) * magnitude,
                    UnityEngine.Random.Range(-1f, 1f) * magnitude,
                    0f
                );

                target.position += newOffset;
                lastOffset = newOffset;

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }

            target.position -= lastOffset;
        }
    }
}
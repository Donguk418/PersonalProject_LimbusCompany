using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class BattleManager : MonoBehaviour
    {
        [Header("전투 편성 캐릭터")]
        [SerializeField] private CharacterRuntime _playerCharacter;
        [SerializeField] private CharacterRuntime _enemyCharacter;

        [Header("전투 규칙 설정")]
        [SerializeField] private int _maxClashCount = 99;                 // 최대 합 반복 제한 횟수

        [Header("연출 딜레이 및 타이밍 설정 (초)")]
        [SerializeField] private float _clashStepDuration = 1.0f;         // 합 1회당 힘겨루기 대치 시간
        [SerializeField] private float _clashStepInterval = 0.2f;         // 다음 합 진행 전 인터벌
        [SerializeField] private float _postClashFreezeDuration = 0.5f;   // 최종 합 종료 후 정적(Freeze) 시간
        [SerializeField] private float _attackInterval = 0.8f;            // 일방 공격 코인 타격 간격
        [SerializeField] private float _turnEndDelay = 0.8f;              // 턴 종료 후 유닛 복귀 전 대기 시간

        [Header("이동 속도 및 배율 설정")]
        [SerializeField] private float _moveSpeed = 10f;                  // 기본 이동 속도
        [SerializeField] private float _attackDashSpeedMultiplier = 1.5f; // 일방 공격 진입 돌진 속도 배율
        [SerializeField] private float _advanceSpeedMultiplier = 1.2f;    // 승자 파고들기 전진 속도 배율
        [SerializeField] private float _drawRushSpeedMultiplier = 1.5f;   // 무승부 후 재돌진 속도 배율

        [Header("거리 및 위치 오프셋 설정")]
        [SerializeField] private float _clashDistance = 1.2f;             // 합 맞부딪힐 때의 간격
        [SerializeField] private float _drawRecoilDistance = 1.2f;        // 무승부 시 서로 튕겨 나가는 반동 거리
        [SerializeField] private float _knockbackDistance = 1.0f;         // 일반 합 패배 시 밀려나는 거리
        [SerializeField] private float _finalKnockbackMultiplier = 1.8f;  // 최종 합 패배 시 크게 밀려나는 계수
        [SerializeField] private float _oneSidedAttackOffset = 0.8f;      // 일방 공격 시 타깃과의 간격

        [Header("보간 시간 및 연출 세기 (초/강도)")]
        [SerializeField] private float _knockbackDuration = 0.15f;        // 일반 합 넉백 보간 시간
        [SerializeField] private float _finalKnockbackDuration = 0.2f;    // 최종 합 대형 넉백 보간 시간
        [SerializeField] private float _drawRecoilDuration = 0.12f;       // 무승부 반동 보간 시간
        [SerializeField] private float _drawPauseDuration = 0.1f;         // 무승부 반동 후 재돌진 전 멈춤 시간
        [SerializeField] private float _shakeDuration = 0.15f;            // 피격 시 셰이크 지속 시간
        [SerializeField] private float _shakeMagnitude = 0.15f;           // 피격 시 셰이크 강도

        private Vector3 _playerOriginPos;
        private Vector3 _enemyOriginPos;

        [ContextMenu("단일 합 전투 테스트용")]
        public void TestSingleClashBattle()
        {
            if (_playerCharacter == null || _enemyCharacter == null)
            {
                Debug.LogError("플레이어와 적 캐릭터를 인스펙터에 연결해야 함");
                return;
            }

            RunSingleClashTurnFlowAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTask RunSingleClashTurnFlowAsync(CancellationToken ct)
        {
            _playerOriginPos = _playerCharacter.transform.position;
            _enemyOriginPos = _enemyCharacter.transform.position;

            await ProcessClashAsync(_playerCharacter, _enemyCharacter, ct);

            await UniTask.Delay(TimeSpan.FromSeconds(_turnEndDelay), cancellationToken: ct);

            await EndTurnAndReturnPositionsAsync(ct);
        }

        public async UniTask ProcessClashAsync(CharacterRuntime charA, CharacterRuntime charB, CancellationToken ct)
        {
            Debug.Log("전투 개시.");

            Vector3 centerPos = (charA.transform.position + charB.transform.position) * 0.5f;
            bool aIsLeft = charA.transform.position.x <= charB.transform.position.x;

            Vector3 clashTargetA = centerPos + (aIsLeft ? Vector3.left : Vector3.right) * (_clashDistance * 0.5f);
            Vector3 clashTargetB = centerPos + (aIsLeft ? Vector3.right : Vector3.left) * (_clashDistance * 0.5f);

            await MoveToPositionsAsync(charA, clashTargetA, charB, clashTargetB, _moveSpeed, ct);

            SkillData skillA = charA.EquippedSkills.Count > 0 ? charA.EquippedSkills[0] : null;
            SkillData skillB = charB.EquippedSkills.Count > 0 ? charB.EquippedSkills[0] : null;

            if (skillA == null || skillB == null)
            {
                Debug.LogError("스킬 데이터 없음.");
                return;
            }

            int coinsA = skillA.Coins.Count;
            int coinsB = skillB.Coins.Count;
            int clashCount = 0;

            while (coinsA > 0 && coinsB > 0 && clashCount < _maxClashCount)
            {
                clashCount++;

                int offenseDiffA = charA.OffenseLevel - charB.OffenseLevel;
                int offenseDiffB = charB.OffenseLevel - charA.OffenseLevel;

                int powerA = ClashResolver.CalculateSkillPower(skillA, coinsA, charA.CurrentSanity, offenseDiffA, charA.BonusCoinProbability);
                int powerB = ClashResolver.CalculateSkillPower(skillB, coinsB, charB.CurrentSanity, offenseDiffB, charB.BonusCoinProbability);

                Debug.Log($"{clashCount}번째 합 - {charA.name}(위력: {powerA}, 코인: {coinsA}) vs {charB.name}(위력: {powerB}, 코인: {coinsB})");

                await UniTask.Delay(TimeSpan.FromSeconds(_clashStepDuration), cancellationToken: ct);

                if (powerA > powerB)
                {
                    coinsB--;
                    Debug.Log($"{charA.name} 승리. {charB.name} 코인 파괴.");
                    Vector3 pushDir = aIsLeft ? Vector3.right : Vector3.left;

                    if (coinsB > 0)
                    {
                        await ApplyClashAdvanceAsync(charA, charB, pushDir * _knockbackDistance, _clashDistance, _moveSpeed * _advanceSpeedMultiplier, ct);
                    }
                }
                else if (powerB > powerA)
                {
                    coinsA--;
                    Debug.Log($"{charB.name} 승리. {charA.name} 코인 파괴.");
                    Vector3 pushDir = aIsLeft ? Vector3.left : Vector3.right;

                    if (coinsA > 0)
                    {
                        await ApplyClashAdvanceAsync(charB, charA, pushDir * _knockbackDistance, _clashDistance, _moveSpeed * _advanceSpeedMultiplier, ct);
                    }
                }
                else
                {
                    Debug.Log("무승부, 합 재개.");
                    await ApplyClashDrawAsync(charA, charB, _drawRecoilDistance, _moveSpeed * _drawRushSpeedMultiplier, ct);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_clashStepInterval), cancellationToken: ct);
            }

            if (coinsA > 0 && coinsB > 0)
            {
                Debug.Log($"최대 합 횟수({_maxClashCount}회) 초과로 무승부.");
                return;
            }

            CharacterRuntime winner = coinsA > 0 ? charA : charB;
            CharacterRuntime loser = coinsA > 0 ? charB : charA;
            SkillData winningSkill = coinsA > 0 ? skillA : skillB;
            int remainingCoins = coinsA > 0 ? coinsA : coinsB;

            Vector3 finalBlowDir = (loser.transform.position.x >= winner.transform.position.x) ? Vector3.right : Vector3.left;
            await ApplyBigKnockbackAsync(loser, finalBlowDir * (_knockbackDistance * _finalKnockbackMultiplier), ct);

            if (winner.CharacterData != null && winner.CharacterData.SanityCondition != null)
                winner.ModifySanity(winner.CharacterData.SanityCondition.ClashWinSpGain);
            if (loser.CharacterData != null && loser.CharacterData.SanityCondition != null)
                loser.ModifySanity(-loser.CharacterData.SanityCondition.ClashLoseSpLoss);

            await UniTask.Delay(TimeSpan.FromSeconds(_postClashFreezeDuration), cancellationToken: ct);

            Debug.Log($"합 종료. {winner.name}의 잔여 코인 {remainingCoins}개로 일방 공격.");

            Vector3 attackApproachDir = winner.transform.position.x < loser.transform.position.x ? Vector3.left : Vector3.right;
            Vector3 finalAttackPos = loser.transform.position + attackApproachDir * _oneSidedAttackOffset;
            await MoveSingleAsync(winner, finalAttackPos, _moveSpeed * _attackDashSpeedMultiplier, ct);

            int currentRunningPower = winningSkill.BasePower;
            bool hasBonus = winner.BonusCoinProbability != 0f;

            for (int i = 0; i < remainingCoins; i++)
            {
                bool isHeads = ClashResolver.RollCoin(winner.CurrentSanity, winner.BonusCoinProbability, hasBonus);

                if (isHeads && i < winningSkill.Coins.Count)
                {
                    currentRunningPower += winningSkill.Coins[i].CoinPower;
                }

                int singleDamage = DamageCalculator.CalculateDamage(
                    skillResultValue: currentRunningPower,
                    attackType: winningSkill.AttackType,
                    sinAttribute: winningSkill.SinAttribute,
                    attackerStat: winner.Stat,
                    defenderStat: loser.Stat,
                    defenderResistances: loser.CharacterData != null ? loser.CharacterData.AttackResistances : null,
                    isCritical: false,
                    resonanceStack: 0,
                    clashCount: clashCount,
                    staggerState: 0,
                    damageMultiplierSum: 0f,
                    fixedDamageOffset: 0
                );

                loser.TakeDamage(singleDamage);
                Debug.Log($"{winner.name} - {i + 1}타격 (앞면: {isHeads}, 스킬값: {currentRunningPower}) -> {singleDamage} 피해 적용 (남은 HP: {loser.CurrentHp})");

                ShakeAsync(loser.transform, _shakeDuration, _shakeMagnitude, ct).Forget();

                if (loser.CurrentHp <= 0)
                {
                    Debug.Log($"{loser.name} 사망, 공격 종료.");
                    break;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_attackInterval), cancellationToken: ct);
            }

            Debug.Log("공격 종료.");
        }

        public async UniTask EndTurnAndReturnPositionsAsync(CancellationToken ct)
        {
            Debug.Log("턴 종료, 살아있는 유닛 재배치.");

            if (_playerCharacter.CurrentHp > 0 && _enemyCharacter.CurrentHp > 0)
            {
                await MoveToPositionsAsync(_playerCharacter, _playerOriginPos, _enemyCharacter, _enemyOriginPos, _moveSpeed, ct);
            }
            else if (_playerCharacter.CurrentHp > 0)
            {
                await MoveSingleAsync(_playerCharacter, _playerOriginPos, _moveSpeed, ct);
            }
            else if (_enemyCharacter.CurrentHp > 0)
            {
                await MoveSingleAsync(_enemyCharacter, _enemyOriginPos, _moveSpeed, ct);
            }
        }

        private async UniTask MoveToPositionsAsync(CharacterRuntime a, Vector3 targetA, CharacterRuntime b, Vector3 targetB, float speed, CancellationToken ct)
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
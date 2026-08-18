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

        [Header("연출 딜레이 및 타이밍 설정 (초)")]
        [SerializeField] private float _clashStepDuration = 2.0f;    // 합 1회당 연출 시간
        [SerializeField] private float _attackInterval = 1.0f;       // 일방 공격 타격 간격
        [SerializeField] private float _moveSpeed = 10f;             // 돌진/복귀 이동 속도

        [Header("합 거리 설정")]
        [SerializeField] private float _clashDistance = 1.5f;        // 합 맞부딪힐 때의 간격
        [SerializeField] private float _knockbackDistance = 1.0f;    // 합 패배 시 밀려나는 거리

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

            // GameObject 수명 주기에 바인딩된 CancellationToken 전달 (객체 파괴 시 비동기 안전 종료)
            ProcessClashAsync(_playerCharacter, _enemyCharacter, this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTask ProcessClashAsync(CharacterRuntime charA, CharacterRuntime charB, CancellationToken ct)
        {
            Debug.Log("== 전투 개시: 합 돌진 (UniTask) ==");

            // 1. 초기 위치 캐싱
            _playerOriginPos = charA.transform.position;
            _enemyOriginPos = charB.transform.position;

            Vector3 centerPos = (_playerOriginPos + _enemyOriginPos) * 0.5f;
            Vector3 clashTargetA = centerPos + Vector3.left * (_clashDistance * 0.5f);
            Vector3 clashTargetB = centerPos + Vector3.right * (_clashDistance * 0.5f);

            // 2. 중앙 격돌 지점으로 동시 돌진 이동
            await MoveToPositionsAsync(charA, clashTargetA, charB, clashTargetB, _moveSpeed, ct);

            SkillData skillA = charA.EquippedSkills.Count > 0 ? charA.EquippedSkills[0] : null;
            SkillData skillB = charB.EquippedSkills.Count > 0 ? charB.EquippedSkills[0] : null;

            if (skillA == null || skillB == null)
            {
                Debug.LogError("스킬 데이터 없음.");
                await MoveToPositionsAsync(charA, _playerOriginPos, charB, _enemyOriginPos, _moveSpeed, ct);
                return;
            }

            int coinsA = skillA.Coins.Count;
            int coinsB = skillB.Coins.Count;
            int clashCount = 0;

            // 3. 합(Clash) 공방 루프
            while (coinsA > 0 && coinsB > 0 && clashCount < 20)
            {
                clashCount++;

                int offenseDiffA = charA.OffenseLevel - charB.OffenseLevel;
                int offenseDiffB = charB.OffenseLevel - charA.OffenseLevel;

                int powerA = ClashResolver.CalculateSkillPower(skillA, coinsA, charA.CurrentSanity, offenseDiffA, charA.BonusCoinProbability);
                int powerB = ClashResolver.CalculateSkillPower(skillB, coinsB, charB.CurrentSanity, offenseDiffB, charB.BonusCoinProbability);

                Debug.Log($"[{clashCount}번째 합] {charA.name}(위력: {powerA}, 코인: {coinsA}) vs {charB.name}(위력: {powerB}, 코인: {coinsB})");

                if (powerA > powerB)
                {
                    coinsB--;
                    Debug.Log($"-> {charA.name} 승리! ({charB.name} 코인 파괴 및 넉백)");
                    await ApplyKnockbackAsync(charB, Vector3.right * _knockbackDistance, clashTargetB, ct);
                }
                else if (powerB > powerA)
                {
                    coinsA--;
                    Debug.Log($"-> {charB.name} 승리! ({charA.name} 코인 파괴 및 넉백)");
                    await ApplyKnockbackAsync(charA, Vector3.left * _knockbackDistance, clashTargetA, ct);
                }
                else
                {
                    Debug.Log("-> 무승부! 양측 반동");
                    await ApplyClashDrawAsync(charA, clashTargetA, charB, clashTargetB, ct);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_clashStepDuration * 0.3f), cancellationToken: ct);
            }

            CharacterRuntime winner = coinsA > 0 ? charA : charB;
            CharacterRuntime loser = coinsA > 0 ? charB : charA;
            SkillData winningSkill = coinsA > 0 ? skillA : skillB;
            int remainingCoins = coinsA > 0 ? coinsA : coinsB;

            // 4. 합 결과 정신력 증감
            if (winner.CharacterData != null && winner.CharacterData.SanityCondition != null)
                winner.ModifySanity(winner.CharacterData.SanityCondition.ClashWinSpGain);
            if (loser.CharacterData != null && loser.CharacterData.SanityCondition != null)
                loser.ModifySanity(-loser.CharacterData.SanityCondition.ClashLoseSpLoss);

            Debug.Log($"합 종료. 승자: {winner.name}, 잔여 코인 {remainingCoins}개 일방 타격 시작!");

            // 5. 승자가 패자 코앞까지 파고들어 일방 타격 연출
            Vector3 finalAttackPos = loser.transform.position + (winner == charA ? Vector3.left : Vector3.right) * 0.8f;
            await MoveSingleAsync(winner, finalAttackPos, _moveSpeed * 1.5f, ct);

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

                int singleDamage = DamageCalculator.CalculateDamage(
                    coinResultPower, winningSkill.AttackType, winningSkill.SinAttribute,
                    winner, loser, false, 0, clashCount, 0, 0f, 0
                );

                totalDamage += singleDamage;
                Debug.Log($"[{winner.name}] {i + 1}타격 (앞면: {isHeads}) -> {singleDamage} 피해!");

                // 피격자 흔들림 (비동기 병렬 실행)
                ShakeAsync(loser.transform, 0.15f, 0.2f, ct).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(_attackInterval), cancellationToken: ct);
            }

            loser.TakeDamage(totalDamage);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);

            // 6. 양측 캐릭터 원래 자리로 복귀
            Debug.Log("전투 종료: 원래 위치로 복귀.");
            await MoveToPositionsAsync(charA, _playerOriginPos, charB, _enemyOriginPos, _moveSpeed, ct);
        }

        // --- UniTask 기반 비동기 보간 연출 메서드 모음 ---

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

        private async UniTask ApplyKnockbackAsync(CharacterRuntime loser, Vector3 pushVector, Vector3 returnTarget, CancellationToken ct)
        {
            Vector3 pushTarget = loser.transform.position + pushVector;
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                loser.transform.position = Vector3.Lerp(loser.transform.position, pushTarget, t / 0.15f);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

            while (Vector3.Distance(loser.transform.position, returnTarget) > 0.05f)
            {
                loser.transform.position = Vector3.MoveTowards(loser.transform.position, returnTarget, _moveSpeed * 0.6f * Time.deltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            loser.transform.position = returnTarget;
        }

        private async UniTask ApplyClashDrawAsync(CharacterRuntime a, Vector3 targetA, CharacterRuntime b, Vector3 targetB, CancellationToken ct)
        {
            a.transform.position += Vector3.left * 0.4f;
            b.transform.position += Vector3.right * 0.4f;
            await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: ct);
            await MoveToPositionsAsync(a, targetA, b, targetB, _moveSpeed, ct);
        }

        private async UniTask ShakeAsync(Transform target, float duration, float magnitude, CancellationToken ct)
        {
            Vector3 originalPos = target.position;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                target.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }

            target.position = originalPos;
        }
    }
}
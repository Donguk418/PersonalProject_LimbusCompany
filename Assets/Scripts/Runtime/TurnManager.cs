using System;
using System.Collections.Generic;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class TurnManager : MonoBehaviour
    {
        [Header("적 및 아군 유닛 목록")]
        [SerializeField] private List<CharacterRuntime> _playerUnits = new();
        [SerializeField] private List<CharacterRuntime> _enemyUnits = new();

        [Header("적 및 아군 스킬 슬롯 목록")]
        [SerializeField] private List<BattleInfo> _playerSlots = new();
        [SerializeField] private List<BattleInfo> _enemySlots = new();

        [Header("진형 배치 기준점(속도가 가장 빠른 유닛) 및 간격")]
        [SerializeField] private Vector3 _playerStartPos = new(-13f, 0f, 0f);                     // 속도가 가장 빠른 아군 위치 기준점
        [SerializeField] private Vector3 _enemyStartPos = new(13f, 0f, 0f);                       // 속도가 가장 빠른 적 위치 기준점
        [SerializeField] private float _xSpacing = 1.6f;
        [SerializeField] private float _yOffset = 1.5f;

        public IReadOnlyList<BattleInfo> PlayerSlots => _playerSlots;
        public IReadOnlyList<BattleInfo> EnemySlots => _enemySlots;

        public event Action OnTurnPrepared;                                                      // 유닛 정렬 및 슬롯 배치가 끝났을 때 호출되는 이벤트

        public void SetupBattle(List<CharacterRuntime> players, List<CharacterRuntime> enemies)
        {
            _playerUnits = players ?? new List<CharacterRuntime>();
            _enemyUnits = enemies ?? new List<CharacterRuntime>();

            _playerSlots.Clear();
            _enemySlots.Clear();

            PrepareNewTurn();
        }

        [ContextMenu("턴 시작, 속도 주사위 롤 및 속도 순으로 유닛 위치 정렬")]
        public void PrepareNewTurn()
        {
            _playerSlots.RemoveAll(slot => slot.Owner == null || slot.Owner.IsDead);           // 사망한 아군 유닛 슬롯 제거
            _enemySlots.RemoveAll(slot => slot.Owner == null || slot.Owner.IsDead);            // 사망한 적 유닛 슬롯 제거

            _playerSlots.Clear();
            _enemySlots.Clear();

            foreach (var unit in _playerUnits)
            {
                unit.DeckHandler.OnTurnStarted();

                for (int i = 0; i < unit.BaseSlotCount; i++)
                {
                    var slot = new BattleInfo(unit);
                    slot.RollSpeed();
                    _playerSlots.Add(slot);
                }
            }

            foreach (var unit in _enemyUnits)
            {
                unit.DeckHandler.OnTurnStarted();

                for (int i = 0; i < unit.BaseSlotCount; i++)
                {
                    var slot = new BattleInfo(unit);
                    slot.RollSpeed();                                                             // 스킬 슬롯을 여러 개 가진 적일 경우, 스킬 슬롯마다 독립된 속도값 부여
                    _enemySlots.Add(slot);
                }
            }

            _playerSlots.Sort((a, b) => b.Speed.CompareTo(a.Speed));                              // 속도가 빠른 순으로 좌->우 내림차순 정렬
            _enemySlots.Sort((a, b) => b.Speed.CompareTo(a.Speed));                               // 속도가 빠른 순으로 우->좌 내림차순 정렬

            ApplyFormation();

 
            foreach (var enemySlot in _enemySlots)                                                // 턴 시작 시, 슬롯별 스킬 타게팅 조건에 맞춰 아군 슬롯 선 타게팅
            {
                SkillData skill = enemySlot.GetSelectedSkill();
                TargetingPriority priority = skill != null ? skill.TargetingPriority : TargetingPriority.Random;
                enemySlot.TargetSlot = EnemyTargetingSolver.PickTarget(enemySlot, _playerSlots, priority);
            }

            foreach (var playerSlot in _playerSlots)                                               // 테스트용 아군 스킬 적에게 랜덤 타게팅
            {
                playerSlot.TargetSlot = EnemyTargetingSolver.PickTarget(playerSlot, _enemySlots, TargetingPriority.Random);
            }

            DebugLogTurnOrder();

            OnTurnPrepared?.Invoke();
        }

        private void ApplyFormation()
        {
            for (int i = 0; i < _playerSlots.Count; i++)                                        // 아군 배치
            {
                var unit = _playerUnits[i];
                float x = _playerStartPos.x + (i * _xSpacing);
                float y = (i % 2 == 0) ? _playerStartPos.y : _playerStartPos.y - _yOffset;      // 짝수 인덱스(속도 순서 1,3,5,7번째)는 상단, 홀수 인덱스(속도 순서 2,4,6번째)는 하단
                float z = _playerStartPos.z + (y * 0.01f);

                unit.transform.position = new(x, y, z);
            }

            for (int i = 0; i < _enemySlots.Count; i++)                                         // 적 배치
            {
                var unit = _enemyUnits[i];
                float x = _enemyStartPos.x - (i * _xSpacing);
                float y = (i % 2 == 0) ? _enemyStartPos.y : _enemyStartPos.y - _yOffset;        // 짝수 인덱스(속도 순서 1,3,5,7번째)는 상단, 홀수 인덱스(속도 순서 2,4,6번째)는 하단
                float z = _enemyStartPos.z + (y * 0.01f);

                unit.transform.position = new(x, y, z);
            }
        }

        private void DebugLogTurnOrder()
        {
            Debug.Log("아군 슬롯 및 위치 속도 순으로 배치.");
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                Debug.Log($"아군 {i}번 슬롯 (좌측 {i + 1}번째) {slot.Owner.name} - 속도: {slot.Speed}");
            }

            Debug.Log("적 슬롯 및 위치 속도 순으로 배치.");
            for (int i = _enemySlots.Count - 1; i >= 0; i--)
            {
                var slot = _enemySlots[i];
                int fromRight = _enemySlots.Count - i;
                string targetName = slot.TargetSlot != null ? $"{slot.TargetSlot.Owner.name}(속도 {slot.TargetSlot.Speed})" : "없음";
                string skillName = slot.GetSelectedSkill() != null ? slot.GetSelectedSkill().SkillName : "스킬 없음";
                TargetingPriority priority = slot.GetSelectedSkill() != null ? slot.GetSelectedSkill().TargetingPriority : TargetingPriority.Random;
                Debug.Log($"적 {i}번 슬롯 (우측 {fromRight}번째) {slot.Owner.name} - 속도: {slot.Speed}");
            }
        }

        private void OnDrawGizmosSelected()                                                     // 씬 뷰에서 배치 미리보기
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 7; i++)
            {
                float x = _playerStartPos.x + (i * _xSpacing);
                float y = (i % 2 == 0) ? _playerStartPos.y : _playerStartPos.y - _yOffset;
                Gizmos.DrawWireSphere(new Vector3(x, y, _playerStartPos.z), 0.3f);
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < 7; i++)
            {
                float x = _enemyStartPos.x - (i * _xSpacing);
                float y = (i % 2 == 0) ? _enemyStartPos.y : _enemyStartPos.y - _yOffset;
                Gizmos.DrawWireSphere(new Vector3(x, y, _enemyStartPos.z), 0.3f);
            }
        }
    }
}
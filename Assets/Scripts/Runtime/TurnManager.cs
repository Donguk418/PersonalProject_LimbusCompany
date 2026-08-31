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

        [Header("적 및 아군 스킬 슬롯 목록 (BattleInfo)")]
        [SerializeField] private List<BattleInfo> _playerSlots = new();
        [SerializeField] private List<BattleInfo> _enemySlots = new();

        [Header("진형 배치 기준점(속도가 가장 빠른 유닛) 및 간격")]
        [SerializeField] private Vector3 _playerStartPos = new(-13f, 0f, 0f);                            // 속도가 가장 빠른 아군 위치 기준점
        [SerializeField] private Vector3 _enemyStartPos = new(13f, 0f, 0f);                              // 속도가 가장 빠른 적 위치 기준점
        [SerializeField] private float _xSpacing = 1.6f;
        [SerializeField] private float _yOffset = 1.5f;

        public IReadOnlyList<BattleInfo> PlayerSlots => _playerSlots;
        public IReadOnlyList<BattleInfo> EnemySlots => _enemySlots;

        private void Start()
        {
            PrepareNewTurn();
        }

        [ContextMenu("턴 시작, 속도 주사위 롤 및 속도 순으로 유닛 위치 정렬")]
        public void PrepareNewTurn()
        {
            _playerSlots.Clear();
            _enemySlots.Clear();

            _playerUnits.RemoveAll(unit => unit == null || unit.IsDead);                                   // 아군, 적 사망한 유닛 체크 및 슬롯 제거
            _enemyUnits.RemoveAll(unit => unit == null || unit.IsDead);

            foreach (var unit in _playerUnits)
            {
                if (unit.DeckHandler == null || unit.EquippedSkills.Count == 0)
                {
                    unit.InitializeCharacter();
                }
            }

            foreach (var unit in _enemyUnits)
            {
                if (unit.DeckHandler == null || unit.EquippedSkills.Count == 0)
                {
                    unit.InitializeCharacter();
                }
            }

            GenerateSlots(_playerUnits, _playerSlots);
            GenerateSlots(_enemyUnits, _enemySlots);

            _playerSlots.Sort((a, b) => b.Speed.CompareTo(a.Speed));                        // 속도 순으로 슬롯 내림차순 정렬
            _enemySlots.Sort((a, b) => b.Speed.CompareTo(a.Speed));

            ApplyFormation(_playerSlots, _playerStartPos, true);                            // 속도 순으로 유닛 위치 내림차순 정렬
            ApplyFormation(_enemySlots, _enemyStartPos, false);

            foreach (var enemySlot in _enemySlots)                                          // 적이 턴 시작과 동시에 아군 슬롯을 무작위로 지정
            {
                SkillData skill = enemySlot.GetSelectedSkill();
                TargetingPriority priority = skill != null ? skill.TargetingPriority : TargetingPriority.Random;
                enemySlot.TargetSlot = EnemyTargetingSolver.PickTarget(enemySlot, _playerSlots, priority);
            }

            foreach (var playerSlot in _playerSlots)                                                               // 테스트용 : 아군 슬롯이 무작위 적 스킬을 지정
            {
                playerSlot.TargetSlot = EnemyTargetingSolver.PickTarget(playerSlot, _enemySlots, TargetingPriority.Random);
            }

            Debug.Log($"아군 슬롯 {_playerSlots.Count}개, 적 슬롯 {_enemySlots.Count}개 생성 및 타겟 지정 완료");
        }

        private void GenerateSlots(List<CharacterRuntime> units, List<BattleInfo> slotList)
        {
            foreach (var unit in units)
            {
                int slotCount = unit.BaseSlotCount;
                for (int i = 0; i < slotCount; i++)
                {
                    var battleInfo = new BattleInfo(unit);                               // 슬롯이 여러 개일 경우, 각 슬롯이 독립된 속도값을 가짐
                    battleInfo.RollSpeed();
                    slotList.Add(battleInfo);
                }
            }
        }

        private void ApplyFormation(List<BattleInfo> slotList, Vector3 startPos, bool isPlayer)
        {
            HashSet<CharacterRuntime> placedUnits = new();
            int placeIndex = 0;

            foreach (var slot in slotList)
            {
                var unit = slot.Owner;
                if (unit == null || placedUnits.Contains(unit)) continue;

                placedUnits.Add(unit);

                float xPos = isPlayer
                    ? startPos.x + (placeIndex * _xSpacing)
                    : startPos.x - (placeIndex * _xSpacing);

                float yPos = (placeIndex % 2 == 0)
                    ? startPos.y
                    : startPos.y - _yOffset;

                unit.transform.position = new Vector3(xPos, yPos, startPos.z);
                placeIndex++;
            }
        }
    }
}
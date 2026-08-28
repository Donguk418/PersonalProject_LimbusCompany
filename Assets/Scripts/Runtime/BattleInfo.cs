using System;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    [Serializable]
    public class BattleInfo
    {
        [SerializeField] private CharacterRuntime _owner;
        [SerializeField] private int _speed;
        [SerializeField] private int _selectedSkillIndex = 0;             // 0: 상단 스킬, 1: 하단 스킬
        [SerializeField] private BattleInfo _targetSlot;                  // 내가 지정한 적 슬롯

        public CharacterRuntime Owner => _owner;
        public int Speed => _speed;
        public int SelectedSkillIndex => _selectedSkillIndex;
        public BattleInfo TargetSlot { get => _targetSlot; set => _targetSlot = value; }

        public BattleInfo(CharacterRuntime owner)
        {
            _owner = owner;
            _speed = 0;
            _selectedSkillIndex = 0;
            _targetSlot = null;
        }

        public void RollSpeed()                                             // 턴 시작 시 속도 주사위 굴리기
        {
            if (_owner == null || _owner.CharacterData == null)
            {
                _speed = 1;
                return;
            }

            int min = _owner.CharacterData.MinSpeed;
            int max = _owner.CharacterData.MaxSpeed;
            _speed = UnityEngine.Random.Range(min, max + 1);
        }

        public void SelectSkill(int slotIndex)
        {
            _selectedSkillIndex = slotIndex;
        }

        public SkillData GetSelectedSkill()                                  // 스킬 슬롯에서 선택된 스킬 반환
        {
            if (_owner == null || _owner.DeckHandler == null) return null;
            var skills = _owner.DeckHandler.SlottedSkills;
            if (_selectedSkillIndex >= 0 && _selectedSkillIndex < skills.Count)
            {
                return skills[_selectedSkillIndex];
            }
            return null;
        }
    }
}
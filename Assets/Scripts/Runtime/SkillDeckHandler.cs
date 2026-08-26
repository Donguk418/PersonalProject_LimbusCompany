using System;
using System.Collections.Generic;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    [Serializable]
    public class SkillDeckHandler
    {
        [Header("인격 원본 스킬 참조")]
        [SerializeField] private SkillData _skill1;
        [SerializeField] private SkillData _skill2;
        [SerializeField] private SkillData _skill3;
        [SerializeField] private SkillData _defenseSkill;

        [Header("덱 및 슬롯 상태")]
        [SerializeField] private List<SkillData> _drawPile = new();               // 아직 사용하지 않은 스킬
        [SerializeField] private List<SkillData> _discardPile = new();            // 이미 사용한 스킬
        [SerializeField] private List<SkillData> _slottedSkills = new();          // 현재 장착 중인 스킬 (0: 상단, 1: 하단)

        [Header("하단 슬롯 오버라이드 상태")]
        [SerializeField] private SkillData _bottomSlotBaseSkill;                  // 턴 시작 시점에 하단 슬롯에 장착 중인 스킬
        [SerializeField] private SkillData _pendingSpecialSkill;                  // 이번 턴에 임시 장착한 스킬
        [SerializeField] private bool _isBottomSlotOverridden;                    // 현재 임시로 수비/EGO 스킬이 장착돼있는지 여부
        [SerializeField] private bool _isBaseSkillPermanentEgo;                   // 하단 슬롯 스킬 자체가 E.G.O로 완전히 대체되었는지 여부

        public event Action<int, SkillData> OnSlotChanged;
        public event Action OnDeckRefilled;
        public event Action OnTurnSynced;

        public IReadOnlyList<SkillData> DrawPile => _drawPile;
        public IReadOnlyList<SkillData> DiscardPile => _discardPile;
        public IReadOnlyList<SkillData> SlottedSkills => _slottedSkills;
        public SkillData DefenseSkill => _defenseSkill;
        public SkillData BottomSlotBaseSkill => _bottomSlotBaseSkill;
        public bool IsBottomSlotOverridden => _isBottomSlotOverridden;
        public bool IsBaseSkillPermanentEgo => _isBaseSkillPermanentEgo;

        public SkillData NextDrawPreviewSkill => _drawPile.Count > 0 ? _drawPile[0] : null;        // 다음 턴에 장착될 예정인 스킬

        public void Initialize(CharacterData data, int initialSlotCount = 2)
        {
            if (data == null || data.SkillList == null) return;

            _drawPile.Clear();
            _discardPile.Clear();
            _slottedSkills.Clear();
            _bottomSlotBaseSkill = null;
            _pendingSpecialSkill = null;
            _isBottomSlotOverridden = false;
            _isBaseSkillPermanentEgo = false;

            _skill1 = null;
            _skill2 = null;
            _skill3 = null;
            _defenseSkill = null;

            foreach (var skill in data.SkillList)
            {
                if (skill == null) continue;

                switch (skill.SkillNum)
                {
                    case 1: _skill1 = skill; break;
                    case 2: _skill2 = skill; break;
                    case 3: _skill3 = skill; break;
                    default:
                        _defenseSkill = skill;
                        break;
                }
            }

            RefillAndShuffleDeck();

            for (int i = 0; i < initialSlotCount; i++)
            {
                SkillData drawn = DrawCard();
                _slottedSkills.Add(drawn);
                OnSlotChanged?.Invoke(i, drawn);
            }

            if (_slottedSkills.Count >= 2)
            {
                _bottomSlotBaseSkill = _slottedSkills[1];
            }
        }

        private void RefillAndShuffleDeck()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            if (_skill1 != null) { _drawPile.Add(_skill1); _drawPile.Add(_skill1); _drawPile.Add(_skill1); }
            if (_skill2 != null) { _drawPile.Add(_skill2); _drawPile.Add(_skill2); }
            if (_skill3 != null) { _drawPile.Add(_skill3); }

            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (_drawPile[i], _drawPile[randomIndex]) = (_drawPile[randomIndex], _drawPile[i]);
            }

            OnDeckRefilled?.Invoke();
        }

        public SkillData DrawCard()
        {
            if (_drawPile.Count == 0)
            {
                RefillAndShuffleDeck();
            }

            if (_drawPile.Count == 0) return null;

            SkillData drawnSkill = _drawPile[0];
            _drawPile.RemoveAt(0);
            return drawnSkill;
        }

        public SkillData ConsumeSkillAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slottedSkills.Count) return null;

            SkillData actualUsedSkill = _slottedSkills[slotIndex];

            if (slotIndex == 0)                                                                  // 상단 스킬 사용 시

            {
                _discardPile.Add(actualUsedSkill);                                               // 사용한 스킬을 이미 사용한 스킬 리스트에 추가


                SkillData newCard = DrawCard();
                _slottedSkills[0] = newCard;
                OnSlotChanged?.Invoke(0, newCard);
            }

            else if (slotIndex == 1 && _slottedSkills.Count >= 2)                                // 하단 스킬 사용 시
            {
                if (!_isBaseSkillPermanentEgo && !_isBottomSlotOverridden)                       // 인격 일반 스킬이었을 경우 이미 사용한 스킬 리스트에 추가
                {
                    _discardPile.Add(actualUsedSkill);
                }
                else if (_isBottomSlotOverridden && !_isBaseSkillPermanentEgo)
                {
                    if (_bottomSlotBaseSkill != null)
                        _discardPile.Add(_bottomSlotBaseSkill);
                }

                _isBottomSlotOverridden = false;                                                 // 하단 슬롯 스킬 사용 시 모든 오버라이드 상태 리셋
                _isBaseSkillPermanentEgo = false;
                _pendingSpecialSkill = null;

                _slottedSkills[1] = _slottedSkills[0];                                            // 상단 스킬을 하단으로 내리고
                _bottomSlotBaseSkill = _slottedSkills[1];
                OnSlotChanged?.Invoke(1, _slottedSkills[1]);

                SkillData newCard = DrawCard();                                                   // 상단에 새 스킬 추가
                _slottedSkills[0] = newCard;
                OnSlotChanged?.Invoke(0, newCard);
            }
            else
            {
                _discardPile.Add(actualUsedSkill);
                SkillData newCard = DrawCard();
                _slottedSkills[slotIndex] = newCard;
                OnSlotChanged?.Invoke(slotIndex, newCard);
            }

            return actualUsedSkill;
        }

        public void SwitchBottomSlotToDefense()                                                    // 하단 스킬 수비로 변경 시
        {
            if (_slottedSkills.Count < 2 || _defenseSkill == null) return;

            _isBottomSlotOverridden = true;                                                        // 변경 전 공격 스킬 백업
            _pendingSpecialSkill = _defenseSkill;
            _slottedSkills[1] = _defenseSkill;
            OnSlotChanged?.Invoke(1, _defenseSkill);
        }

        public bool SwitchBottomSlotToEgo(SkillData egoSkill)                                      // 하단 슬롯 E.G.O 스킬로 전환
        {
            if (_slottedSkills.Count < 2 || egoSkill == null) return false;

            if (_isBaseSkillPermanentEgo && _bottomSlotBaseSkill == egoSkill)                      // 하단 슬롯이 E.G.O 스킬로 완전히 대체되었을 경우, 같은 E.G.O 스킬은 그 슬롯에 재사용 불가
            {
                return false;
            }

            _isBottomSlotOverridden = true;
            _pendingSpecialSkill = egoSkill;
            _slottedSkills[1] = egoSkill;
            OnSlotChanged?.Invoke(1, egoSkill);
            return true;
        }

        public void RevertBottomSlotToOriginal()                                                    // 하단 슬롯에 장착된 E.G.O 스킬을 원래 공격 스킬로 전환
        {
            if (_slottedSkills.Count < 2 || !_isBottomSlotOverridden) return;

            _slottedSkills[1] = _bottomSlotBaseSkill;
            _pendingSpecialSkill = null;
            _isBottomSlotOverridden = false;

            OnSlotChanged?.Invoke(1, _slottedSkills[1]);
        }

        public void OnTurnStarted()                                                                  // 턴 시작 시 하단 슬롯에 E.G.O 스킬 장착 후 사용하지 않았을 경우
        {
            if (_slottedSkills.Count < 2) return;

            if (_isBottomSlotOverridden && _pendingSpecialSkill != null && _pendingSpecialSkill != _defenseSkill)      // 하단 슬롯 스킬을 해당 E.G.O 스킬로 대체
            {
                if (!_isBaseSkillPermanentEgo && _bottomSlotBaseSkill != null)                       // 기존 하단 슬롯의 스킬이 인격 기본 공격 스킬이었을 경우, 이미 사용한 스킬 리스트에 추가
                {
                    _discardPile.Add(_bottomSlotBaseSkill);
                }

                _bottomSlotBaseSkill = _pendingSpecialSkill;
                _slottedSkills[1] = _pendingSpecialSkill;
                _isBaseSkillPermanentEgo = true;
                _isBottomSlotOverridden = false;
                _pendingSpecialSkill = null;

                OnSlotChanged?.Invoke(1, _slottedSkills[1]);
            }

            OnTurnSynced?.Invoke();
        }

        public bool IsBottomSlotDefense()                                                             // 하단 슬롯이 수비 스킬로 전환되어 있는지 확인
        {
            if (_slottedSkills.Count < 2) return false;
            return _slottedSkills[1] == _defenseSkill;
        }

        public void ExpandSlot()
        {
            SkillData drawn = DrawCard();
            _slottedSkills.Add(drawn);
            OnSlotChanged?.Invoke(_slottedSkills.Count - 1, drawn);
        }
    }
}
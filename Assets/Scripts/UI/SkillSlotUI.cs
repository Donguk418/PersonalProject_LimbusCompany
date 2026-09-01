using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class SkillSlotUI : MonoBehaviour
    {
        [Header("인격 일러스트 및 스탯 UI")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _sanityText;

        [Header("속도 주사위 텍스트")]
        [SerializeField] private TextMeshProUGUI _speedText;

        [Header("상단 스킬 슬롯 UI")]
        [SerializeField] private CanvasGroup _upperSkillCanvasGroup;
        [SerializeField] private Image _upperSkillIcon;
        [SerializeField] private TextMeshProUGUI _upperSkillNameText;

        [Header("하단 스킬 슬롯 UI (일반/수비/E.G.O)")]
        [SerializeField] private CanvasGroup _lowerSkillCanvasGroup;
        [SerializeField] private Image _lowerSkillIcon;
        [SerializeField] private TextMeshProUGUI _lowerSkillNameText;

        [Header("합 예상 및 상태 배지 UI")]
        [SerializeField] private GameObject _clashBadgeObject;
        [SerializeField] private TextMeshProUGUI _clashPredictionText;

        [Header("투명도 및 입력 세팅")]
        [SerializeField] private float _unassignedAlpha = 0.5f;                      // 스킬 미지정 대기 상태 투명도(조금 연하게)
        [SerializeField] private float _selectedAlpha = 1.0f;                        // 사용할 스킬 투명도(진하게)
        [SerializeField] private float _discardedAlpha = 0.15f;                      // 사용하지 않을 스킬 투명도(매우 연하게)
        [SerializeField] private float _holdThreshold = 1.0f;                        // 길게 누르기 기준 시간

        private BattleInfo _boundSlot;
        private bool _isDefenseMode;
        private bool _isPointerDownOnPortrait;
        private float _portraitPointerDownTime;

        public static event Action<SkillSlotUI, int, Vector2> OnSkillDragStarted;   // (슬롯, 스킬인덱스 0 or 1, 마우스시작위치)
        public static event Action<Vector2> OnSkillDragging;
        public static event Action<SkillSlotUI, GameObject> OnSkillDragEnded;
        public static event Action<CharacterRuntime> OnOpenEgoPopupRequested;

        public BattleInfo BoundSlot => _boundSlot;
        public bool IsDefenseMode => _isDefenseMode;

        public void Bind(BattleInfo slot)
        {
            _boundSlot = slot;

            _boundSlot.TargetSlot = null;

            UpdateView();
        }

        public void SelectSkillSlot(int skillIndex)
        {
            if (_boundSlot == null)
            {
                return;
            }

            _boundSlot.SelectedSkillIndex = skillIndex;
            UpdateView();
        }

        public void UpdateView()
        {
            if (_boundSlot == null || _boundSlot.Owner == null)
            {
                return;
            }

            var unit = _boundSlot.Owner;

            if (_hpText != null) _hpText.text = $"{unit.CurrentHp}";
            if (_sanityText != null) _sanityText.text = $"{unit.CurrentSanity}";
            if (_speedText != null) _speedText.text = $"{_boundSlot.Speed}";

            var deck = unit.DeckHandler;
            if (deck != null)
            {
                if (deck.SlottedSkills.Count > 0 && deck.SlottedSkills[0] != null)                   // 상단 슬롯 스킬
                {
                    if (_upperSkillNameText != null)
                        _upperSkillNameText.text = deck.SlottedSkills[0].SkillName;
                }

                if (deck.SlottedSkills.Count > 1 && deck.SlottedSkills[1] != null)                   // 하단 슬롯 스킬
                {
                    if (_lowerSkillNameText != null)
                        _lowerSkillNameText.text = deck.SlottedSkills[1].SkillName;
                }
            }

            ApplyAlphaStates();
        }

        private void ApplyAlphaStates()
        {
            if (_boundSlot.TargetSlot == null)
            {
                if (_upperSkillCanvasGroup != null) _upperSkillCanvasGroup.alpha = _unassignedAlpha;
                if (_lowerSkillCanvasGroup != null) _lowerSkillCanvasGroup.alpha = _unassignedAlpha;
                if (_clashBadgeObject != null) _clashBadgeObject.SetActive(false);
            }

            else
            {
                bool isUpper = _boundSlot.SelectedSkillIndex == 0;

                if (_upperSkillCanvasGroup != null)
                    _upperSkillCanvasGroup.alpha = isUpper ? _selectedAlpha : _discardedAlpha;

                if (_lowerSkillCanvasGroup != null)
                    _lowerSkillCanvasGroup.alpha = isUpper ? _discardedAlpha : _selectedAlpha;

                if (_clashBadgeObject != null)
                {
                    _clashBadgeObject.SetActive(true);
                    if (_clashPredictionText != null) _clashPredictionText.text = GetClashPredictionText();
                }
            }
        }

        public void OnPortraitPointerDown()
        {
            _isPointerDownOnPortrait = true;
            _portraitPointerDownTime = Time.time;
        }

        public void OnPortraitPointerUp()
        {
            if (!_isPointerDownOnPortrait)
            {
                return;
            }

            _isPointerDownOnPortrait = false;

            float holdDuration = Time.time - _portraitPointerDownTime;

            if (holdDuration >= _holdThreshold)
            {
                Debug.Log($"[{_boundSlot.Owner.name}] E.G.O 선택 팝업 요청");                      // 1초 이상 길게 누를 경우 E.G.O 선택 팝업 요청
                OnOpenEgoPopupRequested?.Invoke(_boundSlot.Owner);
            }
            else
            {
                ToggleDefenseSkill();                                                              // 짧게 클릭 시 수비 스킬 장착
            }
        }

        private void ToggleDefenseSkill()
        {
            if (_boundSlot == null || _boundSlot.Owner == null)
            {
                return;
            }

            var deck = _boundSlot.Owner.DeckHandler;
            if (deck == null)
            {
                return;
            }

            if (!deck.IsBottomSlotDefense())
            {
                Debug.Log($"[{_boundSlot.Owner.name}]의 하단 슬롯을 수비 스킬로 전환.");
                deck.SwitchBottomSlotToDefense();                                                 // 덱 핸들러 내부에서 하단 슬롯을 수비 스킬로 교체
                SelectSkillSlot(1);
            }
            else
            {
                Debug.Log($"[{_boundSlot.Owner.name}]의 하단 슬롯을 기본 스킬로 복구");
                deck.RevertBottomSlotToOriginal();                                                 // 덱 핸들러 내부에서 원래 공격 스킬로 복구
                SelectSkillSlot(_boundSlot.SelectedSkillIndex);
            }

            UpdateView();
        }



        public void OnUpperSkillBeginDrag(BaseEventData data)
        {
            SelectSkillSlot(0);
            if (data is PointerEventData pData)
            {
                OnSkillDragStarted?.Invoke(this, 0, pData.position);
            }
        }

        public void OnLowerSkillBeginDrag(BaseEventData data)
        {
            SelectSkillSlot(1);
            if (data is PointerEventData pData)
            {
                OnSkillDragStarted?.Invoke(this, 1, pData.position);
            }
        }

        public void OnSkillDrag(BaseEventData data)
        {
            if (data is PointerEventData pData)
            {
                OnSkillDragging?.Invoke(pData.position);
            }
        }

        public void OnSkillEndDrag(BaseEventData data)
        {
            if (data is PointerEventData pData)
            {
                GameObject droppedTarget = pData.pointerCurrentRaycast.gameObject;
                OnSkillDragEnded?.Invoke(this, droppedTarget);
            }
        }

        public void SetTargetSlot(BattleInfo enemySlot)                                              // 합 강탈 로직
        {
            if (enemySlot == null || _boundSlot == null)
            {
                return;
            }

            _boundSlot.TargetSlot = enemySlot;

            if (_boundSlot.Speed > enemySlot.Speed)
            {
                Debug.Log($" 합 강탈 : {gameObject.name} (속도 {_boundSlot.Speed}) : 적 (속도 {enemySlot.Speed})");
            }
            else
            {
                Debug.Log($"대상보다 속도가 낮아 합 강탈 실패 : {gameObject.name} (속도 {_boundSlot.Speed}) : 적 (속도 {enemySlot.Speed})");
            }

            UpdateView();
        }

        private string GetClashPredictionText()
        {
            if (_boundSlot == null || _boundSlot.TargetSlot == null)
            {
                return "일방 공격";
            }

            // TODO: 아군 선택 스킬 위력 vs 적 슬롯 스킬 위력 기댓값 비교 로직 연동
            // (매우 우세 / 우세 / 균등 / 불리 / 매우 불리)
            return "합 대치";
        }
    }
}
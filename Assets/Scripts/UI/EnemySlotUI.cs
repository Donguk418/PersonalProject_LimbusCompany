using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class EnemySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("적 슬롯 UI 요소")]
        [SerializeField] private TextMeshProUGUI _speedText;
        [SerializeField] private Image _skillIcon;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private GameObject _targetedHighlight;
        [SerializeField] private Transform _arrowAnchor;                    // 화살표가 꽂히거나 시작할 기준 앵커
        [SerializeField] private Image _hpGaugeImage;

        private BattleInfo _boundSlot;

        private readonly HashSet<SkillSlotUI> _targetingSlots = new();      // 현재 해당 슬롯을 타게팅중인 아군의 스킬 

        public static event Action<SkillSlotUI, EnemySlotUI> OnSlotTargetAssigned;
        public static event Action<BattleInfo, Vector2> OnShowEnemySkillTooltipRequested;
        public static event Action OnHideEnemySkillTooltipRequested;

        public BattleInfo BoundSlot => _boundSlot;
        public Transform ArrowAnchor => _arrowAnchor != null ? _arrowAnchor : transform;

        public void Bind(BattleInfo slot)
        {
            _boundSlot = slot;
            _targetingSlots.Clear();

            UpdateHighlightState();
            UpdateView();
        }

        public void UpdateView()
        {
            if (_boundSlot == null || _boundSlot.Owner == null)
            {
                return;
            }

            if (_speedText != null)
            {
                _speedText.text = $"{_boundSlot.Speed}";
            }

            var skill = _boundSlot.GetSelectedSkill();
            if (skill != null)
            {
                if (_skillNameText != null)
                {
                    _skillNameText.text = skill.SkillName;
                }

                if (_skillIcon != null && skill.SkillSprite != null)
                {
                    _skillIcon.sprite = skill.SkillSprite;
                }
            }
            else
            {
                if (_skillNameText != null)
                {
                    _skillNameText.text = "행동 대기";
                }
            }

            if (_hpGaugeImage != null)
            {
                float maxHp = _boundSlot.Owner.MaxHp;
                float currentHp = _boundSlot.Owner.CurrentHp;
                _hpGaugeImage.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            GameObject draggedObject = eventData.pointerDrag;
            if (draggedObject == null)
            {
                return;
            }

            if (_boundSlot == null || _boundSlot.Owner == null || _boundSlot.Owner.IsDead)                  // 흐트러짐, 사망 등으로 타겟 지정이 불가능할 경우
            {
                Debug.LogWarning($"[{gameObject.name}] 유효하지 않은 대상입니다.");
                return;
            }

            var playerSlotUI = draggedObject.GetComponentInParent<SkillSlotUI>();
            if (playerSlotUI != null)
            {
                // 타깃 리스트에 등록 및 아군 슬롯 갱신
                RegisterTargeter(playerSlotUI);
                playerSlotUI.SetTargetSlot(_boundSlot);

                OnSlotTargetAssigned?.Invoke(playerSlotUI, this);
            }
        }

        public void RegisterTargeter(SkillSlotUI playerSlot)
        {
            if (playerSlot == null)
            {
                return;
            }

            if (!_targetingSlots.Contains(playerSlot))
            {
                _targetingSlots.Add(playerSlot);
            }

            UpdateHighlightState();
        }

        public void UnregisterTargeter(SkillSlotUI playerSlot)
        {
            if (playerSlot == null)
            {
                return;
            }

            if (_targetingSlots.Contains(playerSlot))
            {
                _targetingSlots.Remove(playerSlot);
            }

            UpdateHighlightState();
        }

        private void UpdateHighlightState()
        {
            if (_targetedHighlight != null)
            {
                _targetedHighlight.SetActive(_targetingSlots.Count > 0);         // 자신을 타겟으로 지정한 수감자가 1명 이상일 경우 하이라이트 적용
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_boundSlot != null && _boundSlot.GetSelectedSkill() != null)
            {
                OnShowEnemySkillTooltipRequested?.Invoke(_boundSlot, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHideEnemySkillTooltipRequested?.Invoke();
        }
    }
}
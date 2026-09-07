using System;
using System.Collections.Generic;
using UnityEngine;

namespace Limbus.Data
{
    [Serializable]
    public class ResourceCost
    {
        [SerializeField] private ResourceConsumeTrigger _resourceTrigger = ResourceConsumeTrigger.OnUse;
        [SerializeField] private int _hpCost;
        [SerializeField] private int _spCost;

        public ResourceConsumeTrigger ResourceTrigger => _resourceTrigger;
        public int HPCost => _hpCost;
        public int SpCost => _spCost;
    }

    [CreateAssetMenu(fileName = "SkillData", menuName = "Limbus/Data/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 스킬 정보")]
        [SerializeField] private string _skillId;
        [SerializeField] private string _skillName;
        [SerializeField] private Sprite _skillIcon;

        [Tooltip("스킬셋 번호")]
        [Range(1, 3)]
        [SerializeField] private int _skillNum = 1;

        [Header("공격, 수비 스킬 구분")]
        [SerializeField] private SkillCategory _skillCategory = SkillCategory.Active;
        [SerializeField] private SinAttribute _sinAttribute = SinAttribute.None;
        [SerializeField] private AttackType _attackType = AttackType.None;
        //[SerializeField] private DefenseType _defenseType = DefenseType.None;

        //[Header("타겟팅 및 가중치 메커니즘")]
        //[Tooltip("공격 가중치")]
        //[Range(1, 7)]
        //[SerializeField] private int _targetWeight = 1;

        [Tooltip("공격 대상")]
        [SerializeField] private TargetType _targetType = TargetType.Enemy;

        //[Tooltip("광역 난사 여부")]
        //[SerializeField] private bool _isRandomMultiTarget = false;

        [Header("위력 및 코스트 매커니즘")]
        [SerializeField] private int _basePower;
        [SerializeField] private ResourceCost _resourceCost;

        //[Tooltip("스킬 사용 시 획득하는 E.G.O 자원 수량")]
        //[SerializeField] private int _egoResourceGainAmount = 1;

        //[SerializeField] private bool _isAssistSkill;

        [Header("코인 데이터")]
        [SerializeField] private List<CoinData> _coins = new();

        [Header("적 AI 타깃팅 규칙")]
        [SerializeField] private TargetingPriority _targetingPriority = TargetingPriority.Random;

        [Header("UI 리소스")]
        [SerializeField] private Sprite _skillSprite;

        public string SkillId => _skillId;
        public string SkillName => _skillName;
        public Sprite SkillIcon => _skillIcon;
        public int SkillNum => _skillNum;
        public SkillCategory SkillCategory => _skillCategory;
        public SinAttribute SinAttribute => _sinAttribute;
        public AttackType AttackType => _attackType;
        //public DefenseType DefenseType => _defenseType;
        //public int TargetWeight => _targetWeight;
        public TargetType TargetType => _targetType;
        //public bool IsRandomMultiTarget => _isRandomMultiTarget;
        public int BasePower => _basePower;
        public ResourceCost ResourceCost => _resourceCost;
        //public int EgoResourceGainAmount => _egoResourceGainAmount;
        //public bool IsAssistSkill => _isAssistSkill;
        public IReadOnlyList<CoinData> Coins => _coins;
        public TargetingPriority TargetingPriority => _targetingPriority;
        public Sprite SkillSprite => _skillSprite;
    }
}
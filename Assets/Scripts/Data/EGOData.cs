using System;
using System.Collections.Generic;
using UnityEngine;

namespace Limbus.Data
{
    [Serializable] public class EgoResourceCost
    {
        [SerializeField] private SinAttribute _sinAttribute = SinAttribute.None;
        [SerializeField] private int _amount = 1;

        public SinAttribute SinAttribute => _sinAttribute;
        public int Amount => _amount;
    }

    [CreateAssetMenu(fileName = "NewEgoData", menuName = "Limbus/Data/E.G.O Data")]
    public class EgoData : ScriptableObject
    {
        [Header("E.G.O 기본 정보")]
        [SerializeField] private string _egoId;
        [SerializeField] private string _egoName;
        [SerializeField] private Sprite _egoIcon;
        [SerializeField] private EgoRiskLevel _riskLevel = EgoRiskLevel.ZAYIN;
        [SerializeField] private SinAttribute _mainSinAttribute = SinAttribute.None;

        [Header("E.G.O 환상 해석 레벨")]
        [Tooltip("현재 E.G.O의 최대 환상 해석 단계")]
        [Range(1, 5)]
        [SerializeField] private int _maxUptieLevel = 5;

        [Header("E.G.O 스탯 보정치")]
        [Tooltip("공격 레벨 보정치")]
        [SerializeField] private int _offenseLevelOffset = 0;     //  (예: -4 입력 시 'E.G.O 공격 레벨 : 인격 레벨 - 4')

        [Header("E.G.O 코스트 (정신력 및 죄악 자원)")]
        [SerializeField] private int _spCost = 10;
        [SerializeField] private List<EgoResourceCost> _resourceCosts = new();

        [Header("E.G.O 패시브 정보")]
        [SerializeField] private string _egoPassiveName;
        [TextArea(2, 5)]
        [SerializeField] private string _egoPassiveDescription;

        [Header("연결된 스킬 데이터")]
        [SerializeField] private SkillData _awakeningSkill;
        [SerializeField] private SkillData _corrosionSkill;


        public string EgoId => _egoId;
        public string EgoName => _egoName;
        public Sprite EgoIcon => _egoIcon;
        public EgoRiskLevel RiskLevel => _riskLevel;
        public SinAttribute MainSinAttribute => _mainSinAttribute;
        public int MaxUptieLevel => _maxUptieLevel;
        public int OffenseLevelOffset => _offenseLevelOffset;
        public int SpCost => _spCost;
        public IReadOnlyList<EgoResourceCost> ResourceCosts => _resourceCosts;
        public string EgoPassiveName => _egoPassiveName;
        public string EgoPassiveDescription => _egoPassiveDescription;
        public SkillData AwakeningSkill => _awakeningSkill;
        public SkillData CorrosionSkill => _corrosionSkill;
    }
}
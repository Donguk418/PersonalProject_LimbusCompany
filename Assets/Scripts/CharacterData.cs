using System;
using System.Collections.Generic;
using UnityEngine;

namespace Limbus.Data
{
    [Serializable] public class AttackResistances
    {
        [SerializeField] private float _slash = 1.0f;  // 내성 계수 : 0.5 = 내성, 2.0 = 취약)
        [SerializeField] private float _pierce = 1.0f;
        [SerializeField] private float _blunt = 1.0f;

        public float Slash => _slash;
        public float Pierce => _pierce;
        public float Blunt => _blunt;
    }

    [Serializable]
    public class SanityConditionData
    {
        [SerializeField] private int _maxSanity = 45;
        [SerializeField] private int _minSanity = -45;

        [Header("정신력 증가 조건 수치")]
        [SerializeField] private int _clashWinSpGain = 10;
        [SerializeField] private int _killEnemySpGain = 10;
        [SerializeField] private int _allyClashWinSpGain = 5;

        [Header("정신력 감소 조건 수치")]
        [SerializeField] private int _clashLoseSpLoss = 10;
        [SerializeField] private int _allyDeathSpLoss = 10;

        public int MaxSanity => _maxSanity;
        public int MinSanity => _minSanity;
        public int ClashWinSpGain => _clashWinSpGain;
        public int KillEnemySpGain => _killEnemySpGain;
        public int AllyDeathSpLoss => _allyDeathSpLoss;
    }

    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Limbus/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("수감자 및 인격 기본 정보")]
        [Tooltip("수감자 ID")]
        [SerializeField] private string _characterId;

        [Tooltip("수감자 이름")]
        [SerializeField] private string _characterName;

        [Tooltip("인격 ID")]
        [SerializeField] private string _identityId;

        [Tooltip("인격 이름")]
        [SerializeField] private string _identityName;

        [SerializeField] private Sprite _characterPortrait;

        [Header("소속 및 특성 키워드")]
        [SerializeField] private List<string> _factionKeywords = new();

        [Header("기본 스탯 및 레벨별 성장 계수")]
        [Tooltip("현재 인격 레벨 상한")]
        [SerializeField] private int _maxLevel = 60;

        [SerializeField] private int _baseMaxHp = 100;
        [SerializeField] private float _hpGrowthRate = 10.0f;

        [SerializeField] private int _baseOffenseLevel = 10;
        [SerializeField] private float _offenseGrowthRate = 1.0f;

        [SerializeField] private int _baseDefenseLevel = 10;
        [SerializeField] private float _defenseGrowthRate = 1.0f;

        [Header("속도 범위")]
        [SerializeField] private int _minSpeed = 2;
        [SerializeField] private int _maxSpeed = 5;

        [Header("내성 정보 및 정신력 메커니즘")]
        [SerializeField] private AttackResistances _attackResistances;
        [SerializeField] private SanityConditionData _sanityCondition;

        [Header("보유 스킬 정보")]
        [SerializeField] private List<SkillData> _skillList = new();
        [SerializeField] private SkillData _defenseSkill;


        public string CharacterId => _characterId;
        public string CharacterName => _characterName;
        public string IdentityId => _identityId;
        public string IdentityName => _identityName;
        public Sprite CharacterPortrait => _characterPortrait;

        public IReadOnlyList<string> FactionKeywords => _factionKeywords;

        public int MinSpeed => _minSpeed;
        public int MaxSpeed => _maxSpeed;

        public AttackResistances AttackResistances => _attackResistances;
        public SanityConditionData SanityCondition => _sanityCondition;

        public IReadOnlyList<SkillData> SkillList => _skillList;
        public SkillData DefenseSkill => _defenseSkill;


        public int GetMaxHp(int level) => Mathf.RoundToInt(_baseMaxHp + (Mathf.Min(level, _maxLevel) - 1) * _hpGrowthRate);
        public int GetOffenseLevel(int level) => Mathf.RoundToInt(_baseOffenseLevel + (Mathf.Min(level, _maxLevel) - 1) * _offenseGrowthRate);
        public int GetDefenseLevel(int level) => Mathf.RoundToInt(_baseDefenseLevel + (Mathf.Min(level, _maxLevel) - 1) * _defenseGrowthRate);
    }
}
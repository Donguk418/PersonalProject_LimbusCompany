using System.Collections.Generic;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class CharacterRuntime : MonoBehaviour
    {
        [Header("정적 데이터 에셋 참조")]
        [SerializeField] private CharacterData _characterData;

        [Header("런타임 스탯")]
        [SerializeField] private int _currentLevel = 60;
        [SerializeField] private int _currentHp;
        [SerializeField] private int _maxHp;
        [SerializeField] private int _offenseLevel;
        [SerializeField] private int _defenseLevel;
        [SerializeField] private int _currentSanity;
        [SerializeField] private int _currentSpeed;

        [Header("스킬 장착 상태(기본 1/2/3스킬 및 수비)")]
        [SerializeField] private List<SkillData> _equippedSkills = new();

        private float _bonusCoinProbability = 0f;

        public CharacterData CharacterData => _characterData;
        public int CurrentLevel => _currentLevel;
        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public int OffenseLevel => _offenseLevel;
        public int DefenseLevel => _defenseLevel;
        public int CurrentSanity => _currentSanity;
        public int CurrentSpeed => _currentSpeed;
        public float BonusCoinProbability => _bonusCoinProbability;
        public IReadOnlyList<SkillData> EquippedSkills => _equippedSkills;

        public bool IsDead => _currentHp <= 0;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_characterData == null) return;

            _maxHp = _characterData.GetMaxHp(_currentLevel);
            _currentHp = _maxHp;
            _offenseLevel = _characterData.GetOffenseLevel(_currentLevel);
            _defenseLevel = _characterData.GetDefenseLevel(_currentLevel);
            _currentSanity = 0;
            _bonusCoinProbability = 0f;

            _equippedSkills.Clear();
            _equippedSkills.AddRange(_characterData.SkillList);

            RollSpeed();
        }

        public void RollSpeed()
        {
            if (_characterData == null) return;
            _currentSpeed = Random.Range(_characterData.MinSpeed, _characterData.MaxSpeed + 1);
        }

        public void ModifySanity(int amount)
        {
            int min = _characterData != null && _characterData.SanityCondition != null ? _characterData.SanityCondition.MinSanity : -45;
            int max = _characterData != null && _characterData.SanityCondition != null ? _characterData.SanityCondition.MaxSanity : 45;
            _currentSanity = Mathf.Clamp(_currentSanity + amount, min, max);
        }

        public void SetBonusCoinProbability(float bonus)
        {
            _bonusCoinProbability = bonus;
        }

        public void TakeDamage(int damage)
        {
            _currentHp = Mathf.Max(0, _currentHp - damage);
            Debug.Log($"[{name}]가 {damage}의 피해를 입음. 남은 HP: {_currentHp}/{_maxHp}");
        }
    }
}
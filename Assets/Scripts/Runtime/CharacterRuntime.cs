using System;
using System.Collections.Generic;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public class CharacterRuntime : MonoBehaviour
    {
        [Header("정적 데이터 에셋 참조")]
        [SerializeField] private CharacterData _characterData;

        [Header("레벨 기본값")]
        [SerializeField] private int _defaultLevel = 60;

        [Header("런타임 스탯")]
        [SerializeField] private CharacterStat _stat = new();

        [Header("스킬 장착 상태")]
        [SerializeField] private List<SkillData> _equippedSkills = new();

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnSanityChanged;
        public event Action<int> OnDamageTaken;
        public event Action<int> OnSpeedChanged;

        public CharacterData CharacterData => _characterData;
        public CharacterStat Stat => _stat;
        public int CurrentLevel => _stat.CurrentLevel;
        public int CurrentHp => _stat.CurrentHp;
        public int MaxHp => _stat.MaxHp;
        public int OffenseLevel => _stat.OffenseLevel;
        public int DefenseLevel => _stat.DefenseLevel;
        public int CurrentSanity => _stat.CurrentSanity;
        public int CurrentSpeed => _stat.CurrentSpeed;
        public float BonusCoinProbability
        {
            get => _stat.BonusCoinProbability;
            set => _stat.BonusCoinProbability = value;
        }
        public IReadOnlyList<SkillData> EquippedSkills => _equippedSkills;

        private void Awake()
        {
            if (_characterData != null)
            {
                InitializeCharacter();
            }
        }

        public void InitializeCharacter()
        {
            if (_characterData == null) return;

            _stat.Initialize(_characterData, _defaultLevel);

            _equippedSkills.Clear();
            if (_characterData.SkillList != null)
            {
                _equippedSkills.AddRange(_characterData.SkillList);
            }

            OnHpChanged?.Invoke(_stat.CurrentHp, _stat.MaxHp);
            OnSanityChanged?.Invoke(_stat.CurrentSanity);
        }

        public void RollSpeed()
        {
            if (_characterData == null) return;
            int speed = UnityEngine.Random.Range(_characterData.MinSpeed, _characterData.MaxSpeed + 1);
            _stat.SetSpeed(speed);
            OnSpeedChanged?.Invoke(_stat.CurrentSpeed);
        }

        public void ModifySanity(int amount)
        {
            int minSp = _characterData != null && _characterData.SanityCondition != null
                ? _characterData.SanityCondition.MinSanity : -45;
            int maxSp = _characterData != null && _characterData.SanityCondition != null
                ? _characterData.SanityCondition.MaxSanity : 45;

            _stat.ModifySanity(amount, minSp, maxSp);
            OnSanityChanged?.Invoke(_stat.CurrentSanity);
        }

        public void TakeDamage(int damage)
        {
            _stat.ModifyHp(-damage);
            OnHpChanged?.Invoke(_stat.CurrentHp, _stat.MaxHp);
            OnDamageTaken?.Invoke(damage);
        }
    }
}
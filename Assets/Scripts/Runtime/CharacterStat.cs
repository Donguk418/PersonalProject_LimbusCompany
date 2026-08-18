using System;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    [Serializable]
    public class CharacterStat
    {
        [Header("레벨 및 HP")]
        [SerializeField] private int _currentLevel = 60;
        [SerializeField] private int _currentHp;
        [SerializeField] private int _maxHp;

        [Header("공격, 방어 레벨 및 정신력, 속도")]
        [SerializeField] private int _offenseLevel;
        [SerializeField] private int _defenseLevel;
        [SerializeField] private int _currentSanity;
        [SerializeField] private int _currentSpeed;

        public int CurrentLevel => _currentLevel;
        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public int OffenseLevel => _offenseLevel;
        public int DefenseLevel => _defenseLevel;
        public int CurrentSanity => _currentSanity;
        public int CurrentSpeed => _currentSpeed;
        public float BonusCoinProbability { get; set; } = 0f;

        public void Initialize(CharacterData data, int level)
        {
            if (data == null) return;

            _currentLevel = level;
            _maxHp = data.GetMaxHp(_currentLevel);
            _currentHp = _maxHp;
            _offenseLevel = data.GetOffenseLevel(_currentLevel);
            _defenseLevel = data.GetDefenseLevel(_currentLevel);
            _currentSanity = 0;
            _currentSpeed = 0;
            BonusCoinProbability = 0f;
        }

        public void SetSpeed(int speed)
        {
            _currentSpeed = speed;
        }

        public void ModifyHp(int amount)
        {
            _currentHp = Mathf.Clamp(_currentHp + amount, 0, _maxHp);
        }

        public void ModifySanity(int amount, int minSp, int maxSp)
        {
            _currentSanity = Mathf.Clamp(_currentSanity + amount, minSp, maxSp);
        }
    }
}
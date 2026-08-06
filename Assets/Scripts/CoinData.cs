using System;
using System.Collections.Generic;
using UnityEngine;

namespace Limbus.Data
{
    [Serializable] public class CoinEffect
    {
        [SerializeField] private CoinEffectTrigger _coinEffectTrigger;
        [SerializeField] private KeyWordType _keyWordType;
        [SerializeField] private int _stackValue;
        [SerializeField] private int _stackAmount;
        [SerializeField] private float _damageMultiplier = 1.0f;

        public CoinEffectTrigger CoinEffectTrigger => _coinEffectTrigger;
        public KeyWordType KeyWordType => _keyWordType;
        public int StackValue => _stackValue;
        public int StackAmount => _stackAmount;
        public float DamageMultiplier => _damageMultiplier;
    }

    [Serializable] public class CoinData
    {
        [SerializeField] private int _coinPower;
        [SerializeField] private string _animationTriggerName;
        [SerializeField] private CoinType _coinType = CoinType.Normal;
        [SerializeField] private List<CoinEffect> _coinEffects = new List<CoinEffect>();

        public int CoinPower => _coinPower;
        public string AnimationTriggerName => _animationTriggerName;
        public CoinType CoinType => _coinType;
        public IReadOnlyList<CoinEffect> CoinEffects => _coinEffects;
    }
}

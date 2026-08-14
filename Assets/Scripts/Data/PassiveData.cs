using UnityEngine;

namespace Limbus.Data
{
    [CreateAssetMenu(fileName = "NewPassiveData", menuName = "Limbus/Data/Passive Data")]
    public class PassiveData : ScriptableObject
    {
        [Header("패시브 기본 정보")]
        [SerializeField] private string _passiveId;
        [SerializeField] private string _passiveName;
        [TextArea(2, 5)]
        [SerializeField] private string _description;

        [Header("패시브 분류 및 지속 범위")]
        [SerializeField] private PassiveType _passiveType = PassiveType.IdentityBattle;
        [SerializeField] private PassiveDuration _duration = PassiveDuration.CurrentTurn;

        [Header("발동 조건 메커니즘")]
        [SerializeField] private PassiveConditionType _conditionType = PassiveConditionType.SinResonance;
        [SerializeField] private SinAttribute _targetSin = SinAttribute.None;
        [SerializeField] private int _requiredAmount = 3;

        public string PassiveId => _passiveId;
        public string PassiveName => _passiveName;
        public string Description => _description;
        public PassiveType PassiveType => _passiveType;
        public PassiveDuration Duration => _duration;
        public PassiveConditionType ConditionType => _conditionType;
        public SinAttribute TargetSin => _targetSin;
        public int RequiredAmount => _requiredAmount;
    }
}
using UnityEngine;

namespace Limbus.Data
{
    public enum SkillCategory
    {
        Active = 0,
        Defense = 1,
        Ego = 2,
        EgoCorrosion = 3
    }

    public enum SinAttribute
    {
        None = 0,
        Wrath = 1,
        Lust = 2,
        Sloth = 3,
        Gluttony = 4,
        Gloom = 5,
        Pride = 6,
        Envy = 7
    }

    public enum AttackType
    {
        None = 0,
        Slash = 1,
        Pierce = 2,
        Blunt = 3
    }

    public enum DefenseType
    {
        None = 0,
        Guard = 1,
        Evade = 2,
        Counter = 3
    }

    public enum TargetType
    {
        Enemy = 0,
        Ally = 1,
        Any = 2,
        Self = 3
    }

    public enum CoinEffectTrigger
    {
        None = 0,
        OnStartBattle = 1,
        OnBeforeUse = 2,
        OnUse = 3,
        OnHit = 4,
        OnCrit = 5,
        OnClashWin = 6,
        OnClashLose = 7,
        OnEvadeSuccess = 8,
        OnUnbreakableCoinHit = 9,
        OnEndSkill = 10,
        OnAllyClashWin = 11,
        OnAllyAttack = 12,
        OnAllyClashLose = 13,
        OnAllyHit = 14
    }

    public enum CoinType
    {
        Normal = 0,
        Unbreakable = 1,
        Excision = 2,
        Purple = 3
    }

    public enum KeyWordType
    {
        None = 0,
        Burn = 1,
        Bleed = 2,
        Tremor = 3,
        Rupture = 4,
        Sinking = 5,
        Poise = 6,
        Charge = 7,

        TremorBurst = 101,
        SinkingDeluge = 102
    }

    public enum EgoRiskLevel
    {
        None = 0,
        ZAYIN = 1,
        TETH = 2,
        HE = 3,
        WAW = 4,
        ALEPH = 5
    }

    public enum ResourceConsumeTrigger
    {
        None = 0,
        OnStartBattle = 1,
        OnBeforeUse = 2,
        OnUse = 3,
        OnHit = 4,
        OnCrit = 5,
        OnClashWin = 6,
        OnClashLose = 7,
        OnEvadeSuccess = 8,
        OnUnbreakableCoinHit = 9,
        OnEndSkill = 10,
        OnAllyClashWin = 11,
        OnAllyAttack = 12,
        OnAllyClashLose = 13,
        OnAllyHit = 14
    }

    public enum PassiveType
    {
        IdentityBattle = 0,
        IdentitySupport = 1,
        Ego = 2
    }

    public enum PassiveConditionType
    {
        None = 0,
        SinResonance = 1,
        AbsouluteSinResonance = 2,
        ResourceOwned = 3,
        EgoUsed = 4
    }

    public enum PassiveDuration
    {
        CurrentTurn = 0,
        EntireBattle = 1
    }
}
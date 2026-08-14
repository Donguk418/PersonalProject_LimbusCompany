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

    public enum BuffType
    {
        None = 0,                // 효과 없음
        PowerUp = 1,             // 위력 증가
        PowerDown = 2,           // 위력 감소
        OffensePowerUp = 3,      // 공격 위력 증가
        OffensePowerDown = 4,    // 공격 위력 감소
        DefensePowerUp = 5,      // 수비 위력 증가
        DefensePowerDown = 6,    // 수비 위력 감소
        ClashPowerUp = 7,        // 합 위력 증가
        ClashPowerDown = 8,      // 합 위력 감소
        PlusCoinBoost = 9,       // 더하기 코인 강화
        PlusCoinDrop = 10,       // 더하기 코인 약화
        MinusCoinBoost = 11,     // 빼기 코인 강화
        MinusCoinDrop = 12,      // 빼기 코인 약화
        SpeedUp = 13,            // 신속
        Bind = 14,               // 속박
        Paralyze = 15,           // 마비
        Stun = 16,               // 행동 불가
        Fragile = 17,            // 취약
        Protection = 18,         // 보호
        DamageUp = 19,           // 피해량 증가
        DamageDown = 20,         // 피해량 감소
        VulnerabilityHitUp = 21, // 약점 공격 시 피해 증가
        OffenseLevelUp = 22,     // 공격 레벨 증가
        OffenseLevelDown = 23,   // 공격 레벨 감소
        DefenseLevelUp = 24,     // 방어 레벨 증가
        DefenseLevelDown = 25,   // 방어 레벨 감소
        Taunt = 26,              // 도발치
        HealBoost = 27,          // 체력 회복량 증가
        HealDrop = 28,           // 체력 회복량 감소
        SpEfficiencyUp = 29,     // 정신력 회복/감소 효율 증가
        SpEfficiencyDown = 30,   // 정신력 회복/감소 효율 감소
        CoverGuard = 31          // 원호 방어
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
        AbsoluteSinResonance = 2,
        ResourceOwned = 3,
        EgoUsed = 4
    }

    public enum PassiveDuration
    {
        CurrentTurn = 0,
        EntireBattle = 1
    }
}
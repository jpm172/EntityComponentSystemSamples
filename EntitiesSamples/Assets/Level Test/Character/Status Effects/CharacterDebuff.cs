using System;
using Unity.Collections;
using Unity.Entities;


[InternalBufferCapacity(20)]
public struct StatusEffect : IBufferElementData
{
    
    public Entity EffectEntity;
}

public struct BasicStatStatusEffect : IComponentData
{
    public StatType AffectedStat;
    public StatModType ModType;
    public float Value;

    public CharacterStats ApplyEffect(CharacterStats stats)
    {
        switch ( AffectedStat )
        {
            case StatType.Armor:
                stats.TotalStats.Armor = CalculateMod( stats.BaseStats.Armor, stats.TotalStats.Armor );
                return stats;
            case StatType.MoveSpeed:
                stats.TotalStats.MoveSpeed = CalculateMod( stats.BaseStats.MoveSpeed, stats.TotalStats.MoveSpeed );
                return stats;
            default:
                throw new IndexOutOfRangeException($"invalid AffectedStat: {(int) AffectedStat}");
        }
    }

    private float CalculateMod( float baseValue, float totalValue )
    {
        switch ( ModType )
        {
            case StatModType.Add:
                return totalValue + Value;
            case StatModType.Multiply:
                return totalValue + ( baseValue * Value );
            case StatModType.Absolute:
                return Value;
            default:
                throw new IndexOutOfRangeException($"invalid StatModType: {(int) ModType}");
        }
    }
}

//[WriteGroup(typeof(TimedStatusEffect))]
public struct BodyStatusEffect : IComponentData
{
    public BodyPart AffectedLimb;
    public BodyStatType AffectedStat;
    public StatModType ModType;
    public float Value;
    
    public CharacterStats ApplyEffect(CharacterStats stats)
    {
        switch ( AffectedLimb)
        {
            case BodyPart.Head:
                stats.TotalStats.HeadStats = ModLimb( stats.BaseStats.HeadStats, stats.TotalStats.HeadStats );
                return stats;
            case BodyPart.Chest:
                stats.TotalStats.ChestStats = ModLimb(stats.BaseStats.ChestStats, stats.TotalStats.ChestStats);
                return stats;
            case BodyPart.LeftArm:
                stats.TotalStats.LeftArmStats = ModLimb( stats.BaseStats.LeftArmStats, stats.TotalStats.LeftArmStats );
                return stats;
            case BodyPart.RightArm:
                stats.TotalStats.RightArmStats = ModLimb( stats.BaseStats.RightArmStats, stats.TotalStats.RightArmStats );
                return stats;
            case BodyPart.LeftLeg:
                stats.TotalStats.LeftLegStats = ModLimb( stats.BaseStats.LeftLegStats, stats.TotalStats.LeftLegStats );
                return stats;
            case BodyPart.RightLeg :
                stats.TotalStats.RightLegStats = ModLimb( stats.BaseStats.RightLegStats, stats.TotalStats.RightLegStats );
                return stats;
            default:
                throw new IndexOutOfRangeException($"invalid AffectedLimb: {(int) AffectedLimb}");
        }
    }

    private CharacterLimb ModLimb(CharacterLimb baseLimb, CharacterLimb totalLimb)
    {
        switch ( AffectedStat )
        {
            case BodyStatType.Condition:
                totalLimb.Condition = CalculateMod( baseLimb.Condition, totalLimb.Condition );
                return totalLimb;
            case BodyStatType.BleedResist:
                //totalLimb.BleedResist = CalculateMod( baseLimb.BleedResist, totalLimb.BleedResist );
                return totalLimb;
            default:
                throw new IndexOutOfRangeException($"invalid AffectedBodyStat: {(int) AffectedStat}");
        }
    }
    
    private float CalculateMod( float baseValue, float totalValue )
    {
        switch ( ModType )
        {
            case StatModType.Add:
                return totalValue + Value;
            case StatModType.Multiply:
                return totalValue + ( baseValue * Value );
            case StatModType.Absolute:
                return Value;
            default:
                throw new IndexOutOfRangeException($"invalid StatModType: {(int) ModType}");
        }
    }
    
}


public struct StatusEffectInfo : IComponentData
{
    public StatsuEffectID ID;
    public StatusEffectType Type;
    public StatusEffectQuality Quality;
    public bool Remove;
}

public struct NameInfo
{
    public string FullName;
    public string DisplayName;

    public NameInfo( string fullName, string displayName )
    {
        FullName = fullName;
        DisplayName = displayName;
    }
}

public struct StatusEffectTimer : IComponentData
{
    public float TimeRemaining;

    public StatusEffectTimer( float timer )
    {
        TimeRemaining = timer;
    }
}

public struct StatusEffectBodyListener : IComponentData
{
    public Entity Owner;
    public BodyPart TargetLimb;
    public float Threshold;
    public bool LessThan;


    public StatusEffectBodyListener( Entity owner, BodyPart targetLimb, float threshold, bool lessThan )
    {
        Owner = owner;
        TargetLimb = targetLimb;
        Threshold = threshold;
        LessThan = lessThan;
    }
    
    public readonly bool CheckLimb( CharacterLimb limb )
    {
        if ( LessThan )
            return limb.CurrentHealth < Threshold;

        return limb.CurrentHealth > Threshold;
    }
    
}

public struct InitializeStatusEffect : IComponentData, IEnableableComponent
{
}

public enum StatsuEffectID : int
{
    Tourniquet = 0,
    Broken = 1,
    
}

public enum StatusEffectType
{
    BasicStats,
    BodyStats
}

public enum StatusEffectQuality
{
    Buff,
    Neutral,
    Debuff
}

public enum StatType
{
    MoveSpeed,
    Armor
}

public enum BodyStatType
{
    Condition,
    BleedResist,
    
}

public enum StatModType
{
    Add,
    Multiply,
    Absolute
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


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

    public CharacterStats ApplyEffect(CharacterStats baseStats, CharacterStats totalStats)
    {
        switch ( AffectedStat )
        {
            case StatType.Armor:
                totalStats.Armor = CalculateMod( baseStats.Armor, totalStats.Armor );
                return totalStats;
            case StatType.MoveSpeed:
                totalStats.MoveSpeed = CalculateMod( baseStats.MoveSpeed, totalStats.MoveSpeed );
                return totalStats;
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
    public StatModType ModType;
    public float Value;
}


public struct StatusEffectInfo : IComponentData
{
    public StatusEffectType Type;
    public StatusEffectQuality Quality;
    public bool Remove;
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
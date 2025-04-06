using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


[InternalBufferCapacity(30)]
public struct StatusEffect : IBufferElementData
{
    public StatusEffectType Type;
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

[InternalBufferCapacity(30)]
public struct TimedStatusEffect : IBufferElementData
{
    //public Entity DebuffEntity;
    public StatType AffectedStat;
    public StatModType ModType;
    public float Value;
    public float Timer;

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
public struct BodyStatusEffect : IBufferElementData
{
    public BodyPart AffectedLimb;
    public StatModType ModType;
    public float Value;
}


public enum StatusEffectType
{
    BasicStats,
    BodyStats
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
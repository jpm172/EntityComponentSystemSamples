using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[InternalBufferCapacity(30)]
public struct CharacterWound : IBufferElementData
{
    public WoundType Type;
    public BodyPart AffectedPart;

    public float MaxHealing;
    public float HealingNeeded;
    public float MaxBleed;
    public float Bleed;

    public bool Healed => HealingNeeded <= math.EPSILON;
    public float HealProgress => HealingNeeded / MaxHealing;

    public CharacterWound(DamageInfo damageInfo, BodyPart part)
    {
        AffectedPart = part;
        HealingNeeded = MaxHealing = damageInfo.Damage;
        Bleed = MaxBleed = damageInfo.BleedDamage;
        
        if ( HealingNeeded <= 5 )
        {
            Type = WoundType.Minor;
        }
        else if ( HealingNeeded <= 15 )
        {
            Type = WoundType.Moderate;
        }
        else
        {
            Type = WoundType.Severe;
        }
    }

    public CharacterWound( CharacterWound woundInfo, BodyPart part )
    {
        AffectedPart = part;
        HealingNeeded = MaxHealing = woundInfo.HealingNeeded;
        Bleed = MaxBleed = woundInfo.Bleed;
        Type = woundInfo.Type;
    }

    public float2 Heal()
    {
        float healAmount = HealingNeeded;
        float bleedHealAmount = Bleed;

        HealingNeeded -= healAmount;
        Bleed -= bleedHealAmount;
        
        return new float2(healAmount, bleedHealAmount);
    }
    
    /*
    public float2 Heal(ref HealthItemDesc item)
    {
        float healAmount = math.min(HealingNeeded, item.CurrentCharges);
        HealingNeeded -= healAmount;
        
        float bleedingHealed = Bleed - (MaxBleed * (HealingNeeded/MaxHealing));
        Bleed -= bleedingHealed;

        //Debug.Log( $"healed {healAmount}, used {(int)math.ceil( healAmount )} charges" );
        
        item.CurrentCharges -= (int)math.ceil( healAmount );
        
        return new float2(healAmount, bleedingHealed);
    }
    */

    public HealResult Heal(ref HealthItemDesc item, int healCharges)
    {
        float healAmount = math.min(HealingNeeded, math.min(item.CurrentCharges, healCharges));
        HealingNeeded -= healAmount;
        
        float bleedingHealed = Bleed - (MaxBleed * (HealingNeeded/MaxHealing));
        Bleed -= bleedingHealed;

        //Debug.Log( $"healed {healAmount}, used {(int)math.ceil( healAmount )} charges" );
        int chargesUsed = (int) math.ceil( healAmount );
        item.CurrentCharges -= chargesUsed;
        
        return new HealResult(healAmount, bleedingHealed, chargesUsed);
    }
    
    
}

public struct HealResult
{
    public float AmountHealed;
    public float BleedingHealed;
    public int ChargesUsed;

    public HealResult( float amountHealed, float bleedingHealed, int chargesUsed )
    {
        AmountHealed = amountHealed;
        BleedingHealed = bleedingHealed;
        ChargesUsed = chargesUsed;
    }
    
}
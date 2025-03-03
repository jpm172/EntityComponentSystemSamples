using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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
    
    
}

using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[InternalBufferCapacity(6)]
public struct CharacterLimb : IBufferElementData
{
    public BodyPart Part;
    public float MaxHealth;
    public float CurrentHealth;
    public float Bleed;
    public float Condition;


    public float MissingHealth => GetMissingHealth();

    public bool Healthy => GetMissingHealth() > 0;
    
    public bool Destroyed => IsDestroyed();

    public CharacterLimb(BodyPart part, float maxHealth)
    {
        Part = part;
        Condition = 1;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        if ( part == BodyPart.Chest )
            CurrentHealth = 0;
        Bleed = 0;
    }


    public float Heal( float healAmount, float bleedHealAmount )
    {
        float clampedHeal = math.min( GetMissingHealth(), healAmount );
        CurrentHealth += clampedHeal;

        Bleed = math.max( 0, Bleed - bleedHealAmount );
        
        return clampedHeal;
    }
    
    public float Damage(CharacterWound wound)
    {
        float clampedDamage = 
            math.select( math.min( wound.HealingNeeded, CurrentHealth ), wound.HealingNeeded, Part == BodyPart.Chest );

        CurrentHealth -= clampedDamage;
        Bleed += wound.Bleed;

        return clampedDamage;
    }

    public float Damage( float damage )
    {
        float clampedDamage = math.select( math.min( damage, CurrentHealth ), damage, Part == BodyPart.Chest );

        CurrentHealth -= clampedDamage;

        return clampedDamage;
    }
    
    private float GetMissingHealth()
    {
        if(Part == BodyPart.Chest)
            return math.abs( CurrentHealth );
        
        return MaxHealth - CurrentHealth;
    }
    
    private bool IsDestroyed()
    {
        if ( Part == BodyPart.Chest )
            return false;
        
        return CurrentHealth <= 0;
    }
    
}


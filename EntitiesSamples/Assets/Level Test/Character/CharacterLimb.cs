using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(6)]
public struct CharacterLimb : IBufferElementData
{
    public BodyPart Part;
    public float MaxHealth;
    public float CurrentHealth;
    public float Bleed;

    public bool Destroyed => IsDestroyed();

    public CharacterLimb(BodyPart part, float maxHealth)
    {
        Part = part;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        Bleed = 0;
    }

    
    
    private bool IsDestroyed()
    {
        return CurrentHealth <= 0;
    }
    
}

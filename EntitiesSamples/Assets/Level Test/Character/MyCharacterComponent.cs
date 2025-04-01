using System;
using Unity.Entities;
using Unity.Mathematics;


[Serializable]
public struct MyCharacterComponent : IComponentData
{
    
    public float MovementSpeed;
    public float Health;
    public float MaxHealth;
    

}


public struct BaseStats : IComponentData
{
    public CharacterStats Stats;
}

public struct TotalStats : IComponentData
{
    public CharacterStats Stats;
}

public struct CharacterStats
{
    public float MoveSpeed;
    public float Health;
    public float MaxHealth;
    public float Armor;
}

[Serializable]
public struct MyCharacterControl : IComponentData
{
    public float3 MoveVector;
}

[Serializable]
public struct PlayerInputs : IComponentData
{
    public float2 MoveInput;
    public float3 AimPosition;
    
    
    
    public bool Shoot;
    public bool AltFire;
    public float Debug;
}


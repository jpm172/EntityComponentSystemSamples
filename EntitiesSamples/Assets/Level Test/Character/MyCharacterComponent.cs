using System;
using Unity.Entities;
using Unity.Mathematics;


[Serializable]
public struct MyCharacterComponent : IComponentData
{
    
    //public float MovementSpeed;
    
}

public struct CharacterStats : IComponentData
{
    public StatInfo BaseStats;
    public StatInfo TotalStats;
}

public struct StatInfo
{
    public float Health;
    public float MaxHealth;
    
    public float MoveSpeed;
    public float Armor;

    public CharacterLimb HeadStats;
    public CharacterLimb ChestStats;
    public CharacterLimb LeftArmStats;
    public CharacterLimb RightArmStats;
    public CharacterLimb LeftLegStats;
    public CharacterLimb RightLegStats;

    
    public CharacterLimb GetLimb( BodyPart limb )
    {
        switch ( limb )
        {
            case BodyPart.Head:
                return HeadStats;
            case BodyPart.Chest:
                return ChestStats;
            case BodyPart.LeftArm:
                return LeftArmStats;
            case BodyPart.RightArm:
                return RightArmStats;
            case BodyPart.LeftLeg:
                return LeftLegStats;
            case BodyPart.RightLeg:
                return RightLegStats;
            default:
                throw new ArgumentOutOfRangeException($"Invalid Limb: {(int)limb}");
        }
    }


    public void SetLimb( CharacterLimb limb )
    {
        switch ( limb.Part )
        {
            case BodyPart.Head:
                HeadStats = limb;
                break;
            case BodyPart.Chest:
                ChestStats = limb;
                break;
            case BodyPart.LeftArm:
                LeftArmStats = limb;
                break;
            case BodyPart.RightArm:
                RightArmStats = limb;
                break;
            case BodyPart.LeftLeg:
                LeftLegStats = limb;
                break;
            case BodyPart.RightLeg:
                RightLegStats = limb;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Invalid Limb: {(int)limb.Part}");
        }
    }
    
    public float ArmsCondition()
    {
        return ( LeftArmStats.Condition + RightArmStats.Condition ) / 2;
    }

    public float LegsCondition()
    {
        return ( LeftLegStats.Condition + RightLegStats.Condition ) / 2;
    }
    
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
    public bool Click;
    public bool Reload;
    public bool QuickSwitch;
    public float Debug;
}


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
    public float TimeSinceShot;
    public float RecoilTimer;

    public float TargetRecoilAngle;
    public float RecoilAngle;

    public float3 TargetRecoilValue;
    public float3 RecoilValue;
    public float3 RecoilOffset;
    
    
    public bool Shoot;
    public bool AltFire;
    public float Debug;
}


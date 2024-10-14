using System;
using Unity.Entities;
using Unity.Mathematics;


[Serializable]
public struct MyCharacterComponent : IComponentData
{
    public float MovementSpeed;

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
    public float3 Forward;
    public float3 Up;
}


using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ProjectileInfo : IComponentData
{
    public float3 Velocity;
    public float Z;

}

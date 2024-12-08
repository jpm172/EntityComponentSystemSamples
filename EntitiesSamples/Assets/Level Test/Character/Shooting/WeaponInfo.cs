using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct WeaponInfo : IComponentData
{
    public float DestroyRadius;
    public float FireRate;
    public float Penetration;
    public float PlayerDamage;
    public float StructureDamage;

}

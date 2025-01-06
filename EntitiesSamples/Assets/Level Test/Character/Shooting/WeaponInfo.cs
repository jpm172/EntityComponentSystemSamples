using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct WeaponInfo : IComponentData
{
    public WeaponType Type;
    public bool IsExplosion;
    public float ThrowForce;
    public float ExplosionRadius;
    public float WeaponSpread;
    public int BulletsPerShot;
    public float Range;
    public float FireRate;
    public float Timer;
    public float Penetration;
    public float PlayerDamage;
    public float StructureDamage;

}

public enum WeaponType : int
{
    Gun = 0,
    Throwable = 1
}

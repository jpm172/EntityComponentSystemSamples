using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct WeaponDesc : IComponentData
{
    public WeaponType Type;

    public bool Null;
    public bool IsExplosion;
    public float ThrowForce;
    public float ExplosionRadius;
    public float WeaponSpread;
    public int BulletsPerShot;
    
    public AmmoType AmmoType;
    public int MaxAmmo;
    public int CurrentAmmo;
    
    public RecoilProfile Recoil;
    public float Range;
    public float FireRate;
    public float Timer;
    public float Penetration;
    public float PlayerDamage;
    public float StructureDamage;
}

public enum WeaponType : ushort
{
    Gun = 0,
    Throwable = 1
}

public enum AmmoType : ushort
{
    Pistol,
    Rifle,
    Shotgun,
    Special
}



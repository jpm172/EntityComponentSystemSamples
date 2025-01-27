using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Weapon Item", order = 1)]
public class WeaponItemData : ItemData
{

    [Tooltip("Equip Time")]
    [Range(0, 30)]
    public float EquipTime;
    
    [Tooltip("Holster Time")]
    [Range(0, 30)]
    public float HolsterTime;
    
    [Tooltip("Ammo Capacity")]
    [Range(1, 1000)]
    public int MaxAmmo;
    
    [Tooltip("Bullets Per Shot")]
    [Range(1, 100)]
    public int BulletsPerShot;
    
    [Range(0.1f, 30)]
    [Tooltip("Fire Rate (Rounds/Second)")]
    
    public float FireRate;
    
    [Tooltip("Spread")]
    public float WeaponSpread;
    
    [Tooltip("Range")]
    [Range(0.1f, 1000)]
    public float Range;
    
    [Tooltip("Penetration")]
    public float Penetration;
    
}

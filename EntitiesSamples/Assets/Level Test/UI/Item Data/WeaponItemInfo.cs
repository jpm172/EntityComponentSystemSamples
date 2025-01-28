using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponItemInfo : ItemInfo
{
    public WeaponInfo Weapon;

    public WeaponItemInfo( WeaponItemData data )
    {
        Data = data;
        Weapon = ItemToWeapon( data );
    }
    
    private WeaponInfo ItemToWeapon(WeaponItemData data)
    {
        
        float fireRate = 1 / data.FireRate;
        WeaponInfo newWeapon = new WeaponInfo
        {
            Type = WeaponType.Gun,
            BulletsPerShot = data.BulletsPerShot,
            MaxAmmo = data.MaxAmmo,
            FireRate = fireRate,
            WeaponSpread = data.WeaponSpread,
            Penetration = data.Penetration,
            Range = data.Range,
        };
        
        return newWeapon;
    }
    
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponItemInfo : ItemInfo
{
    public WeaponDesc Weapon;

    public WeaponItemInfo( WeaponItemData data, int key )
    {
        Data = data;
        Key = key;
        Weapon = ItemToWeapon( data );
    }
    
    private WeaponDesc ItemToWeapon(WeaponItemData data)
    {
        
        float fireRate = 1 / data.FireRate;
        WeaponDesc newWeapon = new WeaponDesc
        {
            Type = WeaponType.Gun,
            AmmoType = data.AmmoType,
            ReloadProfile = data.ReloadProfile,
            BulletsPerShot = data.BulletsPerShot,
            MaxAmmo = data.MaxAmmo,
            FireRate = fireRate,
            WeaponSpread = data.WeaponSpread,
            Recoil = data.Recoil,
            Penetration = data.Penetration,
            Range = data.Range,
        };
        
        return newWeapon;
    }
    
}

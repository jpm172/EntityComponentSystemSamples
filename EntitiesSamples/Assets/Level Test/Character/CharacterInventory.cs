using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CharacterInventory : IComponentData
{

    public Entity EquippedItem;
    public EquippingData SwitchToItem;
    public EquippingData SwitchToBuffer;

    public int LastEquipIndex;
    
    public AmmoInfo Ammo;
    
    public float Timer;
    public float Remaining;

    public bool SwitchBack;

    public bool Switching => SwitchToItem != EquippingData.Null || SwitchToBuffer != EquippingData.Null;


    public bool IsSwitchingTo( Entity entity )
    {
        bool value = SwitchToItem != EquippingData.Null && SwitchToItem.SwitchTo == entity;
        value |= SwitchToBuffer != EquippingData.Null && SwitchToBuffer.SwitchTo == entity;
        
        return value;
    }

    public bool IsInPipeline( Entity entity )
    {
        return EquippedItem == entity || IsSwitchingTo( entity );
    }
}

public struct AmmoInfo
{
    public int MaxPistolAmmo;
    public int MaxRifleAmmo;
    public int MaxShotgunAmmo;
    public int MaxSpecialAmmo;
    
    public int CurrentPistolAmmo;
    public int CurrentRifleAmmo;
    public int CurrentShotgunAmmo;
    public int CurrentSpecialAmmo;

    public AmmoInfo(int maxPistol, int maxRifle, int maxShotgun, int maxSpecial)
    {
        CurrentPistolAmmo = MaxPistolAmmo = maxPistol;
        CurrentRifleAmmo = MaxRifleAmmo = maxRifle;
        CurrentShotgunAmmo = MaxShotgunAmmo = maxShotgun;
        CurrentSpecialAmmo = MaxSpecialAmmo = maxSpecial;
    }

    public int GetAmmo( AmmoType type )
    {
        switch ( type )
        {
            case AmmoType.Pistol:
                return CurrentPistolAmmo;
            case AmmoType.Rifle:
                return CurrentRifleAmmo;
            case AmmoType.Shotgun:
                return CurrentShotgunAmmo;
            case AmmoType.Special:
                return CurrentSpecialAmmo;
            default:
                throw new ArgumentOutOfRangeException($"Invalid Ammo type: {(ushort)type}");
        }
    }

    public void RemoveAmmo( AmmoType type, int amount )
    {
        switch ( type )
        {
            case AmmoType.Pistol:
                CurrentPistolAmmo -= amount;
                break;
            case AmmoType.Rifle:
                CurrentRifleAmmo -= amount;
                break;
            case AmmoType.Shotgun:
                CurrentShotgunAmmo -= amount;
                break;
            case AmmoType.Special:
                CurrentSpecialAmmo -= amount;
                break;
        }
    }
    
    public void AddAmmo( AmmoType type, int amount )
    {
        switch ( type )
        {
            case AmmoType.Pistol:
                CurrentPistolAmmo += amount;
                break;
            case AmmoType.Rifle:
                CurrentRifleAmmo += amount;
                break;
            case AmmoType.Shotgun:
                CurrentShotgunAmmo += amount;
                break;
            case AmmoType.Special:
                CurrentSpecialAmmo += amount;
                break;
        }
    }
    
}


public struct InventoryElement : IBufferElementData
{
    public Entity Item;
}

public struct EquippingData
{
    public Entity SwitchTo;

    private bool _null;

    public static EquippingData Null = new EquippingData
    {
        SwitchTo = Entity.Null,
        _null = true
    };

    public EquippingData( Entity switchTo )
    {
        SwitchTo = switchTo;
        _null = false;
    }
    

    public static bool operator==(EquippingData lhs, EquippingData rhs)
    {
        return lhs.SwitchTo == rhs.SwitchTo && lhs._null == rhs._null;
    }
    
    public static bool operator!=(EquippingData lhs, EquippingData rhs)
    {
        return !(lhs == rhs);
    }

}
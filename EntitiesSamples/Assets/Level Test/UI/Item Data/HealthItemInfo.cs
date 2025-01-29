using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HealthItemInfo : ItemInfo
{
    public HealthItemDesc HealthItem;

    public HealthItemInfo( HealthItemData data, int key )
    {
        Data = data;
        HealthItem = ItemToHealthDesc( data );
        Key = key;
    }
    
    private HealthItemDesc ItemToHealthDesc(HealthItemData data)
    {
        
        HealthItemDesc newWeapon = new HealthItemDesc
        {
            MaxCharges = data.MaxCharges
        };
        
        return newWeapon;
    }
    
}

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
        Quantity = 1;
    }
    
    
    
    private HealthItemDesc ItemToHealthDesc(HealthItemData data)
    {
        
        HealthItemDesc newHealthItem = new HealthItemDesc
        {
            MaxCharges = data.MaxCharges,
            HealRate = data.HealRate,
            Type = data.Type
        };
        
        return newHealthItem;
    }
    
}

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
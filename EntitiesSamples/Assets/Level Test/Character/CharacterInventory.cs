using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CharacterInventory : IComponentData
{

    public Entity EquippedItem;
    public Entity SwitchToItem;
    public Entity SwitchToBuffer;

    public float Timer;
    public float Remaining;

    public bool Switching => SwitchToItem != Entity.Null || SwitchToBuffer != Entity.Null;



}


public struct InventoryElement : IBufferElementData
{
    public Entity Item;
}

public struct EquippingData
{
    public Entity SwitchTo;

    private bool _null;

    public bool IsNull => _null;
    
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
        return lhs.SwitchTo == rhs.SwitchTo && lhs.IsNull == rhs.IsNull;
    }
    
    public static bool operator!=(EquippingData lhs, EquippingData rhs)
    {
        return !(lhs == rhs);
    }

}
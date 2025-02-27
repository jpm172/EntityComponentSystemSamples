using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CharacterInventory : IComponentData
{
    //public int Equipped;

    public Entity EquippedItem;

    //public WeaponDesc PrimaryWeapon;
    //public WeaponDesc SecondaryWeapon;
    

}


public struct InventoryElement : IBufferElementData
{
    public Entity Item;
}
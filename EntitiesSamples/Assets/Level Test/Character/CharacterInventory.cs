using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct CharacterInventory : IComponentData
{
    public int Equipped;
    //public WeaponInfo EquippedWeapon;
    public WeaponDesc PrimaryWeapon;
    public WeaponDesc SecondaryWeapon;
    

}

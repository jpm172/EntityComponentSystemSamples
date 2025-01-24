using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Weapon Item", order = 1)]
public class WeaponItemData : ItemData
{

    [Tooltip("Ammo Capacity")]
    public int MaxAmmo;
    
    
    
}

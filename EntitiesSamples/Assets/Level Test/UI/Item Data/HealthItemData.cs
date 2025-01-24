using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Health Item", order = 1)]
public class HealthItemData : ItemData
{

    [Tooltip("Durability")]
    [Range(1, 10000)]
    public int MaxDurability;
    
    
    
}

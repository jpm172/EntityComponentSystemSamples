using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Health Item", order = 1)]
public class HealthItemData : ItemData
{

    [Tooltip("Max Charges")]
    [Range(1, 10000)]
    public int MaxCharges;

    [Tooltip("Heal Rate")]
    [Range(0.1f, 1000)]
    public float HealRate;

    [Tooltip( "Health Item Type" )] 
    public HealthItemType Type;


}

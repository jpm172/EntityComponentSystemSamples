using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/New Item", order = 1)]
public class ItemData : ScriptableObject
{
    [Tooltip("Item Name")]
    public string ItemName;

    [Tooltip( "Item Sprite" )] 
    public Sprite ItemSprite;

}

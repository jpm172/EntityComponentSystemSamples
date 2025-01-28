using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainer : MonoBehaviour
{
    public ItemInfo Item;
    

    public ItemData Data
    {
        get => Item.Data;
        set => Item.Data = value;
    }

    public ItemType Type => Item.Data.ItemType;

}

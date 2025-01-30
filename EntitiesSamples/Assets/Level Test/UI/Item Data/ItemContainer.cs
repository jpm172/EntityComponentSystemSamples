using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainer : MonoBehaviour
{
    //public ItemInfo Item;
    public int ItemKey;
    public ItemInfo Item => GetItem();

    private ItemInfo GetItem()
    {
        return PlayerUIManager.Instance.AllItems[ItemKey];
    }
    
    public ItemData Data
    {
        get => Item.Data;
        set => Item.Data = value;
    }

    public ItemType Type => Item.Data.ItemType;

}

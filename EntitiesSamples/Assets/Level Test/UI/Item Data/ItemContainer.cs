using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainer : MonoBehaviour
{
    [SerializeField]
    private int _itemKey;
    public ItemInfo Item => GetItem();

    [SerializeField] private bool _hasItem;
    
    private ItemInfo GetItem()
    {
        if(_hasItem)
            return PlayerUIManager.Instance.AllItems[_itemKey];

        return null;
    }

    public int Key => _itemKey;
    
    public ItemData Data => Item.Data;

    public ItemType Type => Item.Data.ItemType;

    public bool HasItem => _hasItem;


    public void Set( ItemInfo newItem )
    {
        if ( newItem == null )
        {
            Clear();
            return;
        }
        
        _hasItem = true;
        _itemKey = newItem.Key;
    }

    public void Clear()
    {
        _hasItem = false;

    }
    
}

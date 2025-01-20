using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _invItemPrefab;
    
    
    [SerializeField]
    private List<ItemData> _items;

    private void Awake()
    {
        foreach ( ItemData data in _items )
        {
            LoadItem( data );
        }
    }

    private void LoadItem( ItemData data )
    {
        ItemInfo newItem = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity, transform ).GetComponent<ItemInfo>();
        InventoryItemLayout layout = newItem.GetComponent<InventoryItemLayout>();
        layout.MaxAmmo = 30;
        layout.CurrentAmmo = 20;
        newItem.Data = data;
    }
    
    public void AddItem(ItemInfo item)
    {
        ItemInfo newItem = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity, transform ).GetComponent<ItemInfo>();
        newItem.Data = item.Data;
    }
    
    
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{

    private List<ItemData> _items;

    private void Awake()
    {
        _items = new List<ItemData>();
    }

    public void AddItem(ItemInfo item)
    {
        
    }
    
}

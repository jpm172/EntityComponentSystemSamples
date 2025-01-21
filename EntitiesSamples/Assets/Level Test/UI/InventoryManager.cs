using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _invItemPrefab;

    [SerializeField]
    private RectTransform _itemLayer;

    [SerializeField]
    private TextMeshProUGUI _itemCounter;

    [SerializeField]
    private int _maxItems = 5;

    private int _itemCount;
    
    [SerializeField]
    private List<ItemData> _items;
    
   

    private void Awake()
    {
        _itemCount = _items.Count;
        foreach ( ItemData data in _items )
        {
            LoadItem( data );
        }
        UpdateItemCounter();
    }

    private void LoadItem( ItemData data )
    {
        ItemInfo newItem = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity, _itemLayer ).GetComponent<ItemInfo>();
        InventoryItemLayout layout = newItem.GetComponent<InventoryItemLayout>();
        layout.MaxAmmo = 30;
        layout.CurrentAmmo = 20;
        newItem.Data = data;

        DragObject drag = newItem.GetComponent<DragObject>();
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromObj = newItem;
    }
    
    public void AddItem(ItemInfo item)
    {
        ItemInfo newItem = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity,  _itemLayer ).GetComponent<ItemInfo>();
        InventoryItemLayout layout = newItem.GetComponent<InventoryItemLayout>();
        newItem.Data = item.Data;
        
        DragObject drag = newItem.GetComponent<DragObject>();
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromObj = newItem;
        
        _itemCount++;
        UpdateItemCounter();
    }
   

    public void RemovedItem()
    {
        _itemCount--;
        UpdateItemCounter();
    }
    
    public void UpdateItemCounter()
    {
        _itemCounter.text = $"{_itemCount}/{_maxItems}";
    }
    
}

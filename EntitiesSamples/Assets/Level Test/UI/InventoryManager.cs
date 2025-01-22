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
    private TextMeshProUGUI _collapseButton;
    
    [SerializeField]
    private int _maxItems = 5;

    private int _itemCount;
    
    [SerializeField]
    private List<ItemData> _items;

    private bool _collapsed;
   

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
        drag.SourceObject = gameObject;
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
        drag.SourceObject = gameObject;
        
        _itemCount++;
        UpdateItemCounter();
    }

    public void ReOrderItem( DragObject drag, Vector2 position )
    {
        for ( int i = 0; i < _itemLayer.transform.childCount; i++ )
        {
            Vector3 pos = _itemLayer.GetChild( i ).GetComponent<RectTransform>().position;
            if ( pos.y <= position.y )
            {
                int index = drag.TransferFromObj.transform.GetSiblingIndex();
                if ( index <= i )
                {
                    index = Math.Max( i - 1, 0 );
                }
                else
                {
                    index = i;
                }
                drag.TransferFromObj.transform.SetSiblingIndex( index );
                return;
            }
        }
        drag.TransferFromObj.transform.SetSiblingIndex( _itemLayer.transform.childCount );
    }


    public void CollapseExpandInventory()
    {
        _collapsed = !_collapsed;
        if ( _collapsed )
        {
            _collapseButton.text = "+";
            _itemLayer.localScale = Vector3.zero;
        }
        else
        {
            _collapseButton.text = "-";
            _itemLayer.localScale = Vector3.one;
        }

        
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

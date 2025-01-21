using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerDownHandler
{
    private static readonly Vector2 _emptySize = new Vector2( 40, 40 );

    [SerializeField]
    private Image _displayImage;

    private RectTransform _rect;
    
    private DragManager _dragManager;
    [SerializeField]
    private ItemInfo _heldItem;

    private bool _hasItem;

    public bool HasItem => _hasItem;

    public void Awake()
    {
        _hasItem = false;
        _dragManager = GetComponentInParent<DragManager>();
        _rect = _displayImage.GetComponent<RectTransform>();
        _heldItem = GetComponent<ItemInfo>();
    }

    public void AddItem( ItemInfo item )
    {

        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.SetNativeSize();

        _heldItem.Data = item.Data;
        _hasItem = true;
        
    }

    public void SwapItem(DragObject drag)
    {
        ItemData swap = _heldItem.Data;
        _heldItem.Data = drag.TransferFromObj.Data;
        drag.TransferFromObj.Data = swap;
        drag.SwapCallback();
        AddItem( _heldItem );
    }

    public void GetTransferItem()
    {
        if ( !_hasItem )
            return;
        
        DragObject transferItem = _dragManager.SpawnItem( _heldItem.Data, GetComponent<RectTransform>().position );
        transferItem.Callback = Callback;
        transferItem.TransferFromObj = _heldItem;
    }

    public void RemoveItem()
    {
        _displayImage.sprite = null;
        _rect.sizeDelta = _emptySize;
        _hasItem = false;
    }
    

    public void OnPointerDown( PointerEventData eventData )
    {
        GetTransferItem();
    }

    public void Callback()
    {
       RemoveItem();
    }
}

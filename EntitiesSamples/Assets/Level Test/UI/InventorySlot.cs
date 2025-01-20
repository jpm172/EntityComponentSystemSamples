using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerDownHandler, IInventory
{
    private static readonly Vector2 _emptySize = new Vector2( 40, 40 );

    [SerializeField]
    private Image _displayImage;

    private RectTransform _rect;
    
    private DragManager _dragManager;
    [SerializeField]
    private ItemData _heldItem;

    private bool _hasItem;
    
    public void Awake()
    {
        _hasItem = false;
        _dragManager = GetComponentInParent<DragManager>();
        _rect = _displayImage.GetComponent<RectTransform>();
    }

    public void AddItem( ItemInfo item )
    {
        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.SetNativeSize();

        _heldItem = item.Data;
        _hasItem = true;
        
    }

    public void GetItem()
    {
        if ( !_hasItem )
            return;
        
        DragObject transferItem = _dragManager.SpawnItem( _heldItem, GetComponent<RectTransform>().position );
        _displayImage.sprite = null;
        _rect.sizeDelta = _emptySize;
        _hasItem = false;
    }


    public void Callback(ItemInfo returnedItem)
    {
        AddItem( returnedItem );
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        GetItem();
    }

    public void Callback()
    {
        Debug.Log( "callback on slot" );
    }
}

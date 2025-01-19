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
        Vector2 size = item.Data.ItemSprite.rect.size;

        _rect.sizeDelta = size;
        _heldItem = item.Data;
        _hasItem = true;
        
    }

    public void GetItem()
    {
        if ( !_hasItem )
            return;
        
        _dragManager.SpawnItem( _heldItem, GetComponent<RectTransform>().position );
        _displayImage.sprite = null;
        _rect.sizeDelta = _emptySize;
        _hasItem = false;
    }


    public void OnPointerDown( PointerEventData eventData )
    {
        GetItem();
    }
}

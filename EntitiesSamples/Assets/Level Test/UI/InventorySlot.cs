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

    [SerializeField]
    private float _padding = 10;
    
    [SerializeField] 
    private RectTransform _containerRect;

    private DragManager _dragManager;
    [SerializeField]
    private ItemInfo _heldItem;

    [SerializeField]
    private ItemType _slotItemType;
    
    private bool _hasItem;

    public bool HasItem => _hasItem;

    public void Awake()
    {
        _hasItem = false;
        _dragManager = GetComponentInParent<DragManager>();
        _heldItem = GetComponent<ItemInfo>();
        _displayImage.gameObject.SetActive( false );
    }

    public void AddItem( ItemInfo item )
    {
        _displayImage.gameObject.SetActive( true );
        
        Vector2 spriteSize = item.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;

        _heldItem.Data = item.Data;
        _hasItem = true;
        
    }

    private void UpdateItem()
    {
        Vector2 spriteSize = _heldItem.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = _heldItem.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
    }
    

    public void SwapItem(DragObject drag)
    {
        ItemData swap = _heldItem.Data;
        _heldItem.Data = drag.TransferFromObj.Data;
        drag.TransferFromObj.Data = swap;
        drag.SwapCallback();
        AddItem( _heldItem );
    }

    public bool IsMatchingItemType(ItemInfo info)
    {
        return info.Data.ItemType == _slotItemType;
    }

    public void GetTransferItem()
    {
        if ( !_hasItem )
            return;
        
        DragObject transferItem = _dragManager.SpawnItem( _heldItem.Data, GetComponent<RectTransform>().position );
        transferItem.Callback = Callback;
        transferItem.SwapCallback = SwapCallback;
        transferItem.TransferFromObj = _heldItem;
        transferItem.SourceObject = gameObject;
    }

    public void RemoveItem()
    {
        _displayImage.sprite = null;
        _displayImage.gameObject.SetActive( false );
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

    public void SwapCallback()
    {
        UpdateItem();
    }
}

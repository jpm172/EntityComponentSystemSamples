using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private Image _displayImage;

    [SerializeField]
    private Image _transferingImage;
    
    [SerializeField]
    private float _padding = 10;
    
    [SerializeField] 
    private RectTransform _containerRect;

    [SerializeField]
    private HotbarManager _hotBar;

    private DragManager _dragManager;
    [SerializeField]
    private ItemContainer _container;

    [SerializeField]
    private ItemType _slotItemType;

    [SerializeField] 
    private InventorySlotType _equipType;
    

    public bool HasItem => _container.HasItem;

    public void Awake()
    {
        _dragManager = GetComponentInParent<DragManager>();
        _container = GetComponent<ItemContainer>();
        _displayImage.gameObject.SetActive( false );
    }

    public void AddItem( ItemContainer item )
    {
        _displayImage.gameObject.SetActive( true );
        
        Vector2 spriteSize = item.Data.ItemSprite.textureRect.size;
        var rect = _containerRect.rect;
        float xScale = rect.width  / (spriteSize.x+ _padding*2);
        float yScale = rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
        
        _container.Set( item.Item );
        //_container.ItemKey = item.ItemKey;
        //_hasItem = true;

        int slotIndex = ( _equipType == InventorySlotType.Primary ) ? 0 : 1;
        _hotBar.AddToHotBar( item, slotIndex );
    }

    public void AddItemFromHotBar(ItemInfo item)
    {
        _displayImage.gameObject.SetActive( true );
        
        Vector2 spriteSize = item.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = item.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
        
        _container.Set( item );
        //_container.ItemKey = item.Key;
        //_hasItem = true;
    }

    private void UpdateItem()
    {
        Vector2 spriteSize = _container.Data.ItemSprite.textureRect.size;
        float xScale = _containerRect.rect.width  / (spriteSize.x+ _padding*2);
        float yScale = _containerRect.rect.height / (spriteSize.y+ _padding*2);
        float scale = Math.Min( xScale, yScale );
        //Debug.Log( xScale + ", " + yScale + " == " + scale );
        
        _displayImage.sprite = _container.Data.ItemSprite;
        _displayImage.rectTransform.sizeDelta = spriteSize * scale;
        
        //int slotIndex = ( _equipType == InventorySlotType.Primary ) ? 0 : 1;
        //_hotBar.AddToHotBar( _container, slotIndex );
        
    }
    

    public void SwapItem(DragObject drag)
    {
        
        //int swap = _container.ItemKey;
        //_container.ItemKey = drag.TransferFromContainer.ItemKey;
        ItemInfo swap = _container.Item;
        _container.Set( drag.TransferFromContainer.Item );

        //drag.TransferFromContainer.ItemKey = swap;
        drag.TransferFromContainer.Set( swap );
        drag.SwapCallback();
        AddItem( _container );
    }

    public bool MatchesType(ItemContainer container)
    {
        return container.Type == _slotItemType;
    }

    public void GetTransferItem()
    {
        if ( !HasItem )
            return;
        
        DragObject transferItem = _dragManager.SpawnItem( _container.Item, GetComponent<RectTransform>().position );
        transferItem.Callback = Callback;
        transferItem.SwapCallback = SwapCallback;
        transferItem.TransferFromContainer = _container;
        transferItem.SourceObject = gameObject;
    }

    public void RemoveItem()
    {
        _displayImage.sprite = null;
        _displayImage.gameObject.SetActive( false );
        //_hasItem = false;
        _container.Clear();
        
        int slotIndex = ( _equipType == InventorySlotType.Primary ) ? 0 : 1;
        _hotBar.RemoveFromHotBar( slotIndex );
        
    }

    public void RemoveItemFromHotBar()
    {
        _displayImage.sprite = null;
        _displayImage.gameObject.SetActive( false );
        _container.Clear();
        //_hasItem = false;
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

public enum InventorySlotType : int
{
    Primary = 0,
    Secondary = 1,
    Armor = 2,
    Helmet = 3,
}

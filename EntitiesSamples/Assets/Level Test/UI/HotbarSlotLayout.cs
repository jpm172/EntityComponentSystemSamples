using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlotLayout : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private int _slotIndex;

    [SerializeField]
    private TextMeshProUGUI _itemNameText;

    private AspectRatioFitter _imageFitter;
    
    [SerializeField]
    private Image _itemImage;

    [SerializeField]
    private ItemInfo _heldItem;

    private bool _hasItem;
    private bool _equipped;

    public bool HasItem => _hasItem;

    public bool Equipped
    {
        get => _equipped;
        set => _equipped = value;
    }

    public int SlotIndex => _slotIndex;
    
    public ItemInfo HeldItem => _heldItem;

    private void Awake()
    {
        _imageFitter = GetComponentInChildren<AspectRatioFitter>();
        _slotIndex = transform.GetSiblingIndex();
        
        
        if ( !_hasItem )
        {
            ClearSlot(false);
        }
    }

    public void SwapWith( HotbarSlotLayout otherSlot )
    {
        ItemInfo swapItem = _heldItem;
        
        _itemImage.enabled = true;
        _heldItem = otherSlot.HeldItem;
        _imageFitter.aspectRatio = _heldItem.Data.ItemSprite.textureRect.size.x / _heldItem.Data.ItemSprite.textureRect.size.y;
        _itemImage.sprite = _heldItem.Data.ItemSprite;
        _itemNameText.text = _heldItem.Data.ItemName;
        
        if ( _hasItem )
        {
            otherSlot.ReplaceSlot( swapItem );
        }
        else
        {
            otherSlot.ClearSlot(false);
        }
        _hasItem = true;
    }
    
    public bool TryPutInSlot(ItemContainer item)
    {
        if ( !CanPutInSlot( item ) )
            return false;
        
        _itemImage.enabled = true;
        _heldItem = item.Item;
        _imageFitter.aspectRatio = _heldItem.Data.ItemSprite.textureRect.size.x / _heldItem.Data.ItemSprite.textureRect.size.y;

        _itemImage.sprite = _heldItem.Data.ItemSprite;
        _itemNameText.text = _heldItem.Data.ItemName;
        
        PlayerUIManager.Instance.AddItemEntity( item.Item, _slotIndex, _equipped );
        
        _hasItem = true;
        return true;
    }

    public void ReplaceSlot(ItemInfo item)
    {
        _heldItem = item;
        _imageFitter.aspectRatio = _heldItem.Data.ItemSprite.textureRect.size.x / _heldItem.Data.ItemSprite.textureRect.size.y;

        _itemImage.sprite = _heldItem.Data.ItemSprite;
        _itemNameText.text = _heldItem.Data.ItemName;
    }
    
    
    public bool CanPutInSlot( ItemContainer item )
    {
        ItemData data = item.Data;

        if ( data.ItemType == ItemType.Helmet || data.ItemType == ItemType.Armor )
            return false;

        if ( data.ItemType == ItemType.Weapon && _slotIndex > 1 )
            return false;

        if ( data.ItemType == ItemType.Health && _slotIndex <= 1 )
            return false;
        
        return true;
    }

    public void ClearSlot(bool deleteEntity)
    {
        if ( _hasItem && deleteEntity )
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        _itemNameText.text = "";
        _itemImage.enabled = false;
        _heldItem = null;
        _hasItem = false;
    }
    

    public void OnPointerDown( PointerEventData eventData )
    {

        if ( eventData.clickCount == 1 )
        {
            ClearSlot(true);
        }
        
            
        
    }
}

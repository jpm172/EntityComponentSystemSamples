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
    private int _slotNumber;

    [SerializeField]
    private TextMeshProUGUI _itemNameText;

    private AspectRatioFitter _imageFitter;
    
    [SerializeField]
    private Image _itemImage;

    [SerializeField]
    private ItemInfo _heldItem;

    private bool _hasItem;

    public bool HasItem => _hasItem;

    public ItemInfo HeldItem => _heldItem;

    private void Awake()
    {
        _imageFitter = GetComponentInChildren<AspectRatioFitter>();
        _slotNumber = transform.GetSiblingIndex() + 1;
        
        
        if ( !_hasItem )
        {
            ClearSlot();
        }
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
        
        _hasItem = true;
        return true;
    }
    
    private bool CanPutInSlot( ItemContainer item )
    {
        ItemData data = item.Data;

        if ( data.ItemType == ItemType.Helmet || data.ItemType == ItemType.Armor )
            return false;

        if ( data.ItemType == ItemType.Weapon && _slotNumber > 2 )
            return false;

        if ( data.ItemType == ItemType.Health && _slotNumber <= 2 )
            return false;
        
        return true;
    }

    public void ClearSlot()
    {
        _itemNameText.text = "";
        _itemImage.enabled = false;
        _heldItem = null;
        _hasItem = false;
    }

    public void OnPointerDown( PointerEventData eventData )
    {

        if ( eventData.clickCount == 1 )
        {
            ClearSlot();
        }
        
            
        
    }
}

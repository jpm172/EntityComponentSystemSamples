using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthItemLayout : InventoryItemLayout
{

    private int _maxDurability;
    private int _currentDurability;

    [SerializeField]
    private Image _durabilityMeter;
    
    
    protected override void Initialize()
    {
        _item = GetComponent<ItemInfo>();
        
        _maxDurability = ( (HealthItemData) _item.Data ).MaxDurability;
        _currentDurability = UnityEngine.Random.Range( 0, _maxDurability + 1 );
        
        float durValue =  (float) _currentDurability / _maxDurability ;
        _durabilityMeter.fillAmount = durValue;
        
        //set the text and change rect to match its size
        _itemText.SetText( _item.Data.ItemName );

        //update weapon sprite and move it into place
        _itemImage.sprite = _item.Data.ItemSprite;
        /*
        Vector2 spriteSize = _item.Data.ItemSprite.textureRect.size;
        spriteSize *= ( _itemImage.rectTransform.rect.height / spriteSize.y );
        _itemImage.rectTransform.sizeDelta = spriteSize;
        _itemImage.rectTransform.anchoredPosition = new Vector2(finalSize.x + _padding, 0);
        */
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthItemLayout : InventoryItemLayout
{

    private int _maxCharges;
    private int _currentCharges;

    [SerializeField]
    private Image _chargeMeter;
    
    
    protected override void Initialize()
    {
        _item = GetComponent<ItemContainer>();
        
        _maxCharges = ( (HealthItemInfo) _item.Item ).HealthItem.MaxCharges;
        _currentCharges = ( (HealthItemInfo) _item.Item ).HealthItem.CurrentCharges;
        
        float durValue =  (float) _currentCharges / _maxCharges ;
        _chargeMeter.fillAmount = durValue;
        
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

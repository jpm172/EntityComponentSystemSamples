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
        Vector2 textPref = _itemText.GetPreferredValues();

        _itemText.rectTransform.sizeDelta = textPref;
        _itemText.rectTransform.anchoredPosition = new Vector2(_padding, 0);
        
        Vector2 finalSize = textPref + new Vector2(_padding, 0);

        //update weapon sprite and move it into place
        _itemImage.sprite = _item.Data.ItemSprite;
        Vector2 spriteSize = _item.Data.ItemSprite.textureRect.size;
        spriteSize *= ( _itemImage.rectTransform.rect.height / spriteSize.y );
        _itemImage.rectTransform.sizeDelta = spriteSize;
        _itemImage.rectTransform.anchoredPosition = new Vector2(finalSize.x + _padding, 0);
        
        //fit the container to hold the weapon sprite
        Vector2 weaponSize = _itemImage.rectTransform.sizeDelta * _itemImage.rectTransform.localScale;
        finalSize.x += weaponSize.x + _padding * 2;
        finalSize.y = Math.Max( weaponSize.y + _padding*2, 36 );

        GetComponent<RectTransform>().sizeDelta = finalSize;
    }
}

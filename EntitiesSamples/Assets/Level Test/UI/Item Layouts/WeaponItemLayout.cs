using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponItemLayout : InventoryItemLayout
{

    private int _currentAmmo;
    private int _maxAmmo;
    
    [SerializeField]
    private Image _ammoCounter;
    protected override void Initialize()
    {
        _item = GetComponent<ItemContainer>();
        //_maxAmmo = ( (WeaponItemData) _item.Data ).MaxAmmo;
        _maxAmmo = ( (WeaponItemInfo) _item.Item ).Weapon.MaxAmmo;
        //_currentAmmo = UnityEngine.Random.Range( 0, _maxAmmo + 1 );
        _currentAmmo = ( (WeaponItemInfo) _item.Item ).Weapon.CurrentAmmo;
        
        //set the text and change rect to match its size
        _itemText.SetText( _item.Data.ItemName );
        Vector2 textPref = _itemText.GetPreferredValues();

        _itemText.rectTransform.sizeDelta = textPref;
        _itemText.rectTransform.anchoredPosition = new Vector2(_padding, 0);
        
        Vector2 finalSize = textPref + new Vector2(_padding, 0);
        
        //set ammo counter to roughly match how much ammo is left
        float ammoValue =  (float) _currentAmmo / _maxAmmo ; 
        ammoValue = Mathf.Round( ammoValue * 10.0f) * 0.1f;
        _ammoCounter.fillAmount = ammoValue;

        //size the container to fit the ammo counter and weapon name
        finalSize.x = Math.Max( _ammoCounter.rectTransform.sizeDelta.x + _padding, finalSize.x );
        
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

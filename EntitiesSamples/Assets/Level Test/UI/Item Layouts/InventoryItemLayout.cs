using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

public class InventoryItemLayout : MonoBehaviour
{
    [SerializeField]
    protected float _padding = 10;

    protected ItemInfo _item;

    [SerializeField]
    protected TextMeshProUGUI _itemText;
    [SerializeField]
    protected Image _itemImage;

    private void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        /*
        _item = GetComponent<ItemInfo>();
        
        //set the text and change rect to match its size
        _itemText.SetText( _item.Data.ItemName );
        Vector2 textPref = _itemText.GetPreferredValues();

        _itemText.rectTransform.sizeDelta = textPref;
        _itemText.rectTransform.anchoredPosition = new Vector2(_padding, 0);
        
        Vector2 finalSize = textPref + new Vector2(_padding, 0);

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
        */
    }

    public void CallBack()
    {
        GetComponentInParent<InventoryManager>().RemovedItem();
        Destroy( gameObject );
    }

    public void SwapCallback()
    {
        Initialize();
    }
    
}

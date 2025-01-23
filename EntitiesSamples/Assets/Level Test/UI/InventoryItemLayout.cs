using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemLayout : MonoBehaviour
{
    [SerializeField]
    private float _padding = 10;

    private ItemInfo _item;

    [SerializeField]
    private int _maxAmmo;
    [SerializeField]
    private int _currentAmmo;

    [SerializeField]
    private TextMeshProUGUI _weaponText;
    [SerializeField]
    private Image _weaponImage;
    [SerializeField]
    private Image _ammoCounter;

    public int MaxAmmo
    {
        get => _maxAmmo;
        set => _maxAmmo = value;
    }

    public int CurrentAmmo
    {
        get => _currentAmmo;
        set => _currentAmmo = value;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        _item = GetComponent<ItemInfo>();

        //set the text and change rect to match its size
        _weaponText.SetText( _item.Data.ItemName );
        Vector2 textPref = _weaponText.GetPreferredValues();

        _weaponText.rectTransform.sizeDelta = textPref;
        _weaponText.rectTransform.anchoredPosition = new Vector2(_padding, 0);
        
        Vector2 finalSize = textPref + new Vector2(_padding, 0);
        
        //set ammo counter to roughly match how much ammo is left
        float ammoValue =  (float) _currentAmmo / _maxAmmo ; 
        ammoValue = Mathf.Round( ammoValue * 10.0f) * 0.1f;
        _ammoCounter.fillAmount = ammoValue;
        
        //size the container to fit the ammo counter and weapon name
        finalSize.x = Math.Max( _ammoCounter.rectTransform.sizeDelta.x + _padding, finalSize.x );
        
        //update weapon sprite and move it into place
        _weaponImage.sprite = _item.Data.ItemSprite;
        Vector2 spriteSize = _item.Data.ItemSprite.textureRect.size;
        spriteSize *= ( _weaponImage.rectTransform.rect.height / spriteSize.y );
        _weaponImage.rectTransform.sizeDelta = spriteSize;
        _weaponImage.rectTransform.anchoredPosition = new Vector2(finalSize.x + _padding, 0);
        
        //fit the container to hold the weapon sprite
        Vector2 weaponSize = _weaponImage.rectTransform.sizeDelta * _weaponImage.rectTransform.localScale;
        finalSize.x += weaponSize.x + _padding * 2;
        finalSize.y = Math.Max( weaponSize.y + _padding*2, 36 );

        GetComponent<RectTransform>().sizeDelta = finalSize;
    }

    public void CallBack()
    {
        GetComponentInParent<InventoryManager>().RemovedItem();;
        Destroy( gameObject );
    }

    public void SwapCallback()
    {
        Initialize();
    }
    
}

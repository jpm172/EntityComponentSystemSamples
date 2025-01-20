using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InvetoryItemLayout : MonoBehaviour
{
    [SerializeField]
    private float _padding = 10;
    [SerializeField]
    private ItemData _item;

    [SerializeField]
    private float _maxAmmo;
    [SerializeField]
    private float _currentAmmo;

    [SerializeField]
    private TextMeshProUGUI _weaponText;
    [SerializeField]
    private Image _weaponImage;
    [SerializeField]
    private Image _ammoCounter;
    
    
        
    private void Awake()
    {
        
        //set the text and change rect to match its size
        _weaponText.SetText( _item.ItemName );
        Vector2 textPref = _weaponText.GetPreferredValues();
        _weaponText.rectTransform.sizeDelta = textPref;
        _weaponText.rectTransform.anchoredPosition = new Vector2(_padding, 0);

        Vector2 finalSize = textPref + new Vector2(_padding, 0);
        
        //set ammo counter to roughly match how much ammo is left
        float ammoValue = Mathf.Round((_currentAmmo / _maxAmmo)  * 10.0f) * 0.1f;
        _ammoCounter.fillAmount = ammoValue;
        
        finalSize.x = Math.Max( _ammoCounter.rectTransform.sizeDelta.x + _padding, finalSize.x );
        
        
        _weaponImage.sprite = _item.ItemSprite;
        _weaponImage.SetNativeSize();

        _weaponImage.rectTransform.anchoredPosition = new Vector2(finalSize.x + _padding, 0);
        
        Vector2 weaponSize = _weaponImage.rectTransform.sizeDelta * _weaponImage.rectTransform.localScale;
        finalSize.x += weaponSize.x + _padding * 2;
        finalSize.y = Math.Max( weaponSize.y + _padding*2, 36 );

        GetComponent<RectTransform>().sizeDelta = finalSize;

    }
}

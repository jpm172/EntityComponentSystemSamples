using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkbenchItemLayout : MonoBehaviour
{
    private ItemContainer _container;

    [SerializeField]
    protected TextMeshProUGUI _itemNameText;

    [SerializeField]
    protected Image _itemImage;
    
    private AspectRatioFitter _imageFitter;
    
    
    
    private void Awake()
    {
        _container = GetComponent<ItemContainer>();
        _imageFitter = _itemImage.GetComponent<AspectRatioFitter>();
        
        if(!_container.HasItem)
            ClearItem();
        
    }

    void Start()
    {
        
    }


    public void SetItem( ItemInfo item )
    {
        _container.Set( item );
        //_container.ItemKey = item.Key;
        _itemImage.enabled = true;
        _imageFitter.aspectRatio = _container.Data.ItemSprite.textureRect.size.x / _container.Data.ItemSprite.textureRect.size.y;
        _itemImage.sprite = _container.Data.ItemSprite;
        _itemNameText.text = _container.Data.ItemName;
    }

    public void ClearItem()
    {
        _container.Clear();
        _itemImage.enabled = false;
        _itemNameText.text = String.Empty;
    }

}

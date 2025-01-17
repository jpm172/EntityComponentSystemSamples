using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField]
    private Image displayImage;

    private DragManager _dragManager;
    [SerializeField]
    private ItemInfo _heldItem;

    private bool _hasItem;
    
    public void Awake()
    {
        _hasItem = false;
        _dragManager = GetComponentInParent<DragManager>();
        _heldItem = GetComponent<ItemInfo>();
    }

    public void AddItem( ItemInfo item )
    {
        displayImage.sprite = item.ItemSprite;
        _heldItem.ItemSprite = item.ItemSprite;
        _hasItem = true;
        
    }

    public void GetItem()
    {
        Debug.Log( "pressed" );
        if ( !_hasItem )
            return;
        _dragManager.SpawnItem( _heldItem );
        displayImage.sprite = null;
        _hasItem = false;
        

    }
    
    
}

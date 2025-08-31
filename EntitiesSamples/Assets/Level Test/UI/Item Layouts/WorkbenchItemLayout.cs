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

    [SerializeField]
    private List<AttachmentSlotLayout> _attachmentSlots;

    public void Initialize()
    {
        _container = GetComponent<ItemContainer>();
        _imageFitter = _itemImage.GetComponent<AspectRatioFitter>();

        foreach ( AttachmentSlotLayout slot in _attachmentSlots )
        {
           slot.Initialize(); 
        }
        
        if(!_container.HasItem)
            ClearItem();
    }

    public void SetItem( ItemInfo item )
    {
        _container.Set( item );
        //_container.ItemKey = item.Key;
        _itemImage.enabled = true;
        _imageFitter.aspectRatio = _container.Data.ItemSprite.textureRect.size.x / _container.Data.ItemSprite.textureRect.size.y;
        _itemImage.sprite = _container.Data.ItemSprite;
        _itemNameText.text = _container.Data.ItemName;
        
        ShowAttachmentSlots(item);
    }

    private void ShowAttachmentSlots(ItemInfo item)
    {

        WeaponItemData weaponData = (WeaponItemData)item.Data;

        List<AttachmentSlot> weaponAttachments = weaponData.AttachmentSlots;
        
        foreach ( AttachmentSlotLayout slot in _attachmentSlots )
        {
            if ( !weaponAttachments.Contains( slot.SlotType ) )
            {
                slot.gameObject.SetActive( false );
            }
            else
            {    
                slot.ClearSlot();
                slot.gameObject.SetActive( true );
            }
        }

    }

    public void ClearItem()
    {
        _container.Clear();
        _itemImage.enabled = false;
        _itemNameText.text = String.Empty;
    }

}

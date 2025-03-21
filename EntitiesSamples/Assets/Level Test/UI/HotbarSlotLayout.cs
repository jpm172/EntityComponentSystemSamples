using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlotLayout : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private int _slotIndex;

    [SerializeField]
    protected TextMeshProUGUI _itemNameText;

    protected AspectRatioFitter _imageFitter;
    
    [SerializeField]
    protected Image _itemImage;

    [SerializeField]
    protected ItemContainer _container;

    protected DragManager _dragManager;
    
    private bool _equipped;

    public bool HasItem => _container.HasItem;

    public bool Equipped
    {
        get => _equipped;
        set => _equipped = value;
    }

    public int SlotIndex => _slotIndex;

    protected virtual bool BlockAdd => true;
    
    public ItemInfo HeldItem => _container.Item;

    private void Awake()
    {
        _container = GetComponent<ItemContainer>();
        _imageFitter = GetComponentInChildren<AspectRatioFitter>();
        _slotIndex = transform.GetSiblingIndex();
        
        if ( !HasItem )
        {
            ClearSlot(false);
        }
    }

    private void Start()
    {
        _dragManager = PlayerUIManager.Instance.gameObject.GetComponent<DragManager>();
    }

    public virtual void SwapWith( HotbarSlotLayout otherSlot )
    {
        ItemInfo swapItem = _container.Item;
        UpdateLayout( otherSlot.HeldItem );

        otherSlot.ReplaceSlot( swapItem );
        //_hasItem = true;
    }
    
    
    public virtual void TryPutInSlot(DragObject drag)
    {
        if ( !CanPutInSlot( drag.Container ) )
            return;

        ItemInfo item = drag.Container.Item;
        
        if(HasItem)
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        UpdateLayout( item );

        PlayerUIManager.Instance.AddItemEntity( item, _slotIndex, _equipped );
        
        //_hasItem = true;
    }
    
    public void TryPutInSlot(ItemContainer item)
    {
        if ( !CanPutInSlot( item ) )
            return;
        
        if(HasItem)
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        UpdateLayout(item.Item);
        
        PlayerUIManager.Instance.AddItemEntity( item.Item, _slotIndex, _equipped );
        
        //_hasItem = true;
    }
    
    

    protected virtual void ReplaceSlot(ItemInfo item)
    {
        if ( item == null )
        {
            ClearSlot(false);
            return;
        }
        
        UpdateLayout(item);
    }

    protected void UpdateLayout(ItemInfo item)
    {
        _container.Set( item );
        //_container.ItemKey = item.Key;
        _itemImage.enabled = true;
        _imageFitter.aspectRatio = _container.Data.ItemSprite.textureRect.size.x / _container.Data.ItemSprite.textureRect.size.y;
        _itemImage.sprite = _container.Data.ItemSprite;
        _itemNameText.text = _container.Data.ItemName;
    }
    
    
    public bool CanPutInSlot( ItemContainer item )
    {
        ItemData data = item.Data;

        if ( data.ItemType == ItemType.Helmet || data.ItemType == ItemType.Armor )
            return false;

        if ( data.ItemType == ItemType.Weapon && _slotIndex > 1 )
            return false;

        if ( data.ItemType == ItemType.Health && _slotIndex <= 1 )
            return false;
        
        return true;
    }

    public virtual void ClearSlot(bool deleteEntity)
    {
        if ( HasItem && deleteEntity )
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        _itemNameText.text = "";
        _itemImage.enabled = false;
        _container.Clear();
    }

    public void ClearFromLinkedSlot()
    {
        if(HasItem)
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        _itemNameText.text = "";
        _itemImage.enabled = false;
        _container.Clear();
    }

    protected virtual void DoubleClickClear()
    {
        if(HasItem)
            PlayerUIManager.Instance.RemoveItemEntity( _slotIndex, _equipped );
        
        _itemNameText.text = "";
        _itemImage.enabled = false;
        _container.Clear();
    }

    public void OnPointerDown( PointerEventData eventData )
    {
        if ( eventData.clickCount == 1 )
        {
            DoubleClickClear();
            //ClearSlot(true);
            return;
        }

        if ( !HasItem )
            return;

        if ( eventData.button == PointerEventData.InputButton.Right )
        {
            //todo: options menu for hotbar
            return;
        }
        
        DragObject transfer = _dragManager.SpawnItem( _container.Item, GetComponent<RectTransform>().position );
        transfer.TransferFromContainer = _container;
        transfer.SourceObject = gameObject;
        transfer.Callback = Callback;
        transfer.BlockAdd = BlockAdd;
    }

    private void Callback()
    {
        ClearSlot( true );
        //ClearFromLinkedSlot();
    }
    
}

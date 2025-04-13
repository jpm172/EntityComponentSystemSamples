using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    
    private const float _collapsedHeight = 30;
    
    private float _spacing;

    private PlayerUIManager _manager;
    
    [SerializeField]
    private GameObject _invItemPrefab;

    [SerializeField]
    private RectTransform _itemLayer;

    [SerializeField]
    private TextMeshProUGUI _itemCounter;

    [SerializeField]
    private TextMeshProUGUI _collapseButton;

    [SerializeField] 
    private bool _hasCapacity;

    [SerializeField]
    private int _maxItems = 5;

    private int _itemCount;
    
    [SerializeField]
    private List<ItemType> _itemTypeWhitelist;
    
    //[SerializeField]
    //private List<ItemData> _items;

    private bool _collapsed;

    private RectTransform _rectTransform;
    

    public void Initialize()
    {
        _manager = GetComponentInParent<PlayerUIManager>();
        _rectTransform = GetComponent<RectTransform>();
        
        _spacing = _itemLayer.GetComponent<VerticalLayoutGroup>().spacing;
        
        LoadItems();
        UpdateInventoryLayout();
    }

    private void LoadItems()
    {
        foreach ( ItemType type in _itemTypeWhitelist )
        {
            if ( type == ItemType.Weapon )
            {
                _itemCount = _manager.WeaponItems.Count;
                foreach ( WeaponItemInfo item in _manager.WeaponItems.Values )
                {
                    LoadItem( item );
                }
            }
            else if ( type == ItemType.Health )
            {
                _itemCount = _manager.HealthItemKeys.Count;
                foreach ( int itemKey in _manager.HealthItemKeys )
                {
                    LoadItem( _manager.AllItems[itemKey] );
                }
            }
        }
    }
    

    private void LoadItem( ItemInfo item )
    {
        ItemContainer newContainer = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity, _itemLayer ).GetComponent<ItemContainer>();
        InventoryItemLayout layout = newContainer.GetComponent<InventoryItemLayout>();
        
        
        //newContainer.ItemKey = data.Key;
        newContainer.Set( item );
        newContainer.transform.SetAsFirstSibling();
        newContainer.transform.SetSiblingIndex( item.Order );
        layout.Initialize();

        DragObject drag = newContainer.GetComponent<DragObject>();
        ConnectDragObject( drag, layout, newContainer );

    }
    
    
    public void AddItem(ItemInfo item)
    {
        
        ItemContainer newContainer = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity,  _itemLayer ).GetComponent<ItemContainer>();
        InventoryItemLayout layout = newContainer.GetComponent<InventoryItemLayout>();
        
        newContainer.Set( item );
        newContainer.transform.SetAsFirstSibling();
        
        DragObject drag = newContainer.GetComponent<DragObject>();
        ConnectDragObject( drag, layout, newContainer );

        _itemCount++;
        UpdateInventoryLayout();
    }

    private void ConnectDragObject( DragObject drag, InventoryItemLayout layout, ItemContainer newContainer )
    {
        drag.Initialize();
        drag.RemoveCallback = layout.RemoveCallback;
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromContainer = newContainer;
        drag.SourceObject = gameObject;
    }

    public void TryAddItem(ItemInfo item)
    {
        if ( !CanAddItem( item ) )
        {
            //TODO: drop item
            return;
        }
        
        AddItem( item );
        
    }

    public void ReOrderItem( DragObject drag, Vector2 position )
    {
        int childCount = _itemLayer.transform.childCount;
        for ( int i = 0; i < childCount; i++ )
        {
            Vector3 pos = _itemLayer.GetChild( i ).GetComponent<RectTransform>().position;
            if ( pos.y <= position.y )
            {
                int index = drag.TransferFromContainer.transform.GetSiblingIndex();
                if ( index <= i )
                {
                    index = Math.Max( i - 1, 0 );
                }
                else
                {
                    index = i;
                }

                //PlayerUIManager.Instance.ReorderItem( drag.Container.ItemKey, index );
                drag.Container.Item.Order = index;
                drag.TransferFromContainer.transform.SetSiblingIndex( index );
                UpdateItemOrders();
                return;
            }
        }
        //PlayerUIManager.Instance.ReorderItem( drag.Container.ItemKey, childCount );
        drag.Container.Item.Order = childCount;
        drag.TransferFromContainer.transform.SetSiblingIndex( childCount );
        UpdateItemOrders();
    }

    private void UpdateItemOrders()
    {
        int childCount = _itemLayer.transform.childCount;
        for ( int i = 0; i < childCount; i++ )
        {
           ItemContainer item = _itemLayer.GetChild( i ).GetComponent<ItemContainer>();
           item.Item.Order = i;
        }
        
    }

    public bool CanAddItem( ItemInfo info )
    {
        if ( _hasCapacity && _itemCount >= _maxItems )
            return false;


        foreach ( ItemType type in _itemTypeWhitelist )
        {
            if ( info.Data.ItemType == type )
                return true;
        }


        return false;
    }

    public void CollapseExpandInventory()
    {
        _collapsed = !_collapsed;
        if ( _collapsed )
        {
            _collapseButton.text = "+";
            _itemLayer.localScale = Vector3.zero;
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _collapsedHeight);

        }
        else
        {
            _collapseButton.text = "-";
            _itemLayer.localScale = Vector3.one;
            _rectTransform.sizeDelta = _itemLayer.rect.size + new Vector2(0,30);
        }
    }
    
    
    public void RemovedItem()
    {
        _itemCount--;
        UpdateInventoryLayout();
    }

    private Vector2 CalculateLayoutSize()
    {
        Vector2 itemSize = _invItemPrefab.GetComponent<RectTransform>().rect.size;
        
        Vector2 layoutSize = new Vector2(_rectTransform.rect.width, Math.Max(_itemCount, 1)*itemSize.y);
        layoutSize += new Vector2( 0, 30 + _spacing *(_itemCount+1) );
        return layoutSize;
    }

    private Vector2 CalculateItemListLayoutSize()
    {
        Vector2 itemSize = _invItemPrefab.GetComponent<RectTransform>().rect.size;
        
        Vector2 layoutSize = new Vector2(_rectTransform.rect.width, Math.Max(_itemCount, 1)*itemSize.y);
        layoutSize += new Vector2( 0, _spacing *(_itemCount+1) );
        return layoutSize;
    }
    
    private void UpdateInventoryLayout()
    {
        //update item capacity
        if(_hasCapacity)
            _itemCounter.text = $"{_itemCount}|{_maxItems}";
        else
            _itemCounter.text = _itemCount.ToString();
        
        //change layout to fit items
        if ( !_collapsed )
        {
            _rectTransform.sizeDelta = CalculateLayoutSize();
        }
        _itemLayer.sizeDelta = CalculateItemListLayoutSize();
            
    }
    
}

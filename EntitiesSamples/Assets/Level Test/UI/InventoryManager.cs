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

    private void Awake()
    {
        _manager = GetComponentInParent<PlayerUIManager>();
        _rectTransform = GetComponent<RectTransform>();
        
        _spacing = _itemLayer.GetComponent<VerticalLayoutGroup>().spacing;
    }

    private void Start()
    {
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
                    item.Weapon.CurrentAmmo = UnityEngine.Random.Range( 0, item.Weapon.MaxAmmo + 1 );
                    LoadItem( item );
                }
            }
            else if ( type == ItemType.Health )
            {
                _itemCount = _manager.HealthItems.Count;
                foreach ( HealthItemInfo item in _manager.HealthItems.Values )
                {
                    item.HealthItem.CurrentCharges = UnityEngine.Random.Range( 0, item.HealthItem.MaxCharges + 1 );
                    LoadItem( item );
                }
            }
        }
    }
    
    
    private List<ItemData> FetchItems()
    {
        List<ItemData> items = new List<ItemData>();
        foreach ( ItemType type in _itemTypeWhitelist )
        {
            if ( type == ItemType.Weapon )
            {
                items.AddRange( _manager.WeaponItems );
            }
            /*
            else if ( type == ItemType.Armor || type == ItemType.Helmet )
            {
                items.AddRange( _manager.EquipmentItems );
                return items;
            }
            else if ( type == ItemType.Health )
            {
                items.AddRange( _manager.HealthItems );
            }
            */
        }

        return items;
    }

    private void LoadItem( ItemInfo data )
    {
        //Debug.Log( data.GetType() );
        ItemContainer newContainer = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity, _itemLayer ).GetComponent<ItemContainer>();
        InventoryItemLayout layout = newContainer.GetComponent<InventoryItemLayout>();
        newContainer.Item = data;
        newContainer.transform.SetAsFirstSibling();

        DragObject drag = newContainer.GetComponent<DragObject>();
        drag.Initialize();
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromContainer = newContainer;
        drag.SourceObject = gameObject;
        
    }
    
    public void AddItem(ItemInfo item)
    {
        /*
        ItemInfo newItem = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity,  _itemLayer ).GetComponent<ItemInfo>();
        InventoryItemLayout layout = newItem.GetComponent<InventoryItemLayout>();
        
        
        
        newItem.Data = item.Data;
        newItem.transform.SetAsFirstSibling();
        
        DragObject drag = newItem.GetComponent<DragObject>();
        drag.Initialize();
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromObj = newItem;
        drag.SourceObject = gameObject;
        
        _itemCount++;
        UpdateInventoryLayout();
        */
    }
    
    public void AddItem(ItemContainer item)
    {
        
        ItemContainer newContainer = Instantiate( _invItemPrefab, Vector3.zero, Quaternion.identity,  _itemLayer ).GetComponent<ItemContainer>();
        InventoryItemLayout layout = newContainer.GetComponent<InventoryItemLayout>();
        
        newContainer.Item = item.Item;
        newContainer.transform.SetAsFirstSibling();
        
        DragObject drag = newContainer.GetComponent<DragObject>();
        drag.Initialize();
        drag.Callback = layout.CallBack;
        drag.SwapCallback = layout.SwapCallback;
        drag.TransferFromContainer = newContainer;
        drag.SourceObject = gameObject;
        
        _itemCount++;
        UpdateInventoryLayout();
        
    }
    

    public void ReOrderItem( DragObject drag, Vector2 position )
    {
        /*
        for ( int i = 0; i < _itemLayer.transform.childCount; i++ )
        {
            Vector3 pos = _itemLayer.GetChild( i ).GetComponent<RectTransform>().position;
            if ( pos.y <= position.y )
            {
                int index = drag.TransferFromObj.transform.GetSiblingIndex();
                if ( index <= i )
                {
                    index = Math.Max( i - 1, 0 );
                }
                else
                {
                    index = i;
                }
                drag.TransferFromObj.transform.SetSiblingIndex( index );
                return;
            }
        }
        drag.TransferFromObj.transform.SetSiblingIndex( _itemLayer.transform.childCount );
        */
    }

    public bool CanAddItem( ItemContainer info )
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
        
        //_rectTransform.sizeDelta = _itemLayer.rect.size + new Vector2(0,30); //works, but only if we delay update by a frame (becuase of deleting GO)
        UpdateInventoryLayout();
    }

    private Vector2 CalculateLayoutSize()
    {
        Vector2 itemSize = _invItemPrefab.GetComponent<RectTransform>().rect.size;
        
        Vector2 layoutSize = new Vector2(_rectTransform.rect.width, Math.Max(_itemCount, 1)*itemSize.y);
        layoutSize += new Vector2( 0, 30 + _spacing *(_itemCount+1) );
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
        if(!_collapsed)
            _rectTransform.sizeDelta = CalculateLayoutSize();
    }
    
}

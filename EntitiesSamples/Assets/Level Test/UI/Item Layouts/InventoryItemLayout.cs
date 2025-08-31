using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Random = System.Random;

public class InventoryItemLayout : MonoBehaviour
{
    [SerializeField]
    protected float _padding = 10;

    protected ItemContainer _item;

    [SerializeField]
    protected TextMeshProUGUI _itemText;
    [SerializeField]
    protected Image _itemImage;

    [SerializeField]
    protected Image _transferingImage;

    [SerializeField]
    private bool _initialized = false;

    public int Key => _item.Key;

    private void OnEnable()
    {
        if(_initialized) 
            ReloadItem();
    }

    private void OnDisable()
    {
        //PlayerUIManager.Instance.ItemUpdateEvent.RemoveListener( ReloadItem );
    }

    public void Initialize()
    {
        _initialized = true;
        _item = GetComponent<ItemContainer>();
        UpdateLayout();
    }

    protected virtual void UpdateLayout()
    {
    }

    public void ReloadItem()
    {
        transform.SetSiblingIndex( PlayerUIManager.Instance.AllItems[_item.Key].Order ); 
        UpdateLayout();
        /*
        if ( !PlayerUIManager.Instance.AllItems.ContainsKey( _item.Key ) )
        {
            GetComponentInParent<InventoryManager>().RemovedItem();
            //PlayerUIManager.Instance.ItemUpdateEvent.RemoveListener( ReloadItem );
            Destroy( gameObject );
        }
        else
        {
            transform.SetSiblingIndex( PlayerUIManager.Instance.AllItems[_item.Key].Order );
            UpdateLayout();
        }
        */
    }

    public virtual void UpdateCallBack()
    {
        Initialize();
    }
    
    public void CallBack()
    {
        //need to split this into a MoveCallback and UseCallback
        /*
        _item.Item.Quantity--;
        if ( _item.Item.Quantity <= 0 )
        {
            RemoveItem();
            return;
        }
        */
        //Initialize();
        GetComponentInParent<InventoryManager>().RemovedItem();
        Destroy( gameObject );
    }

    public void RemoveCallback()
    {
        RemoveItem();
    }

    protected void RemoveItem()
    {
        GetComponentInParent<InventoryManager>().RemovedItem();
        PlayerUIManager.Instance.RemoveItem( _item.Key );
        Destroy( gameObject );
    }

    public void SwapCallback()
    {
        Initialize();
    }
    
}

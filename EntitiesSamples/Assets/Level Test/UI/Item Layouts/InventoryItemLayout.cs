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

    protected ItemContainer _item;

    [SerializeField]
    protected TextMeshProUGUI _itemText;
    [SerializeField]
    protected Image _itemImage;

    [SerializeField]
    protected Image _transferingImage;

    private bool _initialized = false;
    
    private void Start()
    {
        Initialize();
        _initialized = true;
    }

    private void OnEnable()
    {
        PlayerUIManager.Instance.ItemUpdateEvent.AddListener( ReloadItem );
        if(_initialized)
            ReloadItem();
    }

    private void OnDisable()
    {
        PlayerUIManager.Instance.ItemUpdateEvent.RemoveListener( ReloadItem );
    }

    public virtual void Initialize()
    {
    }

    public void ReloadItem()
    {
        
        if ( !PlayerUIManager.Instance.AllItems.ContainsKey( _item.ItemKey ) )
        {
            GetComponentInParent<InventoryManager>().RemovedItem();
            PlayerUIManager.Instance.ItemUpdateEvent.RemoveListener( ReloadItem );
            Destroy( gameObject );
        }
        else
        {
            transform.SetSiblingIndex( PlayerUIManager.Instance.AllItems[_item.ItemKey].Order );
            Initialize();
        }
    }

    public virtual void UpdateCallBack()
    {
        Initialize();
    }
    
    public void CallBack()
    {
        _item.Item.Quantity--;
        if ( _item.Item.Quantity <= 0 )
        {
            RemoveItem();
            return;
        }
        Initialize();
    }

    protected void RemoveItem()
    {
        GetComponentInParent<InventoryManager>().RemovedItem();
        PlayerUIManager.Instance.RemoveItem( _item.ItemKey, false );
        Destroy( gameObject );
    }

    public void SwapCallback()
    {
        Initialize();
    }
    
}

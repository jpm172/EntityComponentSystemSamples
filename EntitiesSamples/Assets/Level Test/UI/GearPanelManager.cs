using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class GearPanelManager : DragManager
{

    private const string InventoryTag = "InventorySlot";

    [SerializeField]
    private GameObject[] _weaponSlots;

    
    protected override void Awake()
    {
        base.Awake();
        
        //_weaponInventory = _invetoryLayer.GetComponent<InventoryManager>();
        
        _weaponSlots = GameObject.FindGameObjectsWithTag( InventoryTag );
    }

    
/*
    public void PickUpItem(DragObject drag)
    {
        _currentDraggedObject = drag;
        drag.transform.SetParent(_dragLayer);
    }
    */


/*
    public void DropItem()
    {
        DragObject drag = _currentDraggedObject.GetComponent<DragObject>();
        TryPutIntoSlot( drag, drag._worldCenterPoint );

        Destroy( _currentDraggedObject.gameObject );
        _currentDraggedObject = null;
    }
    */
    
    /*
    public DragObject SpawnItem( ItemInfo item, Vector3 position )
    {
        DragObject newItem = Instantiate( _transferItemPrefab, position, Quaternion.identity, _defaultLayer ).GetComponent<DragObject>();
        newItem.GetComponent<ItemContainer>().ItemKey = item.Key;
        //newItem.GetComponent<ItemContainer>().Item = item;
        newItem.Initialize();

        if(_currentDraggedObject != null)
        {
            Destroy( _currentDraggedObject.gameObject );
            _currentDraggedObject = null;
        }
        PickUpItem( newItem );
        return newItem;
    }
    */


    protected override void TryPutIntoSlot(DragObject drag, Vector2 position)
    {
        if ( !GetBoundingBoxRect(_dragLayer).Contains(position) )
        {
            //TODO implement dropping items onto ground
            drag.Callback();
            return;
        }

        for ( int i = 0; i < _inventoryRects.Length; i++ )
        {
            Rect invRect = GetBoundingBoxRect( _inventoryRects[i] );
            if ( invRect.Contains( position )  )
            {
                if ( !_inventoryManagers[i].CanAddItem( drag.Container ) )
                    return;
                
                if ( drag.SourceObject == _inventoryRects[i].gameObject )
                {
                    _inventoryManagers[i].ReOrderItem( drag, position );
                    return;
                }
                
                _inventoryManagers[i].AddItem( drag.Container );
                drag.Callback();
                return;
            }
        }


        for(int i = 0; i < _weaponSlots.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _weaponSlots[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                //if trying to place the item in its original slot, just return
                if ( drag.SourceObject == _weaponSlots[i].gameObject )
                    return;
                
                ItemContainer container = drag.Container;
                InventorySlot slot = _weaponSlots[i].GetComponent<InventorySlot>();
                
                if ( !slot.MatchesType( container ) )
                    return;

                if ( slot.HasItem )
                {
                    slot.SwapItem( drag );
                }
                else
                {
                    slot.AddItem( container );
                    drag.Callback?.Invoke();
                }
                

                return;
            }
        }
    }

    
    private IEnumerator DelayAddToSlot(InventorySlot slot, DragObject drag, ItemContainer item)
    {
        yield return new WaitForSeconds( 1 );
        if ( slot.HasItem )
        {
            slot.SwapItem( drag );
        }
        else
        {
            slot.AddItem( item );
            drag.Callback?.Invoke();
        }
        
    }
    
    
}

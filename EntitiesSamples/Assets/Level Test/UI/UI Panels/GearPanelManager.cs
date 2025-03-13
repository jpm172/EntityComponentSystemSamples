using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class GearPanelManager : PanelManager
{

    private const string InventoryTag = "InventorySlot";
    
    
    [SerializeField]
    private GameObject[] _weaponSlots;
    
    [SerializeField]
    protected RectTransform[] _inventoryRects;
    [SerializeField]
    protected InventoryManager[] _inventoryManagers;

    

    private void Awake()
    {
        _weaponSlots = GameObject.FindGameObjectsWithTag( InventoryTag );
        
        InventoryManager[] managers = GetComponentsInChildren<InventoryManager>();
        _inventoryManagers = new InventoryManager[managers.Length];
        _inventoryRects = new RectTransform[managers.Length];
        for ( int i = 0; i < managers.Length; i++ )
        {
            _inventoryManagers[i] = managers[i];
            _inventoryRects[i] = managers[i].GetComponent<RectTransform>();
        }
    }

    public override void DropItem( DragObject drag )
    {
        TryPutIntoSlot( drag, drag._worldCenterPoint );
    }

    private void TryPutIntoSlot(DragObject drag, Vector2 position)
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

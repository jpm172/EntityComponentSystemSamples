using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHotBarSlotLayout : HotbarSlotLayout
{
    [SerializeField]
    private InventorySlot _linkedSlot;

    [SerializeField]
    private InventoryManager _linkedInventory;

    public override void TryPutInSlot( DragObject drag )
    {
        base.TryPutInSlot( drag);
        
        _linkedSlot.AddItemFromHotBar( drag.Container.Item );
        drag.Callback();

    }

    public override void SwapWith( HotbarSlotLayout otherSlot )
    {
        base.SwapWith( otherSlot );
        _linkedSlot.AddItemFromHotBar( _container.Item );
    }

    protected override void ReplaceSlot( ItemInfo item )
    {
        if ( item == null )
        {
            _linkedSlot.RemoveItemFromHotBar();
            base.ClearSlot( false );
            return;
        }
        UpdateLayout( item );

        _linkedSlot.AddItemFromHotBar( _container.Item );
    }

    protected override void DoubleClickClear()
    {
        if ( HasItem )
        {
            _linkedSlot.RemoveItemFromHotBar();
            _linkedInventory.TryAddItem( _container.Item );
        }

        base.DoubleClickClear();
    }
    public override void ClearSlot( bool deleteEntity )
    {
        if ( HasItem )
        {
            _linkedSlot.RemoveItemFromHotBar();
            //_linkedInventory.TryAddItem( _container.Item );
        }

        base.ClearSlot( deleteEntity );
    }
}

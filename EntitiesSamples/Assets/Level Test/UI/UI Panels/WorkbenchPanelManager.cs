using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkbenchPanelManager : PanelManager
{
    [SerializeField]
    private WorkbenchItemLayout _itemLayout;
    
    [SerializeField]
    protected RectTransform[] _inventoryRects;
    [SerializeField]
    protected InventoryManager[] _inventoryManagers;
    
    public override void Initialize()
    {
        _itemLayout.Initialize();
        
        InventoryManager[] managers = GetComponentsInChildren<InventoryManager>();
        _inventoryManagers = new InventoryManager[managers.Length];
        _inventoryRects = new RectTransform[managers.Length];
        for ( int i = 0; i < managers.Length; i++ )
        {
            _inventoryManagers[i] = managers[i];
            _inventoryManagers[i].Initialize();
            _inventoryRects[i] = managers[i].GetComponent<RectTransform>();
        }
    }

    private void OnDisable()
    {
        _itemLayout.ClearItem(); 
    }

    public override void DropItem( DragObject drag )
    {
        
    }

    public void PlaceWeaponOnBench( ItemInfo item )
    {
        _itemLayout.SetItem( item );
    }

    public void ClosePanel()
    {
        
        PlayerUIManager.Instance.CloseWorkbench();
    }

    
}

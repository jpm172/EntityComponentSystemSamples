using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkbenchPanelManager : PanelManager
{
    [SerializeField]
    private WorkbenchItemLayout _itemLayout;
    
    void Start()
    {
        
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
        _itemLayout.ClearItem();
        gameObject.SetActive( false );
    }
}

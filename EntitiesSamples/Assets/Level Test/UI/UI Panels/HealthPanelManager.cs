using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPanelManager : PanelManager
{
    
    
    [SerializeField]
    protected RectTransform[] _inventoryRects;
    [SerializeField]
    protected InventoryManager[] _inventoryManagers;
    
    [SerializeField]
    private BodyHealthManager _bodyManager;
    
    [SerializeField]
    private GameObject[] _bodyParts;
    
    /*
    private void Awake()
    {
        
        InventoryManager[] managers = GetComponentsInChildren<InventoryManager>();
        _inventoryManagers = new InventoryManager[managers.Length];
        _inventoryRects = new RectTransform[managers.Length];
        for ( int i = 0; i < managers.Length; i++ )
        {
            _inventoryManagers[i] = managers[i];
            _inventoryRects[i] = managers[i].GetComponent<RectTransform>();
        }
        
    }
*/
    
    public override void Initialize()
    {
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
    
    
    public override void DropItem( DragObject drag )
    {
        TryPutIntoSlot( drag, drag._worldCenterPoint );
    }
    
    private void TryPutIntoSlot( DragObject drag, Vector2 position )
    {
        if ( !GetBoundingBoxRect( _dragLayer ).Contains( position ) )
        {
            //TODO implement dropping items onto ground
            drag.RemoveCallback();
            return;
        }

        if ( drag.Container.Type != ItemType.Health )
            return;
        
        for(int i = 0; i < _bodyParts.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _bodyParts[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                BodyLabel bodyLabel = _bodyParts[i].GetComponent<BodyLabel>();
                
                //_bodyManager.HealBodyPart( bodyLabel.BodyPart, (HealthItemInfo)drag.Container.Item );
                _bodyManager.HealBodyPartECS( bodyLabel.BodyPart, (HealthItemInfo)drag.Container.Item );
                drag.UpdateCallback();
                return;
            }
        }
        
    }
    
    
}

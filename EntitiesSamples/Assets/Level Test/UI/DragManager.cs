using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class DragManager : MonoBehaviour
{

    private const string InventoryTag = "InventorySlot";
    
    private DemoInputActions _inputActions;
    private InputAction _mouseInput;

    [SerializeField] private RectTransform
        _defaultLayer,
        _dragLayer;

    [SerializeField]
    private GameObject _itemPrefab;
    [SerializeField]
    private GameObject _transferItemPrefab;

    [SerializeField]
    private GameObject[] _weaponSlots;

    [SerializeField]
    private RectTransform[] _inventoryRects;
    [SerializeField]
    private InventoryManager[] _inventoryManagers;

    [SerializeField]
    private DragObject _currentDraggedObject;
    public DragObject CurrentDraggedObject => _currentDraggedObject;
    private Vector3 _offset;
    
    
    public GameObject ItemPrefab => _itemPrefab;

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
        //_weaponInventory = _invetoryLayer.GetComponent<InventoryManager>();
        _inputActions = new DemoInputActions();
        _mouseInput = _inputActions.DemoMap.Shoot;
        _mouseInput.Enable();
        _weaponSlots = GameObject.FindGameObjectsWithTag( InventoryTag );
    }

    private void Update()
    {
        //Have the held item follow the mouse
        if ( _currentDraggedObject != null )
        {
            _currentDraggedObject.transform.position = Input.mousePosition + _offset;
            
            if ( _mouseInput.WasReleasedThisFrame() )
            {
                DropItem();
            }
        }

    }

    public void PickUpItem(DragObject drag)
    {
        _currentDraggedObject = drag;
        drag.transform.SetParent(_dragLayer);
    }


    public void DropItem()
    {
        DragObject drag = _currentDraggedObject.GetComponent<DragObject>();
        TryPutIntoSlot( drag, drag._worldCenterPoint );

        Destroy( _currentDraggedObject.gameObject );
        _currentDraggedObject = null;
    }
    
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


    private bool TryPutIntoSlot(DragObject drag, Vector2 position)
    {
        if ( !GetBoundingBoxRect(_dragLayer).Contains(position) )
        {
            //TODO implement dropping items onto ground
            drag.Callback();
            return true;
        }

        for ( int i = 0; i < _inventoryRects.Length; i++ )
        {
            Rect invRect = GetBoundingBoxRect( _inventoryRects[i] );
            if ( invRect.Contains( position )  )
            {
                if ( !_inventoryManagers[i].CanAddItem( drag.Container ) )
                    return false;
                
                if ( drag.SourceObject == _inventoryRects[i].gameObject )
                {
                    _inventoryManagers[i].ReOrderItem( drag, position );
                    return true;
                }
                
                _inventoryManagers[i].AddItem( drag.Container );
                drag.Callback();
                return true;
            }
        }
        
        
        
        for(int i = 0; i < _weaponSlots.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _weaponSlots[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                //if trying to place the item in its original slot, just return
                if ( drag.SourceObject == _weaponSlots[i].gameObject )
                    return false;
                
                ItemContainer container = drag.Container;
                InventorySlot slot = _weaponSlots[i].GetComponent<InventorySlot>();
                
                if ( !slot.MatchesType( container ) )
                    return false;
                
                //StartCoroutine( DelayAddToSlot(slot, drag, item) );
                
                if ( slot.HasItem )
                {
                    slot.SwapItem( drag );
                }
                else
                {
                    slot.AddItem( container );
                    drag.Callback?.Invoke();
                }
                

                return true;
            }
        }

        return false;
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
    
    private Rect GetBoundingBoxRect(RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var position = corners[0];

        Vector2 size = new Vector2(
            rectTransform.lossyScale.x * rectTransform.rect.size.x,
            rectTransform.lossyScale.y * rectTransform.rect.size.y);

        return new Rect(position, size);
    }
}

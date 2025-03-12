using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    private DemoInputActions _inputActions;
    private InputAction _mouseInput;


    [SerializeField] protected RectTransform
        _defaultLayer,
        _dragLayer;

    [SerializeField]
    protected GameObject _itemPrefab;
    [SerializeField]
    protected GameObject _transferItemPrefab;
    

    [SerializeField]
    private RectTransform _hotBarRect;

    private HotbarManager _hotBar;
    
    [SerializeField]
    protected DragObject _currentDraggedObject;
    public DragObject CurrentDraggedObject => _currentDraggedObject;
    private Vector3 _offset;
    
    public GameObject ItemPrefab => _itemPrefab;

    private void Awake()
    {
        _hotBar = _hotBarRect.GetComponent<HotbarManager>();

        _inputActions = new DemoInputActions();
        _mouseInput = _inputActions.DemoMap.Shoot;
        _mouseInput.Enable();
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
        drag.transform.SetParent(PlayerUIManager.Instance.ActivePanel.DragLayer);
        //drag.transform.SetParent(_dragLayer);
    }

    
    public void DropItem()
    {
        DragObject drag = _currentDraggedObject.GetComponent<DragObject>();
        
        if(!TryPutIntoHotbar( drag, drag._worldCenterPoint ))
            PlayerUIManager.Instance.ActivePanel.DropItem( drag );
        
        /*
        if(!TryPutIntoHotbar( drag, drag._worldCenterPoint ))
            TryPutIntoSlot( drag, drag._worldCenterPoint );
            */

        Destroy( _currentDraggedObject.gameObject );
        _currentDraggedObject = null;
    }

    private bool TryPutIntoHotbar(DragObject drag, Vector2 position)
    {
        if ( GetBoundingBoxRect( _hotBarRect ).Contains( position ) )
        {
            _hotBar.TryAddToHotBar( drag, position );
            return true;
        }

        return false;
    }

    public DragObject SpawnItem( ItemInfo item, Vector3 position )
    {
        DragObject newItem = Instantiate( _transferItemPrefab, position, Quaternion.identity ).GetComponent<DragObject>();
        //DragObject newItem = Instantiate( _transferItemPrefab, position, Quaternion.identity, _defaultLayer ).GetComponent<DragObject>();
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

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
    
    [SerializeField]
    private RectTransform
        _defaultLayer = null,
        _dragLayer = null;


    [SerializeField]
    private GameObject _itemPrefab;
    
    private Rect _boundingBox;

    [SerializeField]
    private GameObject[] _inventorySlots;

    [SerializeField]
    private DragObject _currentDraggedObject;
    public DragObject CurrentDraggedObject => _currentDraggedObject;
    private Vector3 _offset;
    
    
    public GameObject ItemPrefab => _itemPrefab;

    private void Awake()
    {
        _boundingBox = GetBoundingBoxRect(_dragLayer);
        _inputActions = new DemoInputActions();
        _mouseInput = _inputActions.DemoMap.Shoot;
        _mouseInput.Enable();
        _inventorySlots = GameObject.FindGameObjectsWithTag( InventoryTag );
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
        
        if ( !TryPutIntoSlot( drag, drag._worldCenterPoint ) )
        {
            _currentDraggedObject.transform.SetParent( _defaultLayer );
        }
        
        
        
        _currentDraggedObject = null;
    }
    
    public bool IsWithinBounds(Vector2 position)
    {
        return _boundingBox.Contains(position);
    }

    public void SpawnItem( ItemInfo item, Vector3 position )
    {
        DragObject newItem = Instantiate( ItemPrefab, position, Quaternion.identity, _defaultLayer ).GetComponent<DragObject>();
        
        if(_currentDraggedObject == null)
        {
            PickUpItem( newItem );
        }
        
    }
    
    private bool TryPutIntoSlot(DragObject drag, Vector2 position)
    {
        for(int i = 0; i < _inventorySlots.Length; i++)
        {
            Rect rect = GetBoundingBoxRect( _inventorySlots[i].GetComponent<RectTransform>() );
            if ( rect.Contains( position ) )
            {
                ItemInfo item = drag.GetComponent<ItemInfo>();
                _inventorySlots[i].GetComponent<InventorySlot>().AddItem( item );
                Destroy( drag.gameObject );
                return true;
            }
        }

        return false;
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

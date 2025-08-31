using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    private const float TransitionSpeed = 4.5f;

    public GameObject EquipHighlight;

    private DemoInputActions _inputActions;
    private InputAction _slot1;
    
    private Vector3 _openPosition = new Vector3(0,-237, 0);
    private Vector3 _closePosition = new Vector3(0,-300, 0);
    private RectTransform _rect;

    private PlayerUIManager _manager;
    
    [SerializeField]
    private HotbarSlotLayout[] _slots;

    private HotbarSlotLayout _currentEquipped;
    [SerializeField]
    private bool _open;
    private bool _holdOpen;
    [SerializeField]
    private float _openTimer;

    
    
    public bool HoldOpen
    {
        get => _holdOpen;
        set => _holdOpen = SetHoldOpen(value);
    }

    void Start()
    {
        _manager = PlayerUIManager.Instance;
        _slots = GetComponentsInChildren<HotbarSlotLayout>();
        _rect = GetComponent<RectTransform>();
        _inputActions = new DemoInputActions();
        
        _inputActions.Enable();
        _inputActions.DemoMap.Primary.performed += EquipSlot1;
        _inputActions.DemoMap.Secondary.performed += EquipSlot2;
        _inputActions.DemoMap.Hotbar3.performed += EquipSlot3;
        _inputActions.DemoMap.Hotbar4.performed += EquipSlot4;
        _inputActions.DemoMap.Hotbar5.performed += EquipSlot5;
        _inputActions.DemoMap.Hotbar6.performed += EquipSlot6;
        _inputActions.DemoMap.Hotbar7.performed += EquipSlot7;
        _inputActions.DemoMap.Hotbar8.performed += EquipSlot8;
        _inputActions.DemoMap.Hotbar9.performed += EquipSlot9;
        
        _open = true;
        _holdOpen = true;
        
        EquipSlot( 0 );
        
    }


    public int GetEquippedIndex()
    {
        if ( _currentEquipped == null )
            return -1;

        return _currentEquipped.SlotIndex;
    }

    private void FixedUpdate()
    {
        //handles the smooth transitioning between revealed/hidden
        if ( _open )
        {
            _rect.localPosition = Vector3.MoveTowards( _rect.localPosition, _openPosition, TransitionSpeed );

            if ( !_holdOpen )
            {
                _openTimer += Time.fixedDeltaTime;
                if(_openTimer >= 2)
                    HideHotBar();
            }
            else
            {
                _openTimer = 0;
            }
        }
        else
        {
            _rect.localPosition = Vector3.MoveTowards( _rect.localPosition, _closePosition, TransitionSpeed );
        }
        
    }

    private bool SetHoldOpen( bool value )
    {
        if(value && !_open)
            RevealHotBar();
        
        if(!value && _open)
            HideHotBar();
        
        return value;
    }
    
    private void HideHotBar()
    {
        _open = false;
    }

    private void RevealHotBar()
    {
        _open = true;
    }

    public void TryAddToHotBar(DragObject drag, Vector2 position)
    {
        for ( int i = 0; i < _slots.Length; i++ )
        {
            HotbarSlotLayout slot = _slots[i];
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if ( GetBoundingBoxRect( slotRect ).Contains( position ) )
            {
                if (!slot.CanPutInSlot( drag.Container ) )
                    return;

                if ( ContainsItem( drag.Container.Item, out int result ) )
                {
                    if ( result != i )
                    {
                        slot.SwapWith( _slots[result] );
                        _manager.SwapItemEntities( result, i );
                    }
                    
                    return;
                }

                slot.TryPutInSlot( drag );
                return;
            }
        }
    }



    private bool ContainsItem(ItemInfo item, out int result)
    {
        int itemKey = item.Key;
        result = -1;
        for ( int i = 0; i < _slots.Length; i++ )
        {
            HotbarSlotLayout slot = _slots[i];
            
            
            if ( slot.HasItem && slot.HeldItem.Key == itemKey )
            {
                result = i;
                return true;
            }
        }

        return false;
    }

    public bool ContainsItem( int itemKey, out int result )
    {
        result = -1;
        for ( int i = 0; i < _slots.Length; i++ )
        {
            HotbarSlotLayout slot = _slots[i];
            
            if ( slot.HasItem && slot.HeldItem.Key == itemKey )
            {
                result = i;
                return true;
            }
        }

        return false;
    }

    public void AddToHotBar( ItemContainer item, int slotIndex )
    {
        if ( ContainsItem( item.Item, out int result ) )
        {
            _slots[slotIndex].SwapWith( _slots[result] );
            _manager.SwapItemEntities( result, slotIndex );
            return;
        }
        
        _slots[slotIndex].TryPutInSlot( item );

    }

    public void TryRemoveFromHotBar(int itemKey)
    {
        foreach ( HotbarSlotLayout slot in _slots )
        {
            if ( slot.HasItem && slot.HeldItem.Key == itemKey )
            {
                slot.ClearSlot(true);
                return;
            }
        }
    }
    
    public void RemoveFromHotBar( int slotIndex )
    {
        _slots[slotIndex].ClearFromLinkedSlot();
    }
    
    
    
    public void EquipSlot( int slotIndex )
    {

        if ( slotIndex >= _slots.Length )
        {
            throw new IndexOutOfRangeException($"Invalid Slot Index {slotIndex}");
        }
        
        EquipHighlight.transform.SetParent( _slots[slotIndex].transform, false );
        
        _slots[slotIndex].Equipped = true;
        int previousIndex = -1;
        if ( _currentEquipped != null )
        {
            _currentEquipped.Equipped = false;
            previousIndex = _currentEquipped.SlotIndex;
        }
            
        _currentEquipped = _slots[slotIndex];
        
        _manager.EquipSlot( slotIndex, previousIndex );
        
        if(!_open)
            RevealHotBar();
        _openTimer = 0;
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

    private void EquipSlot1(InputAction.CallbackContext context)
    {
        EquipSlot( 0 );
    }
    
    private void EquipSlot2(InputAction.CallbackContext context)
    {
        EquipSlot( 1 );
    }
    
    private void EquipSlot3(InputAction.CallbackContext context)
    {
        EquipSlot( 2 );
    }
    
    private void EquipSlot4(InputAction.CallbackContext context)
    {
        EquipSlot( 3 );
    }
    private void EquipSlot5(InputAction.CallbackContext context)
    {
        EquipSlot( 4 );
    }
    private void EquipSlot6(InputAction.CallbackContext context)
    {
        EquipSlot( 5 );
    }
    private void EquipSlot7(InputAction.CallbackContext context)
    {
        EquipSlot( 6 );
    }
    private void EquipSlot8(InputAction.CallbackContext context)
    {
        EquipSlot( 7 );
    }
    private void EquipSlot9(InputAction.CallbackContext context)
    {
        EquipSlot( 8 );
    }

    
}

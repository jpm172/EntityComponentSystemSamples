using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public GameObject EquipHighlight;

    private DemoInputActions _inputActions;
    private InputAction _slot1;
    
    [SerializeField]
    private HotbarSlotLayout[] _slots;
    // Start is called before the first frame update
    void Start()
    {
        _slots = GetComponentsInChildren<HotbarSlotLayout>();
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

    }

    public void TryAddToHotBar(DragObject drag, Vector2 position)
    {
        for ( int i = 0; i < _slots.Length; i++ )
        {
            HotbarSlotLayout slot = _slots[i];
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if ( GetBoundingBoxRect( slotRect ).Contains( position ) )
            {
                bool hasItem = ContainsItem( drag, out int result );
                if ( slot.TryPutInSlot( drag.Container ) )
                {
                    if ( hasItem && result != i )
                    {
                        ClearSlot( result );
                    }
                }
                return;
            }
        }
    }

    private bool ContainsItem(DragObject drag, out int result)
    {
        int itemKey = drag.Container.ItemKey;
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
        _slots[slotIndex].TryPutInSlot( item );
    }

    public void RemoveFromHotBar( int slotIndex )
    {
        _slots[slotIndex].ClearSlot();
    }
    
    private void EquipSlot( int slotIndex )
    {
        EquipHighlight.transform.parent = _slots[slotIndex].transform;
        EquipHighlight.transform.localPosition = Vector3.zero;
    }

    private void ClearSlot( int slotIndex )
    {
        _slots[slotIndex].ClearSlot();
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

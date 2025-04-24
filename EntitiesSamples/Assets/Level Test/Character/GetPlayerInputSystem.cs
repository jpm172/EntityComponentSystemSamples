using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class GetPlayerInputSystem : SystemBase
{
    private DemoInputActions _inputActions;
    private Camera _camera;
    private static float3 xy = new float3(1,1,0);
    private GameObject _ui;

    protected override void OnCreate()
    {
        _inputActions = new DemoInputActions();
    }

    protected override void OnStartRunning()
    {
        _camera = Camera.main;
        _ui = GameObject.FindWithTag( "PlayerUI" );
        _inputActions.Enable();
        _inputActions.DemoMap.Interact.performed += OnPlayerInteract;
    }

    protected override void OnUpdate()
    {
        Vector2 moveInput = _inputActions.DemoMap.PlayerMovement.ReadValue<Vector2>();
        bool shoot = _inputActions.DemoMap.Shoot.IsPressed();
        bool reload = _inputActions.DemoMap.Reload.IsPressed();
        bool inventory = _inputActions.DemoMap.Inventory.WasPerformedThisFrame();
        bool altFire = _inputActions.DemoMap.AlternateFire.IsPressed();
        bool quickSwitch = _inputActions.DemoMap.QuickSwitch.WasPerformedThisFrame();
        altFire = _inputActions.DemoMap.AlternateFire.WasPerformedThisFrame(); //DEBUG FOR HEALTH!!!


        float3 mousePosition = _camera.ScreenToWorldPoint( Input.mousePosition ) * xy;
        
        foreach (var (playerInputs, playerInventory) in SystemAPI.Query<RefRW<PlayerInputs>, RefRW<CharacterInventory>>())
        {
            shoot &= !playerInventory.ValueRO.Switching;
            altFire &= !playerInventory.ValueRO.Switching;

            if ( inventory )
            {
                PlayerUIManager.Instance.ToggleInventory();
            }

            if ( quickSwitch )
            {
                PlayerUIManager.Instance.QuickSwitch(); 
            }
            
            playerInputs.ValueRW.MoveInput = moveInput;
            playerInputs.ValueRW.AimPosition = mousePosition;
            playerInputs.ValueRW.Shoot = shoot;
            playerInputs.ValueRW.AltFire = altFire;
            playerInputs.ValueRW.Reload = reload;
            playerInputs.ValueRW.QuickSwitch = quickSwitch;
        }
        
        //Debug.Log( mousePosition );

    }

    protected override void OnStopRunning()
    {
        _inputActions.DemoMap.Interact.performed -= OnPlayerInteract;
        _inputActions.Disable();
    }

    private void OnPlayerInteract(InputAction.CallbackContext context)
    {
        Debug.Log( "interact" );
    }
}

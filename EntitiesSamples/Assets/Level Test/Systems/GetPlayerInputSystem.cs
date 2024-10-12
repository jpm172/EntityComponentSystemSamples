using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;


[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class GetPlayerInputSystem : SystemBase
{
    private DemoInputActions _inputActions;

    protected override void OnCreate()
    {
        _inputActions = new DemoInputActions();
    }

    protected override void OnStartRunning()
    {
        _inputActions.Enable();
        _inputActions.DemoMap.Interact.performed += OnPlayerInteract;
    }

    protected override void OnUpdate()
    {
        Vector2 moveInput = _inputActions.DemoMap.PlayerMovement.ReadValue<Vector2>();

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

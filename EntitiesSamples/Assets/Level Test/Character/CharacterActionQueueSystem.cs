using Unity.Burst;
using Unity.Entities;
using UnityEngine;

partial struct CharacterActionQueueSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        
        foreach ( var (actionQueue, inventory, items, player) in 
            SystemAPI.Query<DynamicBuffer<CharacterAction>, RefRW<CharacterInventory>, DynamicBuffer<InventoryElement>>().WithEntityAccess() )
        {
            if(actionQueue.IsEmpty)
                continue;

            CharacterAction currentAction = actionQueue[0];

            switch ( currentAction.Action )
            {
                case ActionType.EquipItem:
                    ProcessEquipItemAction(currentAction, inventory, actionQueue);
                    break;
                case ActionType.EquipSlot:
                    ProcessEquipSlotAction( currentAction, inventory, items, actionQueue );
                    break;
                case ActionType.Use:
                    ProcessUseAction(currentAction, actionQueue, inventory.ValueRO, ref state);
                    break;
                case ActionType.Remove:
                    ProcessRemoveAction(currentAction, inventory, items, ecb, actionQueue, ref state);
                    break;
            }
            
        }
    }

    private void ProcessRemoveAction(CharacterAction currentAction, RefRW<CharacterInventory> inventory, 
        DynamicBuffer<InventoryElement> items, EntityCommandBuffer ecb, DynamicBuffer<CharacterAction> actionQueue, ref SystemState state)
    {
        state.EntityManager.SetComponentEnabled( currentAction.Item, typeof(RemoveItem), true );
        actionQueue.RemoveAt( 0 );
        /*
        bool inHotbar = false;
        foreach ( InventoryElement item in items )
        {
            if ( item.Item == currentAction.Item )
            {
                inHotbar = true;
                actionQueue.RemoveAt( 0 );
                return;
            }
        }

        ecb.DestroyEntity( currentAction.Item );
        actionQueue.RemoveAt( 0 );
        */
        
        
    }

    private void ProcessUseAction(CharacterAction currentAction, DynamicBuffer<CharacterAction> actionQueue, CharacterInventory inventory, ref SystemState state)
    {

        if ( inventory.EquippedItem != currentAction.Item )
        {
            actionQueue.RemoveAt( 0 );
            return;
        }
            
        
        ItemStateInfo itemState = state.EntityManager.GetComponentData<ItemStateInfo>( currentAction.Item );

        if ( itemState.State == ItemState.Ready )
        {
            itemState.State = ItemState.Using;
            state.EntityManager.SetComponentData( currentAction.Item, itemState );
        }

        if ( itemState.State == ItemState.Finished )
        {
            itemState.State = ItemState.Ready;
            state.EntityManager.SetComponentData( currentAction.Item, itemState );
            actionQueue.RemoveAt( 0 );
        }
        
    }
    
    private void ProcessEquipItemAction(CharacterAction currentAction, RefRW<CharacterInventory> inventory, DynamicBuffer<CharacterAction> actionQueue)
    {
        if ( !inventory.ValueRW.IsInPipeline( currentAction.Item ) )
        {
            inventory.ValueRW.SwitchToBuffer = new EquippingData(currentAction.Item);
            return;
        }

        if ( inventory.ValueRO.EquippedItem == currentAction.Item )
        {
            actionQueue.RemoveAt( 0 );
        }
        
    }
    
    private void ProcessEquipSlotAction(CharacterAction currentAction, RefRW<CharacterInventory> inventory, DynamicBuffer<InventoryElement> items, DynamicBuffer<CharacterAction> actionQueue)
    {
        Entity equipItem = items[currentAction.EquipSlot].Item;
        if ( !inventory.ValueRW.IsInPipeline( equipItem ) )
        {
            inventory.ValueRW.SwitchToBuffer = new EquippingData(equipItem);
            return;
        }

        if ( !inventory.ValueRW.Switching )
        {
            actionQueue.RemoveAt( 0 );
        }
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

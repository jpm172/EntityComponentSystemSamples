using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial struct CharacterInventorySystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        
        DeleteItems( ref state, ecb );
        
        foreach ( var (input, inventory, player) in SystemAPI.Query<RefRO<PlayerInputs>, RefRW<CharacterInventory>>().WithEntityAccess() )
        {
            if(!inventory.ValueRW.Switching)
                continue;

            ProcessBuffer( inventory, ref state );

            if ( inventory.ValueRW.SwitchBack )
            {
                inventory.ValueRW.Remaining -= SystemAPI.Time.DeltaTime;
                float itemTime = GetItemEquipTime( inventory.ValueRW.EquippedItem, ref state );


                if ( inventory.ValueRW.IsSwitchingTo( inventory.ValueRW.EquippedItem ) )
                {
                    if ( inventory.ValueRW.Remaining <= 0 )
                    {
                        inventory.ValueRW.SwitchToItem = EquippingData.Null;
                        inventory.ValueRW.SwitchToBuffer = EquippingData.Null;
                        inventory.ValueRW.Remaining = 0;
                        inventory.ValueRW.Timer = 0;
                        inventory.ValueRW.SwitchBack = false;
                        continue;
                    }
                }
                else if ( inventory.ValueRW.Remaining <=  itemTime)
                {
                    inventory.ValueRW.SwitchToItem = inventory.ValueRW.SwitchToBuffer;
                    inventory.ValueRW.SwitchToBuffer = EquippingData.Null;
                    inventory.ValueRW.Timer = itemTime + GetItemEquipTime( inventory.ValueRW.SwitchToItem.SwitchTo, ref state );
                    inventory.ValueRW.SwitchBack = false;
                }
            }
            else
            {
                inventory.ValueRW.Remaining += SystemAPI.Time.DeltaTime;
            }
            

            if ( inventory.ValueRW.Remaining >= inventory.ValueRW.Timer )
            {
                FinishedEquip( inventory, player, ecb, ref state );
            }

        }
    }

    private void DeleteItems(ref SystemState state, EntityCommandBuffer ecb)
    {
        foreach ( var (itemData, destroyItem, itemEntity) in SystemAPI.Query<RefRO<CharacterItemData>, EnabledRefRW<DestroyItem>>()
            .WithEntityAccess() )
        {
            Entity owner = itemData.ValueRO.Owner;
            CharacterInventory characterInventory = state.EntityManager.GetComponentData<CharacterInventory>( owner );

            DynamicBuffer<InventoryItem> inventoryItems = state.EntityManager.GetBuffer<InventoryItem>( owner );
            DynamicBuffer<HotBarItem> hotbar = state.EntityManager.GetBuffer<HotBarItem>( owner );

            for ( int i = inventoryItems.Length - 1; i >= 0; i-- )
            {
                if ( inventoryItems[i].Item == itemEntity )
                {
                    inventoryItems.RemoveAt( i );
                    break;
                }
            }

            for ( int i = 0; i < hotbar.Length; i++ )
            {
                if ( hotbar[i].Item == itemEntity )
                {
                    hotbar.ElementAt( i ).Item = Entity.Null;
                    break;
                }
            }

            if ( owner == PlayerUIManager.Instance.PlayerEntity )
            {
                PlayerUIManager.Instance.RemoveItem( itemData.ValueRO.Key );
            }
            
            if ( characterInventory.IsInPipeline( itemEntity ) )
            {
                characterInventory = ClearItemFromEquipBuffer( characterInventory, itemEntity );
                state.EntityManager.SetComponentData( owner, characterInventory );
            }

            ecb.DestroyEntity( itemEntity );
        }
    }

    //if an item is destroyed, we can probably just remove it and skip the "putting away" timer, since the item was used up
    private CharacterInventory ClearItemFromEquipBuffer( CharacterInventory characterInventory, Entity itemEntity )
    {
        
        if ( characterInventory.SwitchToItem.SwitchTo == itemEntity )
        {
            characterInventory.SwitchToItem = EquippingData.Empty;
        }
        
        if ( characterInventory.SwitchToBuffer.SwitchTo == itemEntity || characterInventory.EquippedItem == itemEntity )
        {
            characterInventory.SwitchToBuffer = EquippingData.Empty;
        }

        return characterInventory;
    }

    private void FinishedEquip( RefRW<CharacterInventory> inventory, Entity player, EntityCommandBuffer ecb, ref SystemState state )
    {

        inventory.ValueRW.EquippedItem = inventory.ValueRW.SwitchToItem.SwitchTo;
        inventory.ValueRW.SwitchToItem = EquippingData.Null;
        inventory.ValueRW.SwitchToBuffer = EquippingData.Null;
        inventory.ValueRW.Remaining = 0;
        inventory.ValueRW.Timer = 0;
    }
    
    private float GetItemEquipTime( Entity entity, ref SystemState state )
    {
        if ( entity == Entity.Null )
            return 0;

        CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( entity );
        return itemData.EquipTime;
    }
    
    private void ProcessBuffer( RefRW<CharacterInventory> inventory, ref SystemState state )
    {
        if ( inventory.ValueRW.SwitchToBuffer == EquippingData.Null )
            return;
        
        if ( inventory.ValueRW.SwitchToItem == EquippingData.Null )
        {
            inventory.ValueRW.SwitchToItem = inventory.ValueRW.SwitchToBuffer;
            inventory.ValueRW.SwitchToBuffer = EquippingData.Null;

            float equipTime1 = GetItemEquipTime( inventory.ValueRW.EquippedItem, ref state );
            float equipTime2 = GetItemEquipTime( inventory.ValueRW.SwitchToItem.SwitchTo, ref state );
            
            inventory.ValueRW.Timer = equipTime1 + equipTime2;
            inventory.ValueRW.Remaining = 0;
            return;
        }

        if ( inventory.ValueRW.SwitchToItem.SwitchTo == inventory.ValueRW.SwitchToBuffer.SwitchTo )
        {
            inventory.ValueRW.SwitchBack = false;
            return;
        }

        inventory.ValueRW.SwitchBack = true;


        
    }
}

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
        foreach ( var (input, inventory) in SystemAPI.Query<RefRO<PlayerInputs>, RefRW<CharacterInventory>>() )
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
                inventory.ValueRW.EquippedItem = inventory.ValueRW.SwitchToItem.SwitchTo;
                inventory.ValueRW.SwitchToItem = EquippingData.Null;
                inventory.ValueRW.SwitchToBuffer = EquippingData.Null;
                inventory.ValueRW.Remaining = 0;
                inventory.ValueRW.Timer = 0;
            }

        }
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

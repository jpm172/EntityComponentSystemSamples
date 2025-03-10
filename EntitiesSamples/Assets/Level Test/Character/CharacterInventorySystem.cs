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
            if ( inventory.ValueRW.Timer <= 0 )
                continue;

            inventory.ValueRW.Remaining += SystemAPI.Time.DeltaTime;

            if ( inventory.ValueRW.Remaining >= inventory.ValueRW.Timer )
            {
                inventory.ValueRW.EquippedItem = inventory.ValueRW.SwitchToItem;
                inventory.ValueRW.SwitchToItem = Entity.Null;
                inventory.ValueRW.Remaining = 0;
                inventory.ValueRW.Timer = 0;
            }

        }
    }
}

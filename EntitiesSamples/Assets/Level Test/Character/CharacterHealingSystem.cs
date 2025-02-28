using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial struct CharacterHealingSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        foreach ( var (input, inventory, player) in SystemAPI.Query< RefRO<PlayerInputs>, RefRW<CharacterInventory>>().WithEntityAccess() )
        {
            Entity equippedItem = inventory.ValueRW.EquippedItem;
            if(!input.ValueRO.Shoot || !state.EntityManager.HasComponent( equippedItem,typeof(HealthItemDesc) ))
                continue;

            
            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct CharacterHealthSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        
        
        
        UseHealingItem( ref state );
    }

    private void UseHealingItem(ref SystemState state)
    {
        foreach ( var (input, inventory, player) in SystemAPI.Query< RefRO<PlayerInputs>, RefRW<CharacterInventory>>().WithEntityAccess() )
        {
            Entity equippedItem = inventory.ValueRW.EquippedItem;

            if ( input.ValueRO.AltFire )
            {
                AddWound(ref state, player);
                return;
            }
                
            
            if(!input.ValueRO.Shoot || !state.EntityManager.HasComponent( equippedItem,typeof(HealthItemDesc) ))
                continue;

            
            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            
        }
    }

    private void AddWound(ref SystemState state, Entity player)
    {
        int limbIndex = 0;
        DynamicBuffer<CharacterLimb> body = state.EntityManager.GetBuffer<CharacterLimb>( player );
        CharacterLimb limb = body[limbIndex];

        limb.CurrentHealth = math.max( 0, limb.CurrentHealth - 10 );
        body[limbIndex] = limb;

    }
}

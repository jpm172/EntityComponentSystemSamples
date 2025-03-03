using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
        ApplyBleedDamage( ref state );
        
        foreach ( var (damage, wounds, character, player) in 
            SystemAPI.Query<DynamicBuffer<DamageInfo>, DynamicBuffer<CharacterWound>, RefRW<MyCharacterComponent>>().WithEntityAccess() )
        {
            if(damage.IsEmpty)
                continue;

            DynamicBuffer<CharacterLimb> body = state.EntityManager.GetBuffer<CharacterLimb>( player );

            for ( int i = damage.Length - 1; i >= 0; i-- )
            {
                CharacterWound newWound = new CharacterWound(damage[i], BodyPart.LeftArm);
                AddWound( newWound, body, character );
                wounds.Add( newWound );
                damage.RemoveAt( i );
            }

        }


        UseHealingItem( ref state );
    }

    private void ApplyBleedDamage(ref SystemState state)
    {
        foreach ( var (body, character, player) in SystemAPI.Query<DynamicBuffer<CharacterLimb>, RefRW<MyCharacterComponent>>().WithEntityAccess() )
        {
            for ( int i = 0; i < body.Length; i++ )
            {
                ref CharacterLimb limb = ref body.ElementAt( i ); 
                float bleedDmg = limb.Bleed * SystemAPI.Time.DeltaTime;
            
                
                if ( limb.Destroyed )
                {
                    ref CharacterLimb chest = ref body.ElementAt( (int)BodyPart.Chest );
                    //chest.CurrentHealth -= bleedDmg;
                    character.ValueRW.Health -= chest.Damage( bleedDmg );
                    continue;
                }

                character.ValueRW.Health -= limb.Damage( bleedDmg );
                //float damageDealt = limb.Damage( bleedDmg );
                //limb.CurrentHealth -= bleedDmg;
            }


            if ( character.ValueRW.Health <= 0 )
            {
                //TODO: die
            }
            
        }
    }
    
    private void UseHealingItem(ref SystemState state)
    {
        foreach ( var (input, inventory, player) in SystemAPI.Query< RefRO<PlayerInputs>, RefRW<CharacterInventory>>().WithEntityAccess() )
        {
            Entity equippedItem = inventory.ValueRW.EquippedItem;


            if(!input.ValueRO.Shoot || !state.EntityManager.HasComponent( equippedItem,typeof(HealthItemDesc) ))
                continue;

            
            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            
        }
    }

    private void AddWound(CharacterWound newWound,  DynamicBuffer<CharacterLimb> body, RefRW<MyCharacterComponent> character)
    {
        int limbIndex = (int)newWound.AffectedPart;
        /*
        for ( int i = 0; i < body.Length; i++ )
        {
            if ( body[i].Part == newWound.AffectedPart )
            {
                limbIndex = i;
                break;
            }
        }
        */
        CharacterLimb limb = body[limbIndex];

        float damageDealt = limb.Damage( newWound );

        character.ValueRW.Health -= damageDealt;
        
        //limb.CurrentHealth = math.max( 0, limb.CurrentHealth - newWound.HealingNeeded );
        //limb.Bleed += newWound.Bleed;
        body[limbIndex] = limb;

    }
}

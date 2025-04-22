using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct CharacterHealthSystem : ISystem
{
    

    private NativeArray<BodyPart> _bodyParts;
    //private EndSimulationEntityCommandBufferSystem _commandBuffer;
    public void OnCreate( ref SystemState state )
    {
        //_commandBuffer = state.World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        _bodyParts = new NativeArray<BodyPart>(
            new []{BodyPart.Head, BodyPart.Chest, BodyPart.LeftArm, BodyPart.RightArm, BodyPart.LeftLeg, BodyPart.RightLeg}
            , Allocator.Persistent);
    }

    public void OnDestroy( ref SystemState state )
    {
        _bodyParts.Dispose();
    }

    public void OnUpdate( ref SystemState state )
    {
        
        //ApplyBleedDamage( ref state );
        
        foreach ( var (damage, wounds, character) in 
            SystemAPI.Query<DynamicBuffer<DamageInfo>, DynamicBuffer<CharacterWound>, RefRW<CharacterStats>>() )
        {
            if(damage.IsEmpty)
                continue;
            

            for ( int i = damage.Length - 1; i >= 0; i-- )
            {
                CharacterWound newWound = new CharacterWound(damage[i], BodyPart.LeftArm);
                AddWound( newWound, wounds, character );
            }
            damage.Clear();
            
        }


        UseHealingItem( ref state );
    }

    private void ApplyBleedDamage(ref SystemState state)
    {
        foreach ( var (stats, character, player) in SystemAPI.Query<RefRW<CharacterStats>, RefRW<MyCharacterComponent>>().WithEntityAccess() )
        {
            
            for ( int i = 0; i < _bodyParts.Length; i++ )
            {
                CharacterLimb limb = stats.ValueRW.BaseStats.GetLimb( _bodyParts[i] ); 
                float bleedDmg = limb.Bleed * SystemAPI.Time.DeltaTime;
            
                
                if ( limb.Destroyed )
                {
                    CharacterLimb chest = stats.ValueRW.BaseStats.GetLimb( BodyPart.Chest );
                    stats.ValueRW.BaseStats.Health -= chest.Damage( bleedDmg );
                    stats.ValueRW.BaseStats.SetLimb( chest );
                    continue;
                }

                stats.ValueRW.BaseStats.Health -= limb.Damage( bleedDmg );
                stats.ValueRW.BaseStats.SetLimb( limb );
            }


            if ( stats.ValueRW.BaseStats.Health <= 0 )
            {
                //TODO: die
            }
            
        }
    }
    
    
    private void UseHealingItem(ref SystemState state)
    {
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        foreach ( var (input, inventory, character, player) in 
            SystemAPI.Query< RefRO<PlayerInputs>, RefRW<CharacterInventory>, RefRW<CharacterStats>>().WithEntityAccess() )
        {
            Entity equippedItem = inventory.ValueRW.EquippedItem;
            
            if(!state.EntityManager.HasComponent( equippedItem,typeof(HealthItemDesc) ))
                continue;

            QuickUseData quickData = new QuickUseData();
            bool quickUse = false;
            if ( state.EntityManager.HasComponent<QuickUseData>( equippedItem ) )
            {
                quickData = state.EntityManager.GetComponentData<QuickUseData>( equippedItem );
                quickUse = true;
            }
                
            if(!input.ValueRO.Shoot && !quickUse)
                continue;
            
            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( equippedItem );
            
            if ( healthItem.Type == HealthItemType.HealthKit )
            {
                if ( quickUse )
                {
                    //UseHealthKit(player,character, ref healthItem, quickData, ref state );
                    if ( quickData.PreviousEquipped != Entity.Null )
                    {
                        inventory.ValueRW.SwitchToBuffer = new EquippingData(quickData.PreviousEquipped);
                    }
                    inventory.ValueRW.EquippedItem = Entity.Null;

                }
                else
                {
                    UseHealthKit(player,character, ref healthItem, ref state );
                }
                
            }
            /*
            else if ( healthItem.Type == HealthItemType.Tourniquet )
            {
                if ( quickUse )
                {
                    UseTourniquet( player,character, ref healthItem, ref itemData, quickData, ref state );
                    if ( quickData.PreviousEquipped != Entity.Null )
                    {
                        inventory.ValueRW.SwitchToBuffer = new EquippingData(quickData.PreviousEquipped);
                    }
                    inventory.ValueRW.EquippedItem = Entity.Null;
                }
                else
                {
                    UseTourniquet( player,character, ref healthItem, ref itemData, ref state );
                }
                
            }
            */

            state.EntityManager.SetComponentData( equippedItem, healthItem );
            state.EntityManager.SetComponentData( equippedItem, itemData );

            if ( healthItem.CurrentCharges > 0 )
            {
                PlayerUIManager.Instance.UpdateItem( healthItem, itemData );

                if ( quickUse )
                {
                    if ( quickData.InHotBar )
                    {
                        ecb.RemoveComponent<QuickUseData>( equippedItem );
                    }
                    else
                    {
                        ecb.DestroyEntity( equippedItem );
                    }
                }
                
            }
            else
            {
                PlayerUIManager.Instance.RemoveItem( itemData.Key, true );
                ecb.AddComponent<DestroyOnUnequip>( equippedItem );
                //ecb.DestroyEntity( equippedItem );
            }
                
                
        }
    }

/*
    private void UseTourniquet(Entity player, RefRW<MyCharacterComponent> character, ref HealthItemDesc healthItem, ref CharacterItemData itemData, ref SystemState state)
    {
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        DynamicBuffer<CharacterLimb> body = state.EntityManager.GetBuffer<CharacterLimb>( player );

        int maxIndex = -1;
        float max = float.MinValue;
        for(int i = 0; i < body.Length; i++)
        {
            CharacterLimb limb = body[i];
            if(limb.Part == BodyPart.Chest || limb.Part == BodyPart.Head)
                continue;

            if ( limb.Bleed > math.EPSILON && limb.Bleed > max )
            {
                maxIndex = i;
                max = limb.Bleed;
            }
        }

        if ( maxIndex == -1 )
            return;

        ref CharacterLimb mostBleeding = ref body.ElementAt( maxIndex );
        for ( int i = 0; i < wounds.Length; i++ )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if ( wound.AffectedPart == mostBleeding.Part )
            {
                mostBleeding.Bleed -= wound.Bleed;
                wound.Bleed = 0;
            }
        }
        
        healthItem.CurrentCharges--;
        if ( healthItem.CurrentCharges <= 0 )
        {
            itemData.Quantity--;
            if ( itemData.Quantity > 0 )
            {
                healthItem.CurrentCharges = healthItem.MaxCharges;
            }
        }

    }
    
    private void UseTourniquet(Entity player, RefRW<MyCharacterComponent> character, ref HealthItemDesc healthItem, ref CharacterItemData itemData, QuickUseData quickData, ref SystemState state)
    {
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        DynamicBuffer<CharacterLimb> body = state.EntityManager.GetBuffer<CharacterLimb>( player );
        

        ref CharacterLimb mostBleeding = ref body.ElementAt( (int)quickData.Part );
        for ( int i = 0; i < wounds.Length; i++ )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if ( wound.AffectedPart == mostBleeding.Part )
            {
                mostBleeding.Bleed -= wound.Bleed;
                wound.Bleed = 0;
            }
        }
        
        healthItem.CurrentCharges--;
        if ( healthItem.CurrentCharges <= 0 )
        {
            itemData.Quantity--;
            if ( itemData.Quantity > 0 )
            {
                healthItem.CurrentCharges = healthItem.MaxCharges;
            }
        }

    }
    */
    
    private void UseHealthKit( Entity player, RefRW<CharacterStats> character, ref HealthItemDesc healthItem, ref SystemState state )
    {
        
        healthItem.HealTimer += SystemAPI.Time.DeltaTime*healthItem.HealRate;
        if ( healthItem.HealTimer < 1 )
            return;

        int healCharges = (int)healthItem.HealTimer;
        healthItem.HealTimer = 0;
        
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        
        for ( int i = wounds.Length - 1; i >= 0; i-- )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            HealResult healResult = wound.Heal(ref healthItem, healCharges);
            healCharges -= healResult.ChargesUsed;
            
            CharacterLimb limb = character.ValueRW.BaseStats.GetLimb( wound.AffectedPart );
            character.ValueRW.BaseStats.Health += limb.Heal( healResult.AmountHealed, healResult.BleedingHealed );
            character.ValueRW.BaseStats.SetLimb( limb );
                
            if ( wound.Healed )
            {
                PlayerUIManager.Instance.HealedWoundECS( i );
                wounds.RemoveAt( i );   
            }

            if ( healthItem.CurrentCharges <= 0 || healCharges <= 0 )
            {
                return;
            }
        }
    }
    
    /*
    private void UseHealthKit( Entity player, RefRW<MyCharacterComponent> character, ref HealthItemDesc healthItem, QuickUseData quickData, ref SystemState state )
    {
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        DynamicBuffer<CharacterLimb> body = state.EntityManager.GetBuffer<CharacterLimb>( player );
        
        for ( int i = wounds.Length - 1; i >= 0; i-- )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            
            if(wound.AffectedPart != quickData.Part)
                continue;
            
            float2 healResult = wound.Heal(ref healthItem);

            ref CharacterLimb limb = ref body.ElementAt( (int) wound.AffectedPart );
            character.ValueRW.Health += limb.Heal( healResult.x, healResult.y );
                
            if ( wound.Healed )
            {
                PlayerUIManager.Instance.HealedWoundECS( i );
                wounds.RemoveAt( i );   
            }

            if ( healthItem.CurrentCharges <= 0 )
            {
                return;
            }
        }
    }
    */

    private void AddWound(CharacterWound newWound,  DynamicBuffer<CharacterWound> wounds, RefRW<CharacterStats> character)
    {
        //int limbIndex = (int)newWound.AffectedPart;
        //ref CharacterLimb limb = ref body.ElementAt(limbIndex);
        CharacterLimb limb = character.ValueRW.BaseStats.GetLimb( newWound.AffectedPart );

        
        if ( limb.Destroyed )
        {
            
            for ( int i = 0; i < _bodyParts.Length; i++ )
            {
                BodyPart part = _bodyParts[i];
                if ( part == limb.Part )
                    continue;

                CharacterLimb spreadLimb = character.ValueRW.BaseStats.GetLimb( part );
                if(!spreadLimb.Destroyed)
                {
                    CharacterWound spreadWound = new CharacterWound(newWound, spreadLimb.Part );
                    PlayerUIManager.Instance.NewWoundECS(spreadWound);
                    character.ValueRW.BaseStats.Health -= spreadLimb.Damage( spreadWound );
                    character.ValueRW.BaseStats.SetLimb( spreadLimb );
                    wounds.Add( spreadWound );
                }
            }
            

            return;
        }
        PlayerUIManager.Instance.NewWoundECS(newWound);
        wounds.Add( newWound );
        character.ValueRW.BaseStats.Health -= limb.Damage( newWound );
        character.ValueRW.BaseStats.SetLimb( limb );
    }

}

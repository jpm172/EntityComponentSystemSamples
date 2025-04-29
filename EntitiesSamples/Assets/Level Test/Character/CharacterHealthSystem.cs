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
            

            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            //if ( healthItem.State == ItemState.Ready && (input.ValueRO.Shoot || IsQuickUse( equippedItem, ref state )) )
            if ( healthItem.State == ItemState.Ready && input.ValueRO.Shoot )
            {
                healthItem.TimerRemaining = healthItem.HealTime;
                healthItem.State = ItemState.Using;
                healthItem.HealData.TargetLimb = GetMostHurtLimb( character.ValueRO );
            }

            if ( inventory.ValueRO.Switching )
            {
                healthItem.State = ItemState.Ready;
                state.EntityManager.SetComponentData( equippedItem, healthItem );
            }
            
            if(healthItem.State == ItemState.Ready)
                continue;
            
            
            CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( equippedItem );
            
            if ( healthItem.Type == HealthItemType.HealthKit )
            {
                UseHealthKit(player,character, ref healthItem, ref state );
            }
            else if ( healthItem.Type == HealthItemType.Tourniquet )
            {
                UseTourniquet( player,character, ecb, ref healthItem, ref itemData, ref state );
            }
            

            state.EntityManager.SetComponentData( equippedItem, healthItem );
            state.EntityManager.SetComponentData( equippedItem, itemData );

            if ( healthItem.CurrentCharges > 0 )
            {
                PlayerUIManager.Instance.UpdateItem( healthItem, itemData );
            }
            else
            {
                PlayerUIManager.Instance.RemoveItem( itemData.Key, true );
                ecb.AddComponent<DestroyOnUnequip>( equippedItem );
            }
                
                
        }
    }

    private BodyPart GetMostHurtLimb(CharacterStats stats)
    {
        BodyPart result = BodyPart.Chest;
        float maxDamage = Single.NegativeInfinity;
        foreach ( BodyPart part in _bodyParts )
        {
            CharacterLimb limb = stats.BaseStats.GetLimb( part );
            if ( limb.MissingHealth > maxDamage )
            {
                result = part;
                maxDamage = limb.MissingHealth;
            }
        }

        return result;
    }
    
    private bool IsQuickUse(Entity item, ref SystemState state)
    {
        return state.EntityManager.HasComponent<UseOnEquip>( item );
    }


    private void UseTourniquet(Entity player, RefRW<CharacterStats> stats, EntityCommandBuffer ecb,
        ref HealthItemDesc healthItem, ref CharacterItemData itemData, ref SystemState state)
    {
        
        healthItem.TimerRemaining -= SystemAPI.Time.DeltaTime;
        if ( healthItem.TimerRemaining > 0 )
            return;
        
        
        
        
        healthItem.TimerRemaining = 0;
        healthItem.State = ItemState.Ready;

        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );

        BodyPart maxBleedPart = BodyPart.Chest;
        float max = float.MinValue;

        foreach ( BodyPart part in _bodyParts )
        {
            if(part == BodyPart.Chest || part == BodyPart.Head)
                continue;
            CharacterLimb limb = stats.ValueRO.BaseStats.GetLimb( part );
            if ( limb.Bleed > math.EPSILON && limb.Bleed > max )
            {
                maxBleedPart = part;
                max = limb.Bleed;
            }
        }

        if ( maxBleedPart == BodyPart.Chest )
            return;

        Entity effect = ecb.CreateEntity();
        BodyStatusEffect se = new BodyStatusEffect
        {
            AffectedLimb = maxBleedPart,
            AffectedStat = BodyStatType.Condition,
            ModType = StatModType.Multiply,
            Value = -1
        };
        StatusEffectInfo info = new StatusEffectInfo
            {Type = StatusEffectType.BodyStats, Quality = StatusEffectQuality.Neutral, ID = StatsuEffectID.Tourniquet};
        
        EntityArchetype ea = new EntityArchetype();
        
        ecb.AddComponent( effect, se  );
        ecb.AddComponent( effect, info  );
        ecb.AddComponent( effect, new StatusEffectTimer(5)  );
        ecb.AppendToBuffer( player, new StatusEffect{EffectEntity = effect} );
        ecb.AddComponent<InitializeStatusEffect>( effect );


        /*
        //check to see if already has tourniquet 
        DynamicBuffer<StatusEffect> effects = state.EntityManager.GetBuffer<StatusEffect>( player );
        foreach ( StatusEffect effect in effects )
        {
            StatusEffectInfo info = state.EntityManager.GetComponentData<StatusEffectInfo>( effect.EffectEntity );
            if(info.ID != StatsuEffectID.Tourniquet)
                continue;

            BodyStatusEffect bodyEffect = state.EntityManager.GetComponentData<BodyStatusEffect>( effect.EffectEntity );
            if ( bodyEffect.AffectedLimb == maxBleedPart )
            {
                //todo: prevent using item
            }
        }
        */
        

        CharacterLimb mostBleeding = stats.ValueRO.BaseStats.GetLimb( maxBleedPart );
        for ( int i = 0; i < wounds.Length; i++ )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if ( wound.AffectedPart == mostBleeding.Part )
            {
                mostBleeding.Bleed -= wound.Bleed;
                wound.Bleed = 0;
            }
        }
        
        stats.ValueRW.BaseStats.SetLimb( mostBleeding );
        
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


    /*
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
        
        healthItem.TimerRemaining -= SystemAPI.Time.DeltaTime;
        if ( healthItem.TimerRemaining > 0 )
            return;

        int healCharges = healthItem.ChargesPerHeal;
        healthItem.TimerRemaining = 0;
        healthItem.State = ItemState.Ready;
        
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        
        for ( int i = wounds.Length - 1; i >= 0; i-- )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if(wound.AffectedPart != healthItem.HealData.TargetLimb)
                continue;
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
    private void UseHealthKit( Entity player, RefRW<CharacterStats> character, ref HealthItemDesc healthItem, ref SystemState state )
    {
        
        healthItem.TimerRemaining -= SystemAPI.Time.DeltaTime;
        if ( healthItem.TimerRemaining > 0 )
            return;

        int healCharges = healthItem.ChargesPerHeal;
        healthItem.TimerRemaining = 0;
        healthItem.State = ItemState.Ready;
        
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
    */
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

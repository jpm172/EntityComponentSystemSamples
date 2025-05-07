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

            ItemStateInfo itemState = state.EntityManager.GetComponentData<ItemStateInfo>( equippedItem );
            HealthItemDesc healthItem = state.EntityManager.GetComponentData<HealthItemDesc>( equippedItem );
            //if ( healthItem.State == ItemState.Ready && (input.ValueRO.Click || IsQuickUse( equippedItem, ref state )) )
            
            /*
            if ( healthItem.State != ItemState.Ready && input.ValueRO.Click )
            {
                healthItem.State = ItemState.Ready;
                state.EntityManager.SetComponentData( equippedItem, healthItem );
                continue;
            }
            
            if ( healthItem.State == ItemState.Ready && input.ValueRO.Click )
            {
                healthItem.TimerRemaining = healthItem.HealTime;
                healthItem.State = ItemState.Start;
            }
            */

            
            
            if ( inventory.ValueRO.Switching )
            {
                healthItem.State = ItemState.Ready;
                state.EntityManager.SetComponentData( equippedItem, healthItem );
            }
            
            if(itemState.State == ItemState.Ready)
                continue;

            CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( equippedItem );
            
            if ( healthItem.Type == HealthItemType.HealthKit )
            {
                UseHealthKit(player,equippedItem, character, ref healthItem, ref state );
            }
            else if ( healthItem.Type == HealthItemType.Tourniquet )
            {
                UseTourniquet( player,  character, ecb, ref healthItem, ref itemData, ref state );
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

    private bool HasHurtLimb(CharacterStats stats, out BodyPart mostHurtLimb)
    {
        mostHurtLimb = BodyPart.Chest;
        bool result = false;
        float maxDamage = Single.NegativeInfinity;
        
        foreach ( BodyPart part in _bodyParts )
        {
            CharacterLimb limb = stats.BaseStats.GetLimb( part );
            if ( !limb.Healthy && limb.MissingHealth > maxDamage )
            {
                mostHurtLimb = part;
                result = true;
                maxDamage = limb.MissingHealth;
            }
        }

        return result;
    }
    
    private bool IsQuickUse(Entity item, ref SystemState state)
    {
        return state.EntityManager.HasComponent<QuickUseData>( item );
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

        AddTourniquetStatus( ecb, maxBleedPart, player );


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


    private void AddTourniquetStatus(EntityCommandBuffer ecb, BodyPart affectedLimb, Entity player)
    {
        Entity effect = ecb.CreateEntity();
        BodyStatusEffect se = new BodyStatusEffect
        {
            AffectedLimb = affectedLimb,
            AffectedStat = BodyStatType.BleedMod,
            ModType = StatModType.Multiply,
            Value = -10
        };
        StatusEffectInfo info = new StatusEffectInfo
            {IsParent = true, Type = StatusEffectType.BodyStats, Quality = StatusEffectQuality.Neutral, ID = StatsuEffectID.Tourniquet};
        
        
        ecb.AddComponent( effect, se  );
        ecb.AddComponent( effect, info  );
        ecb.AddComponent( effect, new StatusEffectTimer(5)  );
        ecb.AppendToBuffer( player, new StatusEffect{EffectEntity = effect} );
        ecb.AddComponent<InitializeStatusEffect>( effect );
        
        effect = ecb.CreateEntity();
        se = new BodyStatusEffect
        {
            AffectedLimb = affectedLimb,
            AffectedStat = BodyStatType.Condition,
            ModType = StatModType.Add,
            Value = -0.1f
        };
        info = new StatusEffectInfo
            {IsParent = false, Type = StatusEffectType.BodyStats, Quality = StatusEffectQuality.Debuff, ID = StatsuEffectID.Tourniquet};
        
        
        ecb.AddComponent( effect, se  );
        ecb.AddComponent( effect, info  );
        ecb.AddComponent( effect, new StatusEffectTimer(5)  );
        ecb.AppendToBuffer( player, new StatusEffect{EffectEntity = effect} );
        ecb.AddComponent<InitializeStatusEffect>( effect );
        
        
    }
    
    

    private void UseHealthKit( Entity player, Entity equippedItem, RefRW<CharacterStats> character, ref HealthItemDesc healthItem, ref SystemState state )
    {
        
        healthItem.TimerRemaining -= SystemAPI.Time.DeltaTime;
        if ( healthItem.TimerRemaining > 0 )
            return;

        HealthKitInfo kitInfo = state.EntityManager.GetComponentData<HealthKitInfo>( equippedItem );
        ItemStateInfo itemState = state.EntityManager.GetComponentData<ItemStateInfo>( equippedItem );
        
        
        if ( itemState.State == ItemState.Start )
        {
            itemState.State = ItemState.Using;
            
            if ( IsQuickUse( equippedItem, ref state ) )
            {
                QuickUseData quickData = state.EntityManager.GetComponentData<QuickUseData>( equippedItem );
                kitInfo.UseType = HealthKitUseType.HealLimb;
                kitInfo.TargetLimb = quickData.Part;
            }
            else
            {
                kitInfo.UseType = HealthKitUseType.HealAll;
            }
            
            state.EntityManager.SetComponentData( equippedItem, kitInfo );
        }

        int healCharges = healthItem.ChargesPerHeal;
        healthItem.TimerRemaining = healthItem.HealTime;

        BodyPart targetLimb = kitInfo.TargetLimb;

        if ( kitInfo.UseType == HealthKitUseType.HealAll )
        {
            if ( !HasHurtLimb( character.ValueRO, out targetLimb ) )
            {
                itemState.State = ItemState.Finished;
                state.EntityManager.SetComponentData( equippedItem, itemState );
                return;
            }
        }
        else if ( kitInfo.UseType == HealthKitUseType.HealLimb )
        {
            if ( character.ValueRO.BaseStats.GetLimb( targetLimb ).Healthy )
            {
                itemState.State = ItemState.Finished;
                state.EntityManager.SetComponentData( equippedItem, itemState );
                return;
            }
        }
            
        
        DynamicBuffer<CharacterWound> wounds = state.EntityManager.GetBuffer<CharacterWound>( player );
        
        for ( int i = wounds.Length - 1; i >= 0; i-- )
        {
            ref CharacterWound wound = ref wounds.ElementAt( i );
            if(wound.AffectedPart != targetLimb)
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

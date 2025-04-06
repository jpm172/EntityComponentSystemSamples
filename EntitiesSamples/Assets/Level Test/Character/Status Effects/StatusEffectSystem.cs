using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(StatusEffectsGroup))]
[BurstCompile]
public partial struct StatusEffectSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    [BurstCompile]
    public void OnUpdate( ref SystemState state )
    {
        //SystemAPI.Query<>().WithOptions( EntityQueryOptions.FilterWriteGroup )
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        
        foreach ( var (statusEffects, baseStats, totalStats) in
            SystemAPI.Query<DynamicBuffer<StatusEffect>, RefRO<BaseStats>, RefRW<TotalStats>>() )
        {
            CharacterStats modifiedStats = baseStats.ValueRO.Stats;
            for ( int i = statusEffects.Length - 1; i >= 0; i-- )
            {
                StatusEffect baseEffect = statusEffects[i];
                StatusEffectInfo info =
                    state.EntityManager.GetComponentData<StatusEffectInfo>( baseEffect.EffectEntity );
                if ( info.Remove )
                {
                    ecb.DestroyEntity( baseEffect.EffectEntity );
                    statusEffects.RemoveAt( i );
                    continue;
                }
                
                if ( baseEffect.Type == StatusEffectType.BasicStats )
                {
                    BasicStatStatusEffect effect =
                        state.EntityManager.GetComponentData<BasicStatStatusEffect>( baseEffect.EffectEntity );
                    
                    modifiedStats = effect.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                }
            }
            //Debug.Log( $"{statusEffects.Capacity}, {statusEffects.Length}" ); //TrimExcess
            totalStats.ValueRW.Stats = modifiedStats;
        }
    }
}


[UpdateInGroup(typeof(StatusEffectsGroup), OrderFirst = true)]
public partial struct StatusEffectTimerSystem :ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        foreach ( var (effectInfo, effectTimer) in
            SystemAPI.Query<RefRW<StatusEffectInfo>, RefRW<StatusEffectTimer>>() )
        {
            effectTimer.ValueRW.TimeRemaining -= SystemAPI.Time.DeltaTime;
            if(effectTimer.ValueRW.TimeRemaining <= 0)
                effectInfo.ValueRW.Remove = true;
        }
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class StatusEffectsGroup: ComponentSystemGroup
{}

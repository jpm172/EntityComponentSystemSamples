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
        //state.RequireForUpdate<StatusEffectInfo>();
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
                    PlayerUIManager.Instance.RemovedStatusEffectECS( i );
                    statusEffects.RemoveAt( i );
                    continue;
                }
                
                if ( info.Type == StatusEffectType.BasicStats )
                {
                    BasicStatStatusEffect effect =
                        state.EntityManager.GetComponentData<BasicStatStatusEffect>( baseEffect.EffectEntity );
                    
                    modifiedStats = effect.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                }
                else if ( info.Type == StatusEffectType.BodyStats )
                {
                    BodyStatusEffect effect = state.EntityManager.GetComponentData<BodyStatusEffect>( baseEffect.EffectEntity );
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
        state.RequireForUpdate<StatusEffectTimer>();
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

[UpdateInGroup(typeof(StatusEffectsGroup), OrderFirst = true)]
public partial struct StatusEffectBodyListenerSystem :ISystem
{
    public void OnCreate( ref SystemState state )
    {
        state.RequireForUpdate<StatusEffectBodyListener>();
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        foreach ( var (effectInfo, bodyListener) in
            SystemAPI.Query<RefRW<StatusEffectInfo>, RefRW<StatusEffectBodyListener>>() )
        {
            if(effectInfo.ValueRW.Remove)
                continue;

            DynamicBuffer<CharacterLimb> body =
                state.EntityManager.GetBuffer<CharacterLimb>( bodyListener.ValueRO.Owner );

            CharacterLimb limb = body[(int) bodyListener.ValueRO.TargetLimb];
            effectInfo.ValueRW.Remove = bodyListener.ValueRO.CheckLimb( limb );


        }
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class StatusEffectsGroup: ComponentSystemGroup
{}

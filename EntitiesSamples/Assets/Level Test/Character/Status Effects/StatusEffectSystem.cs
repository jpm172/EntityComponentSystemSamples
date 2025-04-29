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

        InitializeStatusEffects(ref state);



        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        
        foreach ( var (statusEffects, stats) in
            SystemAPI.Query<DynamicBuffer<StatusEffect>, RefRW<CharacterStats>>() )
        {
            stats.ValueRW.TotalStats = stats.ValueRW.BaseStats;
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
                    
                    stats.ValueRW = effect.ApplyEffect( stats.ValueRW );
                }
                else if ( info.Type == StatusEffectType.BodyStats )
                {
                    BodyStatusEffect effect = state.EntityManager.GetComponentData<BodyStatusEffect>( baseEffect.EffectEntity );
                    stats.ValueRW = effect.ApplyEffect( stats.ValueRW );
                }
            }
            //Debug.Log( $"{statusEffects.Capacity}, {statusEffects.Length}" ); //TrimExcess
            //stats.ValueRW = modifiedStats;
        }
    }


    private void InitializeStatusEffects(ref SystemState state)
    {
        foreach ( var (info, init, entity) in
            SystemAPI.Query<RefRO<StatusEffectInfo>, EnabledRefRW<InitializeStatusEffect>>().WithEntityAccess())
        {
            if ( init.ValueRW )
            {
                PlayerUIManager.Instance.AddedStatusEffect( entity );
                init.ValueRW = false;
            }
            
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

            CharacterStats stats = state.EntityManager.GetComponentData<CharacterStats>( bodyListener.ValueRO.Owner );
            
            effectInfo.ValueRW.Remove = bodyListener.ValueRO.CheckLimb( stats.TotalStats.GetLimb( bodyListener.ValueRO.TargetLimb ) );


        }
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class StatusEffectsGroup: ComponentSystemGroup
{}

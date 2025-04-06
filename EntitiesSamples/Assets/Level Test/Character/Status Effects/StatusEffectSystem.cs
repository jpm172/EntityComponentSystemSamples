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
    
        //float startTime = Time.realtimeSinceStartup; 
        /*
        foreach ( var (statusEffects, baseStats, totalStats) in
            SystemAPI.Query<DynamicBuffer<StatusEffect>, RefRO<BaseStats>, RefRW<TotalStats>>() )
        {
            CharacterStats modifiedStats = baseStats.ValueRO.Stats;
            for ( int i = statusEffects.Length - 1; i >= 0; i-- )
            {
                StatusEffect baseEffect = statusEffects[i];

                if ( baseEffect.Type == StatusEffectType.BasicStats )
                {
                    BasicStatStatusEffect effect =
                        state.EntityManager.GetComponentData<BasicStatStatusEffect>( baseEffect.EffectEntity );
                    
                    modifiedStats = effect.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                    
                }
                //modifiedStats = d.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                
            }
            totalStats.ValueRW.Stats = modifiedStats;
        }
        */
        
        //Debug.Log( "done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );
        
        
        //float startTime = Time.realtimeSinceStartup; 
        foreach ( var (statusEffects, baseStats, totalStats) in
            SystemAPI.Query<DynamicBuffer<TimedStatusEffect>, RefRO<BaseStats>, RefRW<TotalStats>>() )
        {
            CharacterStats modifiedStats = baseStats.ValueRO.Stats;
            for ( int i = statusEffects.Length - 1; i >= 0; i-- )
            {
                ref TimedStatusEffect d = ref statusEffects.ElementAt( i );
                modifiedStats = d.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                /*
                d.Timer -= SystemAPI.Time.DeltaTime;
                if ( d.Timer <= 0 )
                {
                    statusEffects.RemoveAt( i );
                }
                */
                
            }
            totalStats.ValueRW.Stats = modifiedStats;
        }
        //Debug.Log( "done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );
        
    }
}

[UpdateInGroup(typeof(StatusEffectsGroup))]
public partial struct BodyStatusEffectSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {

    }

    public void OnUpdate( ref SystemState state )
    {
      
    }
}


[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class StatusEffectsGroup: ComponentSystemGroup
{}

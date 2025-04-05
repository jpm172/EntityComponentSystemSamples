using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(StatusEffectsGroup))]
public partial struct StatusEffectSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        //SystemAPI.Query<>().WithOptions( EntityQueryOptions.FilterWriteGroup )
    
        foreach ( var (statusEffects, baseStats, totalStats) in
            SystemAPI.Query<DynamicBuffer<TimedStatusEffect>, RefRO<BaseStats>, RefRW<TotalStats>>() )
        {
            CharacterStats modifiedStats = baseStats.ValueRO.Stats;
            for ( int i = statusEffects.Length - 1; i >= 0; i-- )
            {
                ref TimedStatusEffect d = ref statusEffects.ElementAt( i );
                modifiedStats = d.ApplyEffect( baseStats.ValueRO.Stats, modifiedStats );
                d.Timer -= SystemAPI.Time.DeltaTime;
                if ( d.Timer <= 0 )
                {
                    statusEffects.RemoveAt( i );
                }
            }
            totalStats.ValueRW.Stats = modifiedStats;
        }
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

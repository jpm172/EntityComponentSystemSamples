using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

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
        foreach ( var (debuffs, baseStats, totalStats) in
            SystemAPI.Query<DynamicBuffer<CharacterDebuff>, RefRO<BaseStats>, RefRW<TotalStats>>() )
        {
            totalStats.ValueRW.Stats = baseStats.ValueRO.Stats;
        }
    }
}

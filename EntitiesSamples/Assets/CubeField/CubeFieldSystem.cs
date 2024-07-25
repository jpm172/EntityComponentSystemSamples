using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct CubeFieldSystem : ISystem
{
    [BurstCompile]
    public void OnCreate( ref SystemState state )
    {
        state.RequireForUpdate<CubeFieldConfig>();
    }
    
    [BurstCompile]
    public void OnDestroy( ref SystemState state )
    {
        
    }
    
    [BurstCompile]
    public void OnUpdate( ref SystemState state )
    {
        state.Enabled = false;//only run once
        CubeFieldConfig config = SystemAPI.GetSingleton<CubeFieldConfig>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        
        for ( int x = 0; x < config.dimensions.x; x++ )
        {
            for ( int y = 0; y < config.dimensions.y; y++ )
            {
                Entity spawnedEntity = ecb.Instantiate( config.cubePrefab );
                ecb.SetComponent( spawnedEntity, new LocalTransform
                {
                    Position = new float3( x, y, 0 ) *1.25f,
                    Rotation = quaternion.identity,
                    Scale = 1
                } );
            }
        }
        
        ecb.Playback( state.EntityManager );
        
    }
}

/*
public partial class CubeFieldSystem : SystemBase
{
    //
    protected override void OnCreate()
    {
        RequireForUpdate<CubeFieldConfig>();
    }//
    
    protected override void OnUpdate()
    {
        if ( Input.GetKeyDown( KeyCode.Space ) )
        {
            CubeFieldConfig config = SystemAPI.GetSingleton<CubeFieldConfig>();
            float3 spacing = new float3(.1f,.1f, .1f);
                
            for ( int x = 0; x < config.dimensions.x; x++ )
            {
                for ( int y = 0; y < config.dimensions.y; y++ )
                {
                    Entity spawnedEntity = EntityManager.Instantiate( config.cubePrefab );
                    EntityManager.SetComponentData( spawnedEntity, new LocalTransform
                    {
                        Position = new float3( x, y, 0 ) *1.25f,
                        Rotation = quaternion.identity,
                        Scale = 1
                    } );
                }
            }
            
            
        }
    }
}*/
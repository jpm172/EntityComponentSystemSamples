using System.Collections;
using System.Collections.Generic;
using Boids;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public partial struct CubeSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }


    public void OnUpdate( ref SystemState state )
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        CubeJob job = new CubeJob
        {
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            deltaTime = SystemAPI.Time.DeltaTime
        };
        job.Schedule();
    }
}

public partial struct CubeJob : IJobEntity
{
    public float deltaTime;
    public EntityCommandBuffer ECB;
    
    public void Execute(Entity entity, ref CubeData cubeData, ref LocalTransform transform, EnabledRefRO<EntityCollider> col )
    {
        
        transform = transform.RotateY( math.radians(cubeData.speed * deltaTime) );
        ECB.DestroyEntity( entity );
        
    }
}

/*
public partial class CubeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ( var (cubeData, tranform) in SystemAPI.Query<RefRO<CubeData>, RefRW<LocalTransform>>().WithDisabled<EntityCollider>() )
        {
            tranform.ValueRW = tranform.ValueRO.RotateY( math.radians(cubeData.ValueRO.speed * deltaTime) );
            
        }
    }
}
*/

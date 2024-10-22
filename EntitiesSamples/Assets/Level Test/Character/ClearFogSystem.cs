using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ClearFogSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        
        new ClearFogJob()
        {

        }.Schedule();
        
        
    }
}

//[BurstCompile]
public partial struct ClearFogJob : IJobEntity
{

    private void Execute( ref LocalTransform transform, in ClearFogComponent clearFog )
    {
        
        


    }

    
}

using System.Collections;
using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EyeSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        new ClearFogJob()
        {
            PhysicsWorld = physicsWorld
        }.Schedule();
        
        
    }
}

//[BurstCompile]
public partial struct ClearFogJob : IJobEntity
{

    public PhysicsWorldSingleton PhysicsWorld;
    
    private void Execute( ref LocalTransform transform, in EyeComponent eye )
    {
        int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        float degreesPerStep = eye.FOV / stepCount;

        Debug.DrawLine( transform.Position, transform.Position + transform.Right()*eye.ViewDistance, Color.red, .1f );
        //Debug.DrawLine( transform.Position, transform.Position + transform.RotateZ( eye.FOV*math.TORADIANS ).Right(), Color.blue, .1f );
        
        
        for ( int i = 0; i <= stepCount; i++ )
        {
            float angle = -( eye.FOV / 2 ) + degreesPerStep * i;
            float3 endRay = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;
            Debug.DrawLine( transform.Position, transform.Position + endRay, Color.blue, .1f );
        }
    }

    
}

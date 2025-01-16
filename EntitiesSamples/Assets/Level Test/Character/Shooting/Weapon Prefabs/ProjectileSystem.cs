using System.Collections;
using System.Collections.Generic;
using Unity.Physics.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using SphereCollider = Unity.Physics.SphereCollider;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileSystem : ISystem
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
        var deltaTime = SystemAPI.Time.DeltaTime;
        new ProjectileMoveJob
        {
            DeltaTime = deltaTime,
            PhysicsWorld = physicsWorld
        }.Schedule();
    }
}

public partial struct ProjectileMoveJob : IJobEntity
{
    
    public float DeltaTime;
    public PhysicsWorldSingleton PhysicsWorld;
    private static readonly float AngleAdjust = math.radians( 90 );
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };

    private void Execute( ref LocalTransform transform, ref ProjectileInfo projectileInfo,  in PhysicsCollider col )
    {
        /*
        float2 targetMove =  projectileInfo.Velocity.xy * DeltaTime;
        float3 vel = new float3( targetMove, 0 );
        
        float3 result = CollideAndBounce( col, vel, transform.Position, transform, vel, out float2 newVel );
        float drag = ( 1 - DeltaTime * projectileInfo.Drag );
        projectileInfo.Velocity.xy = (newVel* drag)/DeltaTime;

        transform.Position.xy += result.xy;
        */


        float3 vel = projectileInfo.Velocity * DeltaTime;
        
        float3 result = CollideAndBounce3D( col, vel, transform.Position, transform, out float3 newVel );
        
        float drag = ( 1 - DeltaTime * projectileInfo.Drag );
        projectileInfo.Velocity.xy = (newVel.xy* drag)/DeltaTime;
        projectileInfo.Velocity.z = newVel.z / DeltaTime;
        
        transform.Position += result;
        
        //-z is going up, +z is falling down
    }
    
    private float3 CollideAndBounce3D( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform,  out float3 newVel )
    {
        float skinWidth = .1f;
        newVel = vel;

        float dist = math.length( vel ) + skinWidth;
        
        float radius = col.Value.As<SphereCollider>().Radius * transform.Scale;
        
        float3 xyVel = new float3(vel.xy, 0);
        float3 xyPos = new float3(pos.xy, 0);

        newVel.z += 4 * (DeltaTime/6);
        //bounce off the ground
        if ( pos.z + vel.z >= 0 )
        {
            //vel.xy *= new float2(.8f, .8f);
            vel.z = -vel.z * .75f;
            newVel.z = vel.z;
            
        }

        if ( PhysicsWorld.SphereCast( xyPos, radius - skinWidth, math.normalizesafe( xyVel ), dist, out ColliderCastHit hit, CastFilter ) )
        {
            float3 snapToSurface = math.normalizesafe( xyVel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth  );

            if(math.length( snapToSurface ) <= skinWidth)
                snapToSurface = float3.zero;

            newVel.xy = math.reflect( vel.xy, hit.SurfaceNormal.xy );

            snapToSurface.z = vel.z;
            
            return snapToSurface;
        }
        
        return vel;
        
    }


    private float3 CollideAndBounce( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform,  float3 velInit, out float2 newVel )
    {
        float skinWidth = .1f;
        newVel = velInit.xy;

        float dist = math.length( vel ) + skinWidth;
        
        float radius = col.Value.As<SphereCollider>().Radius * transform.Scale;
        
        if ( PhysicsWorld.SphereCast( pos, radius - skinWidth, math.normalizesafe( vel ), dist, out ColliderCastHit hit, CastFilter ) )
        {
            //float3 snapToSurface = math.normalizesafe( vel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth );
            float3 snapToSurface = math.normalizesafe( vel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth  );

            if(math.length( snapToSurface ) <= skinWidth)
                snapToSurface = float3.zero;

            newVel = math.reflect( velInit.xy, hit.SurfaceNormal.xy );

            //newVel = new float2(0,0);
            float3 bounceOffset = new float3(  newVel.xy , 0 );
            
            return snapToSurface;
        }
        
        return vel;
        
    }

    
    
    public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {
        return vector - math.project(vector, planeNormal);
    }
    
}

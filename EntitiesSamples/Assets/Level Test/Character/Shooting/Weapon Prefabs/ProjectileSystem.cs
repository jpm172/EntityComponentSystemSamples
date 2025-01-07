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
        //rotate the character to look at the mouse
        //float3 forward = input.AimPosition - transform.Position;
        /*
        quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward );
        
        transform.Rotation = rotation;
        transform = transform.RotateZ( AngleAdjust );

        float2 targetMove = input.MoveInput * attributes.MovementSpeed * DeltaTime;
        float3 vel = new float3( targetMove, 0 );
        */
        
        float2 targetMove =  projectileInfo.Velocity.xy * DeltaTime;
        float3 vel = new float3( targetMove, 0 );

        // result = CollideAndSlide( col, vel, transform.Position, transform, 0, vel, out float2 newVel );
        float3 result = CollideAndBounce( col, vel, transform.Position, transform, vel, out float2 newVel );
        
        float dragForceMagnitude = math.pow(math.length(newVel), 2) * projectileInfo.Drag ; // The variable you’re talking about
        float2 dragForceVector = dragForceMagnitude * -math.normalizesafe(newVel);
        
        //projectileInfo.Velocity.xy = (newVel+dragForceVector)/DeltaTime;
        projectileInfo.Velocity.xy = (newVel)/DeltaTime;

        transform.Position.xy += result.xy;
    }


    private float3 CollideAndBounce( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform,  float3 velInit, out float2 newVel )
    {
        int maxDepth = 5;
        float skinWidth = .1f;
        newVel = velInit.xy;

        float dist = math.length( vel ) + skinWidth;
        
        float radius = col.Value.As<SphereCollider>().Radius * .25f;

        Debug.DrawLine( pos, pos + math.normalizesafe( vel ), Color.red, .1f  );
        //if ( PhysicsWorld.CastCollider( cast, out ColliderCastHit hit ) )
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

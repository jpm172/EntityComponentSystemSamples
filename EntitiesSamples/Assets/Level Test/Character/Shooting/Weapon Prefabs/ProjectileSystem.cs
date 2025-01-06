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
        projectileInfo.Velocity.xy = newVel/DeltaTime;

        transform.Position.xy += result.xy;
    }


    private float3 CollideAndBounce( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform,  float3 velInit, out float2 newVel )
    {
        int maxDepth = 5;
        float skinWidth = 0;
        newVel = velInit.xy;

        float dist = math.length( vel ) + skinWidth;

        //float radius = col.Value.Value.CalculateAabb().Extents.x / 2;
        float radius = col.Value.As<SphereCollider>().Radius;

        //if ( PhysicsWorld.CastCollider( cast, out ColliderCastHit hit ) )
        if ( PhysicsWorld.SphereCast( pos, radius - skinWidth, math.normalizesafe( vel ), dist, out ColliderCastHit hit, CastFilter ) )
        {
            float3 snapToSurface =
                math.normalizesafe( vel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth );
            
            float3 leftOver = vel - snapToSurface;

            if(math.length( snapToSurface ) <= skinWidth)
                snapToSurface = float3.zero;
            
            float mag = math.length( leftOver );

            leftOver =  math.normalizesafe(ProjectOnPlane( leftOver, hit.SurfaceNormal ));
            leftOver *= mag;
            newVel = math.reflect( velInit.xy, hit.SurfaceNormal.xy );
            
            // steep slope/wall

            float scale = 1 - math.dot( math.normalizesafe( hit.SurfaceNormal.xy ),
                              -math.normalizesafe( velInit.xy ) );

            leftOver *= scale;
            
            //
            
            
            return snapToSurface;
        }
        
        return vel;
        
    }

    private float3 CollideAndSlide( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform, int depth, float3 velInit )
    {
        int maxDepth = 5;
        float skinWidth = .0625f;
        
        //Debug.Log( pos );
        if ( depth >= maxDepth )
        {
            return float3.zero;
        }
        

        float dist = math.length( vel ) + skinWidth;

        //float radius = col.Value.Value.CalculateAabb().Extents.x / 2;
        float radius = col.Value.As<SphereCollider>().Radius;

        //if ( PhysicsWorld.CastCollider( cast, out ColliderCastHit hit ) )
        if ( PhysicsWorld.SphereCast( pos, radius - skinWidth, math.normalizesafe( vel ), dist, out ColliderCastHit hit, CastFilter ) )
        {
            float3 snapToSurface =
                math.normalizesafe( vel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth );
            
            float3 leftOver = vel - snapToSurface;

            if(math.length( snapToSurface ) <= skinWidth)
                snapToSurface = float3.zero;
            
            float mag = math.length( leftOver );

            leftOver =  math.normalizesafe(ProjectOnPlane( leftOver, hit.SurfaceNormal ));
            leftOver *= mag;
            
            
            // steep slope/wall

            float scale = 1 - math.dot( math.normalizesafe( hit.SurfaceNormal.xy ),
                              -math.normalizesafe( velInit.xy ) );

            leftOver *= scale;
            
            //
            
            
            return snapToSurface + CollideAndSlide( col, leftOver, pos + snapToSurface, transform, depth + 1, velInit );
        }
        
        return vel;
    }
    
    public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {
        return vector - math.project(vector, planeNormal);
    }
    
    private bool PhysicsCheck(float2 input, LocalTransform transform, PhysicsCollider col, float2 end, out ColliderCastHit hit, out NativeList<ColliderCastHit> castHits)
    {

        /*
        float3 offset = new float3(input.x, input.y, 0)/GameSettings.PixelsPerUnit;
        ColliderCastInput cast = new ColliderCastInput(col.Value, transform.Position + offset, transform.Position + new float3(end.x, end.y, 0),
            transform.Rotation);
            */
        
        ColliderCastInput cast = new ColliderCastInput(col.Value, transform.Position, transform.Position + new float3(end.x, end.y, 0),
            transform.Rotation);
        
        castHits = new NativeList<ColliderCastHit>(Allocator.Temp);
        bool result = PhysicsWorld.CastCollider( cast, ref castHits );

        PhysicsWorld.CastCollider( cast, out hit );
        
        return result;
    }

    private bool GetClosestPoint( LocalTransform transform, PhysicsCollider col, ColliderCastHit hit, NativeList<ColliderCastHit> castHits, out RaycastHit rayHit, out float2 adjust )
    {
        uint mask = 1 << 6;
        mask = ~mask;
        adjust = new float2();
        bool result = false;

        CollisionFilter filter = new CollisionFilter
        {
            CollidesWith = mask,
            BelongsTo = mask
        };
        
        Debug.Log( castHits.Length );
        foreach ( ColliderCastHit cHit in castHits )
        {
            RaycastInput rayInput = new RaycastInput
            {
                Start = transform.Position,
                End = cHit.Position + (cHit.Position - transform.Position),
                Filter = filter
            };

            if ( PhysicsWorld.CastRay( rayInput, out rayHit ) )
            {
                result = true;
                adjust += ( cHit.Position - rayHit.Position ).xy;
            } 
        }
        

        rayHit = new RaycastHit(); //temp debug
        
        /*
        NativeList<RaycastHit> hits = new NativeList<RaycastHit>(Allocator.Temp);
        PhysicsWorld.CastRay( rayInput, ref hits );
        foreach ( RaycastHit rHit in hits )
        {
            adjust += ( hit.Position - rHit.Position ).xy;
        }
        
        hits.Dispose();
        */
        if ( result )
        {
            //Debug.DrawLine( rayInput.Start, rayHit.Position, Color.blue, .1f );
            //Debug.Log( rayHit.Fraction + ", " + transform.Position );
        }

        return result;

    }
}

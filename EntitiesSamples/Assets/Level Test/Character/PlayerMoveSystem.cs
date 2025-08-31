using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using RaycastHit = Unity.Physics.RaycastHit;

//[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
public partial struct PlayerMoveSystem : ISystem
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
        new PlayerMoveJob
        {
            DeltaTime = deltaTime,
            PhysicsWorld = physicsWorld
        }.Schedule();
        
        
    }
}

[BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    
    public float DeltaTime;
    public PhysicsWorldSingleton PhysicsWorld;
    private static readonly float AngleAdjust = math.radians( 90 );
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };

    private void Execute( ref LocalTransform transform, in PlayerInputs input, CharacterStats stats, PhysicsCollider col )
    {
        //rotate the character to look at the mouse
        float3 forward = input.AimPosition - transform.Position;
        quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward );
        
        transform.Rotation = rotation;
        transform = transform.RotateZ( AngleAdjust );

        float finalSpeed = math.clamp( stats.TotalStats.MoveSpeed * stats.TotalStats.LegsCondition(), 1, 10 );
        
        float2 targetMove = input.MoveInput * finalSpeed * DeltaTime;
        float3 vel = new float3( targetMove, 0 );

        float3 result = CollideAndSlide( col, vel, transform.Position, transform, 0, vel );

        transform.Position.xy += result.xy;
    }


    private float3 CollideAndSlide( PhysicsCollider col, float3 vel, float3 pos, LocalTransform transform, int depth, float3 velInit )
    {
        int maxDepth = 5;
        float skinWidth = .0625f;

        //Debug.Log( pos );
        if(depth>= maxDepth)
            return float3.zero;

        
        

        float dist = math.length( vel ) + skinWidth;

        //float radius = col.Value.Value.CalculateAabb().Extents.x / 2;
        float radius = col.Value.As<CapsuleCollider>().Radius;;

        //if ( PhysicsWorld.CastCollider( cast, out ColliderCastHit hit ) )
        if ( PhysicsWorld.SphereCast( pos, radius - skinWidth, math.normalizesafe( vel ), dist, out ColliderCastHit hit, CastFilter ) )
        {
            float3 snapToSurface =
                math.normalizesafe( vel ) * ( math.distance( pos.xy, hit.Position.xy ) - radius - skinWidth );
            
            float3 leftOver = vel - snapToSurface;

            if(math.length( snapToSurface ) <= skinWidth)
                snapToSurface = float3.zero;
            
            float mag = math.length( leftOver );
            //leftOver =  math.normalizesafe(math.project( leftOver, hit.SurfaceNormal ));
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
    
}

using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformSystemGroup))]
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

//[BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;
    public PhysicsWorldSingleton PhysicsWorld;
    private static readonly float AngleAdjust = math.radians( 90 );

    private void Execute( ref LocalTransform transform, in PlayerInputs input, MyCharacterComponent attributes,
        PhysicsCollider col )
    {
        /*
        bool result = PhysicsWorld.BoxCast( input.AimPosition, quaternion.identity,
            new float3( 1, 1, 1 ) / GameSettings.PixelsPerUnit, new float3( 1, 0, 0 ), 1, CollisionFilter.Default,
            QueryInteraction.Default );
        Debug.Log( result );
        */
        //rotate the character to look at the mouse
        float3 forward = input.AimPosition - transform.Position;
        quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward );
        
        transform.Rotation = rotation;
        transform = transform.RotateZ( AngleAdjust );
            
        float2 targetMove = input.MoveInput * attributes.MovementSpeed * DeltaTime;
        if ( PhysicsCheck( transform, col, targetMove, out ColliderCastHit hit  ) )
        {
            //Debug.Log( hit.Fraction );
            targetMove *= hit.Fraction;
            //return;
        }

        transform.Position.xy += targetMove;
        //float2 targetPosition = transform.Position.xy + input.MoveInput * attributes.MovementSpeed;
        //velocity = math.lerp(velocity, targetVelocity, MathUtilities.GetSharpnessInterpolant(interpolationSharpness, deltaTime));
        
        
        
        

    }

    private bool PhysicsCheck(LocalTransform transform, PhysicsCollider col, float2 end, out ColliderCastHit hit)
    {
        ColliderCastInput cast = new ColliderCastInput(col.Value, transform.Position, transform.Position + new float3(end.x, end.y, 0),
            transform.Rotation);
        

        
        bool result = PhysicsWorld.CastCollider( cast, out hit );
        return result;
    }
    
}

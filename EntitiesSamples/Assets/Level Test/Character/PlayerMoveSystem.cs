using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using RaycastHit = Unity.Physics.RaycastHit;

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
        if ( PhysicsCheck( input.MoveInput, transform, col, targetMove, out ColliderCastHit hit, out NativeList<ColliderCastHit> castHits ) )
        {
            //Debug.Log( transform.Position.xy + ", " + hit.Position.xy );
            float2 relativeHit = transform.Position.xy - hit.Position.xy;

            
            if ( GetClosestPoint( transform, col, hit, castHits, out RaycastHit rayHit, out float2 adjust ) )
            {
                //Debug.Log( hit.Position - rayHit.Position );
                //transform.Position.xy -=  ( hit.Position - rayHit.Position ).xy;
                transform.Position.xy -=  adjust;
                
            }

            castHits.Dispose();
            return;
            /*
            if ( hit.Fraction > math.EPSILON )
            {
                targetMove *= hit.Fraction;
            }
            else if ( GetClosestPoint( transform, col, hit, out RaycastHit rayHit ) )
            {
                //Debug.Log( hit.Position - rayHit.Position );
                transform.Position.xy -=  ( hit.Position - rayHit.Position ).xy;
                return;
            }
            else
            {
                return;
            }
            */

            //Debug.DrawLine( transform.Position, hit.Position, Color.red, .1f );
            //transform.Position.xy += relativeHit;
            

        }

        transform.Position.xy += targetMove;
        //float2 targetPosition = transform.Position.xy + input.MoveInput * attributes.MovementSpeed;
        //velocity = math.lerp(velocity, targetVelocity, MathUtilities.GetSharpnessInterpolant(interpolationSharpness, deltaTime));
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
        
        //Debug.Log( castHits.Length );
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

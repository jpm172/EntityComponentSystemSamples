using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Collider = UnityEngine.Collider;
using RaycastHit = Unity.Physics.RaycastHit;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct PlayerShootingSystem : ISystem
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
       // EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        new PlayerShootJob
        {
            PhysicsWorld = physicsWorld,
            ECB = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer()
        }.Schedule();


        //ecb.Playback( state.EntityManager );
        //ecb.Dispose();
    }
}

public partial struct PlayerShootJob : IJobEntity
{
    private static readonly float Range = 30;
    public PhysicsWorldSingleton PhysicsWorld;
    public EntityCommandBuffer ECB;
    
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };
    
    private void Execute( in LocalTransform transform, in PlayerInputs input )
    {
        if ( !input.Shoot )
            return;
        
        if(CastRay( transform, out RaycastHit hit ))
        {
            //Debug.Log( hit.RigidBodyIndex + ", " + hit.ColliderKey.Value );
            BlobAssetReference<Unity.Physics.Collider> col = PhysicsWorld.Bodies[hit.RigidBodyIndex].Collider;
            ECB.DestroyEntity( hit.Entity );
        }

    }

    private bool CastRay( LocalTransform transform, out RaycastHit hit)
    {
        float3 rayEnd = transform.Right() * Range;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = CastFilter
        };
        
        
        
        if ( PhysicsWorld.CastRay( rayInput, out hit ) )
        {
            Debug.DrawLine( rayInput.Start, hit.Position, Color.red, .2f );
            return true;
        } 
        Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );

        return false;
    }
}

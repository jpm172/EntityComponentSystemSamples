using System.Collections;
using System.Collections.Generic;
using Baking.BlobAssetBakingSystem;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Collider = Unity.Physics.Collider;
//[UpdateInGroup(typeof(PhysicsDebugDisplayGroup))]
//[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst =  true)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ShootingCleanUpSystem : ISystem
{
//
    private EntityQuery query;
    private EntityQuery _endQuery;
    private EntityQuery destroyedQuery;
    public void OnCreate( ref SystemState state )
    {
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<DestructibleTag, OldCollider>().Build(ref state);
        _endQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<DestructibleTag, OldCollider>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState).Build(ref state);
        destroyedQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<DestructibleCleanUp>().WithNone<PhysicsCollider>().Build(ref state);
    }

    public void OnDestroy( ref SystemState state )
    {
        var entities = _endQuery.ToEntityArray(Allocator.Temp);
        foreach ( Entity e in entities )
        {
            state.EntityManager.GetComponentData<PhysicsCollider>( e ).Value.Dispose();
        }
    }

    public void OnUpdate( ref SystemState state )
    {
        state.EntityManager.CompleteDependencyBeforeRW<PhysicsWorldSingleton>();
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach ( Entity e in entities )
        {
            /*
            DynamicBuffer<ColliderBufferElement> buf = state.EntityManager.GetBuffer<ColliderBufferElement>( e );
            if ( buf.Length > 0 )
            {
                
            }
            Debug.Log( buf.Length );
            foreach ( var VARIABLE in buf )
            {
                //VARIABLE.Value.Dispose();
                if(VARIABLE.Value.Value.IsCreated)
                    VARIABLE.Value.Value.Dispose();
            }
            buf.Clear();
            */
            OldCollider old = state.EntityManager.GetComponentData<OldCollider>( e );
            if ( old.Value.IsCreated )
            {
                old.Value.Dispose();
                //old.Value = BlobAssetReference<Collider>.Null;
                //state.EntityManager.SetComponentData( e, old );
                
                state.EntityManager.SetComponentEnabled(e, typeof(OldCollider), false);
            }
            
        }
        
        var destroyed = destroyedQuery.ToEntityArray( Allocator.Temp );
        foreach ( Entity e in destroyed )
        {
            state.EntityManager.GetComponentData<DestructibleCleanUp>(e).Value.Value.Dispose();
            state.EntityManager.RemoveComponent<DestructibleCleanUp>( e );
        }

    }
}

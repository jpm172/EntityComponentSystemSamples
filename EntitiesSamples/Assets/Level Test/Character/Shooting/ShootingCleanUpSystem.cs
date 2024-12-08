using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst =  true)]
public partial struct ShootingCleanUpSystem : ISystem
{

    private EntityQuery query;
    private EntityQuery destroyedQuery;
    public void OnCreate( ref SystemState state )
    {
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<DestructibleTag, OldCollider>().Build(ref state);
        destroyedQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<DestructibleCleanUp>().WithNone<PhysicsCollider>().Build(ref state);
    }

    public void OnDestroy( ref SystemState state )
    {
        var entities = query.ToEntityArray(Allocator.Temp);
        foreach ( Entity e in entities )
        {
            state.EntityManager.GetComponentData<PhysicsCollider>( e ).Value.Dispose();
        }
    }

    public void OnUpdate( ref SystemState state )
    {
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach ( Entity e in entities )
        {
            OldCollider old = state.EntityManager.GetComponentData<OldCollider>( e );
            if ( old.Value.IsCreated )
            {
                old.Value.Dispose();
                old.Value = BlobAssetReference<Collider>.Null;
                state.EntityManager.SetComponentData( e, old );
                //state.EntityManager.SetComponentEnabled(e, typeof(OldCollider), false);
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

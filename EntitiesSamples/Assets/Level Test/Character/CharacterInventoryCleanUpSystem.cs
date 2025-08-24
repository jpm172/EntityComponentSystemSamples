using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public partial struct CharacterInventoryCleanUpSystem : ISystem
{

    private EntityQuery query;
    
    public void OnCreate( ref SystemState state )
    {
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<CharacterItemData>().WithAbsent<DestroyItem>().Build(ref state);
        //state.RequireForUpdate<DestroyOnUnequip>();
    }

    public void OnDestroy( ref SystemState state )
    {
       
    }

    public void OnUpdate( ref SystemState state )
    {
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        var entities = query.ToEntityArray(Allocator.Temp);
        foreach ( Entity e in entities )
        {
            CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( e );
            CharacterInventory inv = state.EntityManager.GetComponentData<CharacterInventory>( itemData.Owner );

            if(!inv.IsInPipeline( e ))
                ecb.RemoveComponent<CharacterItemData>( e );
            
            /*
            if(!inv.IsInPipeline( e ))
                ecb.DestroyEntity( e );
                */
        }
    }
}

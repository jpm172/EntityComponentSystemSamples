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
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<DestroyOnUnequip, CharacterItemData>().Build(ref state);
        //state.RequireForUpdate<DestroyOnUnequip>();
    }

    public void OnDestroy( ref SystemState state )
    {
       
    }

    public void OnUpdate( ref SystemState state )
    {
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        
        foreach ( var (itemData, remove, itemEntity) in
            SystemAPI.Query<RefRO<CharacterItemData>, EnabledRefRW<RemoveItem>>().WithEntityAccess())
        {
            if ( remove.ValueRW && ProcessItem(itemEntity, itemData.ValueRO, ecb, ref state) )
            {
                remove.ValueRW = false;
            }
            
        }
        
        /*
        var entities = query.ToEntityArray(Allocator.Temp);
        foreach ( Entity e in entities )
        {
            CharacterItemData itemData = state.EntityManager.GetComponentData<CharacterItemData>( e );
            CharacterInventory inv = state.EntityManager.GetComponentData<CharacterInventory>( itemData.Owner );

            if(!inv.IsInPipeline( e ))
                ecb.DestroyEntity( e );
        }
        */
    }
    
    private bool ProcessItem(Entity itemEntity, CharacterItemData itemData, EntityCommandBuffer ecb, ref SystemState state)
    {

        DynamicBuffer<CharacterAction> actionQueue = state.EntityManager.GetBuffer<CharacterAction>( itemData.Owner );

        for ( int i = actionQueue.Length - 1; i >= 0; i-- )
        {
            if(actionQueue[i].Item == itemEntity)
                actionQueue.RemoveAt( i );
        }
        
        CharacterInventory inv = state.EntityManager.GetComponentData<CharacterInventory>( itemData.Owner );

        if ( !inv.IsInPipeline( itemEntity ) )
        {
            ecb.DestroyEntity( itemEntity );
            return true;
        }

        

        return false;
    }
    
    
}

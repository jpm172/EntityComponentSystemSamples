using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

partial struct DestructibleStructureSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (
            var (col, info, entity)
            in SystemAPI.Query<EnabledRefRO<EntityCollider>, RefRO<MaterialMeshInfo>>().WithEntityAccess()
        )
        {
            BufferData data = state.EntityManager.GetComponentData<BufferData>( entity );
            
            //state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial( info.ValueRO.MaterialID ).SetBuffer( "_PointsBuffer", newBuffer );
            //Debug.Log( info.ValueRO.Material );
            RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            //arr.Materials[1].SetBuffer( "_PointsBuffer", newBuffer );

            for ( int i = 0; i < data.PointField.Length; i++ )
            {
                data.PointField[i] = 0;
            }
            data.SetBuffer();
            
            arr.GetMaterial( info.ValueRO ).SetBuffer( "_PointsBuffer", data.Buffer );
            //arr.Materials[info.ValueRO.Material].SetBuffer( "_PointsBuffer", data.Buffer );
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

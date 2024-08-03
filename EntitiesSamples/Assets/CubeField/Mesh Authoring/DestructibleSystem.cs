using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public partial struct DestructibleSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {

        foreach (
            var (col, buffer,  info, entity)
            in SystemAPI.Query<EnabledRefRO<EntityCollider>, DynamicBuffer<DestructibleData>, RefRO<MaterialMeshInfo>>()
                .WithEntityAccess()
        )
        {
            ComputeBuffer newBuffer = new ComputeBuffer(3, sizeof(int));
            newBuffer.SetData( new int[]{0,0,0});
            //state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial( info.ValueRO.MaterialID ).SetBuffer( "_PointsBuffer", newBuffer );
            RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            arr.Materials[1].SetBuffer( "_PointsBuffer", newBuffer );
        }
        
        
/*
        ComputeBuffer newBuffer = new ComputeBuffer(3, sizeof(int));
        newBuffer.SetData( new int[]{1,0,0});
        state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial( job.id.Value ).SetBuffer( "_PointsBuffer", newBuffer );
        */
    }
    
    public partial struct DestructibleJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer ECB;
        public NativeReference<BatchMaterialID> id;
        
        //public void Execute(Entity entity, ref CubeData cubeData, ref LocalTransform transform, EnabledRefRO<EntityCollider> col )
        public void Execute(Entity entity, EnabledRefRO<EntityCollider> col, DynamicBuffer<DestructibleData> buffer, MaterialMeshInfo info )
        {
            id.Value = info.MaterialID;
            //buffer.Clear();
            /*
            ComputeBuffer newBuffer = new ComputeBuffer(3, sizeof(int));
            newBuffer.SetData( new int[]{0,0,0});
            array.Materials[0].SetBuffer( "_PointsBuffer", newBuffer );
            newBuffer.Dispose();
            */
            //transform = transform.RotateY( math.radians(cubeData.speed * deltaTime) );
            //ECB.DestroyEntity( entity );

        }
        
    }
}

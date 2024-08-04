using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


    // Start is called before the first frame update
    public struct LevelSpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int MeshCount;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [ReadOnly]
        public NativeArray<RenderBounds> MeshBounds;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);
            
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
            // MeshBounds must be set according to the actual mesh for culling to work.
            int meshIndex = index % MeshCount;
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(meshIndex, meshIndex));
            Ecb.SetComponent(index, e, MeshBounds[meshIndex]);
        }

        public float4x4 ComputeTransform(int index)
        {

            float4x4 M = float4x4.TRS(
                new float3(0,0, 0),
                quaternion.identity,
                new float3(1));

            return M;
        }

    }


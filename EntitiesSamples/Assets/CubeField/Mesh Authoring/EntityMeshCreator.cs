using System;
using System.Collections;
using System.Collections.Generic;
using Baking.BakingDependencies;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;


public class EntityMeshCreator : MonoBehaviour
{

    public Mesh mesh;
    public Material mat;

    
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int EntityCount;
        public int MeshCount;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [ReadOnly]
        public NativeArray<RenderBounds> MeshBounds;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);
            
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
            Ecb.SetComponent(index, e, new MaterialColor() {Value = ComputeColor(index)});
            // MeshBounds must be set according to the actual mesh for culling to work.
            int meshIndex = index % MeshCount;
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, meshIndex));
            Ecb.SetComponent(index, e, MeshBounds[meshIndex]);
        }

        public float4 ComputeColor(int index)
        {
            float t = (float) index / (EntityCount - 1);
            var color = Color.HSVToRGB(t, 1, 1);
            return new float4(color.r, color.g, color.b, 1);
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
    
    private void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        
        EntityCommandBuffer ecbJob = new EntityCommandBuffer(Allocator.TempJob);

        var filterSettings = RenderFilterSettings.Default;
        filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
        filterSettings.ReceiveShadows = false;

        var renderMeshArray = new RenderMeshArray(new[] {mat}, new[] {mesh});
        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = filterSettings,
            LightProbeUsage = LightProbeUsage.Off,
        };


        Entity prototype = entityManager.CreateEntity();
        entityManager.SetName( prototype, "MyEntity" );
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(prototype, new MaterialColor());

        
        //var bounds = new NativeArray<RenderBounds>(Meshes.Count, Allocator.TempJob);
        var bounds = new NativeArray<RenderBounds>(1, Allocator.TempJob);
        for (int i = 0; i < bounds.Length; ++i)
            bounds[i] = new RenderBounds {Value = mesh.bounds.ToAABB()};

        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecbJob.AsParallelWriter(),
            EntityCount = 1,
            MeshCount = 1,
            MeshBounds = bounds
        };

        var spawnHandle = spawnJob.Schedule(1, 128);
        bounds.Dispose(spawnHandle);

        spawnHandle.Complete();
        
        
        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        entityManager.DestroyEntity(prototype);
        
    }
}

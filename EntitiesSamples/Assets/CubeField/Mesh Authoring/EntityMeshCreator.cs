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
using Random = UnityEngine.Random;


public class EntityMeshCreator : MonoBehaviour
{

    //public Mesh mesh;
    public Material mat;
    public int EntityCount = 10;
    
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
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(meshIndex, meshIndex));
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

        Mesh[] meshes = GetRandomMeshes(10);
        
        var matList = new List<Material>();
        for (int i=0;i< 10; i++)
        {
            var newMat = new Material(mat);
            Color col = Color.HSVToRGB(((float)(i * 10) / (float)1) % 1.0f, 0.7f, 1.0f);
            newMat.SetColor("_Color", col);              // set for LW
            newMat.SetColor("_BaseColor", col);          // set for HD
            //
            ComputeBuffer buffer = new ComputeBuffer(3, sizeof(int));

            buffer.SetData( new int[]{1+i,2+i,3+i});
            newMat.SetBuffer( "_PointsBuffer", buffer );
            //buffer.Dispose();
            
            //
            matList.Add(newMat);
        }
        
        //we can have a bunch of unique shaders
        var renderMeshArray = new RenderMeshArray(matList.ToArray(), meshes);
        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = filterSettings,
            LightProbeUsage = LightProbeUsage.Off,
        };

        //create the base entity that will be used as a template for spawning the reset
        Entity prototype = entityManager.CreateEntity();
        entityManager.SetName( prototype, "MyEntity" );
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        
        entityManager.AddComponentData(prototype, new MaterialColor());
        entityManager.AddComponentData( prototype, new EntityCollider() );
        entityManager.SetComponentEnabled<EntityCollider>( prototype, false );
        entityManager.AddBuffer<DestructibleData>( prototype );

        DynamicBuffer<DestructibleData> db = entityManager.GetBuffer<DestructibleData>( prototype );
        for ( int i = 0; i < 16; i++ )
        {
            db.Add( new DestructibleData {Value = 1} );
        }
            
        //entityManager.SetComponentEnabled( prototype, typeof(DestructibleData), false);
        
        var bounds = new NativeArray<RenderBounds>(meshes.Length, Allocator.TempJob);
        for (int i = 0; i < bounds.Length; ++i)
            bounds[i] = new RenderBounds {Value = meshes[i].bounds.ToAABB()};

        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecbJob.AsParallelWriter(),
            EntityCount = EntityCount,
            MeshCount = meshes.Length,
            MeshBounds = bounds
        };

        
        var spawnHandle = spawnJob.Schedule(EntityCount, 128);
        bounds.Dispose(spawnHandle);
        spawnHandle.Complete();
        
        
        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        entityManager.DestroyEntity(prototype);
    }


    private Mesh[] GetRandomMeshes(int amount)
    {
        Mesh[] result = new Mesh[amount];
        int blockSize = 64;
        
        int[] solidPointField = new int[blockSize*blockSize];
        
        for ( int n = 0; n < solidPointField.Length; n++ )
        {
            solidPointField[n] = 1;
        }
        
        for ( int i = 0; i < amount; i++ )
        {
            MinimalMeshConstructor meshConstructor = new MinimalMeshConstructor();
            
            
            /*
            int[] points = new int[blockSize*blockSize];
            for ( int n = 0; n < points.Length; n++ )
            {
                points[n] = Random.Range( 0, 2 );
            }
            */
            result[i] = meshConstructor.ConstructMesh( new Vector2Int( 10, 5 ),
                new Vector2Int( blockSize*i, 0 ),
                blockSize,
                solidPointField );
        }


        return result;
    }
}

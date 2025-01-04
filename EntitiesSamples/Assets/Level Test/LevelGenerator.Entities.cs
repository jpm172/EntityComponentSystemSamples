using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

public partial class LevelGenerator
{

    private List<BlobAssetReference<Unity.Physics.Collider>> _collidersMade;
    
    public void ClearLeverButton()
    {
        ClearLevelEntities();
        
    }

    //disposes+removes all entities made for the level
    private void ClearLevelEntities()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager entityManager = world.EntityManager;
        
        NativeArray<Entity> entities = entityManager.GetAllEntities();

        foreach ( Entity e in entities )
        {
            if(!entityManager.HasComponent( e, typeof(ClearOnNewLevelTag) ))
                continue;
            
            if ( entityManager.HasComponent( e, typeof( BufferData ) ) )
            {
                entityManager.GetComponentData<BufferData>(e).Dispose();
            }

            entityManager.DestroyEntity( e );
            
        }
        
        entities.Dispose();
    }
    
    private void MakeEntities()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager entityManager = world.EntityManager;
        EntityCommandBuffer ecbJob = new EntityCommandBuffer(Allocator.TempJob);

        RenderFilterSettings filterSettings = RenderFilterSettings.Default;
        filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
        filterSettings.ReceiveShadows = false;
        

        List<Material> matList = new List<Material>();
        List<Mesh> meshList = new List<Mesh>();

        Dictionary<Material, int> materialMap = new Dictionary<Material, int>();
        Dictionary<Mesh, int> meshMap = new Dictionary<Mesh, int>();
        //NativeHashMap<int, int> meshMaterialMap = new NativeHashMap<int, int>(_floors.Count, Allocator.TempJob);
        NativeHashMap<int, EntityRenderInfo> entityRenderMap = new NativeHashMap<int, EntityRenderInfo>(_floors.Count+_walls.Count, Allocator.TempJob);

        int entityCounter = 0;
        
        //gather all the meshes/materials used
        foreach ( LevelFloor floor in _floors )
        {
            //floor.FloorMaterial.renderQueue = SortingLayer.GetLayerValueFromName( "Floor" );
            if ( !meshMap.ContainsKey( floor.FloorMesh ) )
            {
                meshList.Add( floor.FloorMesh );
                meshMap[floor.FloorMesh] = meshList.Count - 1;
            }

            if ( !materialMap.ContainsKey( floor.FloorMaterial ) )
            {
                matList.Add( floor.FloorMaterial );
                materialMap[floor.FloorMaterial] = matList.Count-1;
            }

            EntityRenderInfo info = new EntityRenderInfo
            {
                MaterialIndex = materialMap[floor.FloorMaterial],
                MeshIndex = meshMap[floor.FloorMesh],
                Position = floor.Position
            };
            entityRenderMap[entityCounter] = info;
            
            entityCounter++;
        }

        
        //need to do the SortingGroup stuff via setting shader priority
        //doing it in code is inconsistent
        foreach ( LevelWall wall in _walls )
        {
            if ( !meshMap.ContainsKey( wall.Mesh ) )
            {
                meshList.Add( wall.Mesh );
                meshMap[wall.Mesh] = meshList.Count - 1;
            }
            
            
            //matList.Add( new Material(wall.Material) );
            matList.Add( wall.Material );
            
            
            EntityRenderInfo info = new EntityRenderInfo
            {
                MaterialIndex = matList.Count-1,
                MeshIndex = meshMap[wall.Mesh],
                Position = wall.Position
            };
            
            entityRenderMap[entityCounter] = info;
            entityCounter++;
        }
        
        
        
        //put them into the RenderMeshArray used for ECS
        RenderMeshArray renderMeshArray = new RenderMeshArray(matList.ToArray(), meshList.ToArray());
        RenderMeshDescription renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = filterSettings,
            LightProbeUsage = LightProbeUsage.Off,
        };

        Entity floorEntity = CreateBaseFloorEntity( entityManager, renderMeshArray, renderMeshDescription );
       
        CreateWallEntities( entityRenderMap, entityManager, renderMeshArray, renderMeshDescription );
        

        
        var bounds = new NativeArray<RenderBounds>(meshList.Count, Allocator.TempJob);
        for (int i = 0; i < bounds.Length; ++i)
            bounds[i] = new RenderBounds {Value = meshList[i].bounds.ToAABB()};

        LevelSpawnUnmanagedJob spawnJob = new LevelSpawnUnmanagedJob
        {
            Prototype = floorEntity,
            Ecb = ecbJob.AsParallelWriter(),
            MeshBounds = bounds,
            EntityRenderMap = entityRenderMap
        };

        var spawnHandle = spawnJob.Schedule(entityCounter-_walls.Count, 128);
        bounds.Dispose(spawnHandle);
        entityRenderMap.Dispose( spawnHandle );
        
        spawnHandle.Complete();
        
        
        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        entityManager.DestroyEntity(floorEntity);
        
        
    }

    private void CreateWallEntities( NativeHashMap<int, EntityRenderInfo> renderMap, EntityManager entityManager, RenderMeshArray renderMeshArray, RenderMeshDescription renderMeshDescription )
    {
        int entityCountOffset = _floors.Count;
        
        for ( int i = 0; i < _walls.Count; i++ )
        {
            Entity prototype = entityManager.CreateEntity();
            LevelWall wall = _walls[i];
            
            EntityRenderInfo info = renderMap[entityCountOffset+i];
            
#if UNITY_EDITOR
            entityManager.SetName( prototype, wall.StructureMat +" Wall" + (i+1) );
#endif
        
            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(info.MaterialIndex, info.MeshIndex));

            entityManager.AddComponentData( prototype, new LocalToWorld {Value = float4x4.TRS(
                new float3(info.Position),
                quaternion.identity,
                new float3(1))});

            if ( wall.StructureMat == LevelMaterial.Indestructible )
            {
                CreateStaticWall(entityManager, prototype, wall, renderMeshArray, info);
            }
            else
            {
                CreateDynamicWall(entityManager, prototype, wall, renderMeshArray, info);
            }
            
        }
    }

    private void CreateDynamicWall(EntityManager entityManager, Entity prototype, LevelWall wall, RenderMeshArray renderMeshArray, EntityRenderInfo info)
    {
        entityManager.AddBuffer<DestructibleData>( prototype );
        DynamicBuffer<DestructibleData> destructibleBuffer = entityManager.GetBuffer<DestructibleData>( prototype );
        for ( int j = 0; j < wall.PointField.Length; j++ )
        {
            destructibleBuffer.Add( new DestructibleData {Value = wall.PointField[j]} );
        }

        entityManager.AddComponentData( prototype, new StructureInfo
        {
            Material = wall.StructureMat,
            Size = wall.Bounds.Size(),
            //Bounds = wall.Bounds
        } );
        entityManager.AddComponentData( prototype, new OldCollider() );
        entityManager.SetComponentEnabled( prototype, typeof(OldCollider), false );
        entityManager.AddComponentData( prototype, new DestructibleTag() );
        
        entityManager.AddComponentData( prototype, new ClearOnNewLevelTag() );
        entityManager.AddComponentData( prototype, new BufferData
        {
            Buffer = new ComputeBuffer( wall.PointField.Length,sizeof(int) )
        } );
        entityManager.GetComponentData<BufferData>(prototype).SetBuffer(wall.PointField);
        renderMeshArray.Materials[info.MaterialIndex].SetBuffer( "_PointsBuffer", entityManager.GetComponentData<BufferData>(prototype).Buffer );
        
        
        NativeArray<CompoundCollider.ColliderBlobInstance> childCols = new NativeArray<CompoundCollider.ColliderBlobInstance>(wall.Geo.Count, Allocator.Temp);
        Unity.Physics.Material levelMat = Unity.Physics.Material.Default;
        levelMat.CustomTags = (byte)wall.StructureMat;
        for(int w = 0; w < wall.Geo.Count; w++)
        {
            BoxGeometry geo = wall.Geo[w];
            CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
            {
                Collider = Unity.Physics.BoxCollider.Create( geo, CollisionFilter.Default, Unity.Physics.Material.Default ),
                Entity = prototype,
                CompoundFromChild = new RigidTransform
                {
                    rot = quaternion.identity,
                    pos = float3.zero
                }
            };
            childCols[w] = newChild;
            _collidersMade.Add( newChild.Collider );
        }

        BlobAssetReference<Unity.Physics.Collider> compCol = Unity.Physics.CompoundCollider.Create( childCols );

        PhysicsCollider physicsCollider = new PhysicsCollider {Value = compCol};
        entityManager.AddComponentData(prototype, physicsCollider );
        entityManager.AddComponentData( prototype, new DestructibleCleanUp{Value = physicsCollider} );
        childCols.Dispose();
        
        entityManager.AddSharedComponent(prototype, new PhysicsWorldIndex());
    }
    
    private void CreateStaticWall(EntityManager entityManager, Entity prototype, LevelWall wall, RenderMeshArray renderMeshArray, EntityRenderInfo info)
    {
        
        entityManager.AddComponentData( prototype, new StructureInfo
        {
            Material = wall.StructureMat,
           Size = wall.Bounds.Size(),
           //Bounds = wall.Bounds
        } );

        entityManager.AddComponentData( prototype, new DestructibleTag() );
        
        entityManager.AddComponentData( prototype, new ClearOnNewLevelTag() );


        NativeArray<CompoundCollider.ColliderBlobInstance> childCols = new NativeArray<CompoundCollider.ColliderBlobInstance>(wall.Geo.Count, Allocator.Temp);

        Unity.Physics.Material levelMat = Unity.Physics.Material.Default;

        levelMat.CustomTags = (byte)wall.StructureMat;

        for(int w = 0; w < wall.Geo.Count; w++)
        {
            BoxGeometry geo = wall.Geo[w];
            CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
            {
                Collider = Unity.Physics.BoxCollider.Create( geo, CollisionFilter.Default, levelMat ),
                Entity = prototype,
                CompoundFromChild = new RigidTransform
                {
                    rot = quaternion.identity,
                    pos = float3.zero
                }
            };
            childCols[w] = newChild;
            _collidersMade.Add( newChild.Collider );
        }

        BlobAssetReference<Unity.Physics.Collider> compCol = Unity.Physics.CompoundCollider.Create( childCols );

        PhysicsCollider physicsCollider = new PhysicsCollider {Value = compCol};
        entityManager.AddComponentData(prototype, physicsCollider );
        entityManager.AddComponentData( prototype, new DestructibleCleanUp{Value = physicsCollider} );
        childCols.Dispose();
        
        entityManager.AddSharedComponent(prototype, new PhysicsWorldIndex());
    }
    
    private Entity CreateBaseFloorEntity(EntityManager entityManager, RenderMeshArray renderMeshArray, RenderMeshDescription renderMeshDescription )
    {
        //create the base entity that will be used as a template for spawning the reset
        Entity prototype = entityManager.CreateEntity();
        entityManager.AddComponentData( prototype, new ClearOnNewLevelTag() );
        
        #if UNITY_EDITOR
        entityManager.SetName( prototype, "Floor" );
        #endif
        
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

        return prototype;
    }
    
}

public struct EntityRenderInfo
{
    public int MeshIndex;
    public int MaterialIndex;
    public Vector3 Position;
}
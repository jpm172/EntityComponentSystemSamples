using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Physics;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using Material = UnityEngine.Material;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector2Int dimensions;

    [SerializeField]
    private Material[] floorMaterials;

    [SerializeField]
    private Material wallMaterial;
    
    private List<LevelFloor> _floors;
    private List<LevelWall> _walls;
    private int[] _levelLayout;
    public void GenerateLevel()
    {
        _levelLayout = new int[dimensions.x*dimensions.y];
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        
        MakeFloors();
        MakeWalls();
        MakeEntities();
    }

    private void MakeWalls()
    {
        
        int blockSize = 4;
        for ( int x = 0; x < dimensions.x; x++ )
        {
            for ( int y = 0; y < dimensions.y; y++ )
            {
                int[] solidPointField = new int[blockSize*blockSize];
        
                for ( int n = 0; n < solidPointField.Length/2; n++ )
                {
                    solidPointField[n] = 1;
                }
                MinimalMeshConstructor meshConstructor = new MinimalMeshConstructor();
                
                Vector2Int floorDimensions = new Vector2Int(3,3);
                Vector2Int position = new Vector2Int(x,y);
                Mesh mesh = meshConstructor.ConstructMesh( floorDimensions, position, blockSize, solidPointField );
                
                LevelWall newWall = new LevelWall
                {
                   Mesh = mesh,
                   Material = wallMaterial,
                   PointField = solidPointField
                };
                
                _walls.Add( newWall  );
            }
        }
    }
    
    private void MakeFloors()
    {
        int blockSize = 64;
        
        int[] solidPointField = new int[blockSize*blockSize];
        
        for ( int n = 0; n < solidPointField.Length; n++ )
        {
            solidPointField[n] = 1;
        }
        
        for ( int x = 0; x < dimensions.x; x++ )
        {
            for ( int y = 0; y < dimensions.y; y++ )
            {
                MinimalMeshConstructor meshConstructor = new MinimalMeshConstructor();
                Vector2Int floorDimensions = new Vector2Int(Random.Range( 1, 10 ), Random.Range( 1,10 ));
                Vector2Int position = new Vector2Int(x,y);
                Mesh floorMesh = meshConstructor.ConstructMesh( floorDimensions, position, blockSize, solidPointField );
                
                
                LevelFloor newFloor = new LevelFloor
                {
                    FloorMesh = floorMesh, 
                    FloorMaterial = floorMaterials[Random.Range( 0, floorMaterials.Length )]
                };
                
                _floors.Add( newFloor  );
            }
        }


        
        
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
                MeshIndex = meshMap[floor.FloorMesh]
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
            
            
            matList.Add( new Material(wall.Material) );
            
            
            EntityRenderInfo info = new EntityRenderInfo
            {
                MaterialIndex = matList.Count-1,
                MeshIndex = meshMap[wall.Mesh]
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
        BlobAssetReference<Unity.Physics.Collider> blob = Unity.Physics.BoxCollider.Create( new BoxGeometry { BevelRadius = .1f,
            Center = new float3(0),
            Orientation = new quaternion(1,1,1,1),
            Size = new float3(1)} );
        entityManager.AddComponentData( floorEntity, new PhysicsCollider{Value = blob} );
        
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
            entityManager.SetName( prototype, "Wall" + (i+1) );
#endif
        
            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                renderMeshDescription,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(info.MaterialIndex, info.MeshIndex));

            entityManager.AddComponentData( prototype, new LocalToWorld {Value = float4x4.TRS(
                new float3(0,0, 0),
                quaternion.identity,
                new float3(1))});
            entityManager.AddComponentData( prototype, new EntityCollider() );
            entityManager.SetComponentEnabled<EntityCollider>( prototype, false );

            entityManager.AddComponentData( prototype, new BufferData
            {
                PointField = wall.PointField,
                Buffer = new ComputeBuffer( wall.PointField.Length,sizeof(int) )
            } );
            entityManager.GetComponentData<BufferData>(prototype).SetBuffer();
            renderMeshArray.Materials[info.MaterialIndex].SetBuffer( "_PointsBuffer", entityManager.GetComponentData<BufferData>(prototype).Buffer );
            


        }
    }

    private Entity CreateBaseFloorEntity(EntityManager entityManager, RenderMeshArray renderMeshArray, RenderMeshDescription renderMeshDescription )
    {
        //create the base entity that will be used as a template for spawning the reset
        Entity prototype = entityManager.CreateEntity();
        
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
    
    private Entity CreateBaseWallEntity(EntityManager entityManager, RenderMeshArray renderMeshArray, RenderMeshDescription renderMeshDescription )
    {
        //create the base entity that will be used as a template for spawning the reset
        Entity prototype = entityManager.CreateEntity();
        
        #if UNITY_EDITOR
        entityManager.SetName( prototype, "Wall" );
        #endif
        
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

        entityManager.AddComponentData( prototype, new EntityCollider() );
        entityManager.SetComponentEnabled<EntityCollider>( prototype, false );
        //entityManager.AddBuffer<DestructibleData>( prototype );
        entityManager.AddComponentData( prototype, new BufferData() );
        
        
        return prototype;
    }
    
}


public struct EntityRenderInfo
{
    public int MeshIndex;
    public int MaterialIndex;
}

public struct LevelFloor
{
    public Mesh FloorMesh;
    public Material FloorMaterial;
    public Vector2 Position;
}

public struct LevelWall
{
    public Mesh Mesh;
    public Material Material;
    public Vector2 Position;
    public int[] PointField;
}
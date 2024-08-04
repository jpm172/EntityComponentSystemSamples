using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector2Int dimensions;

    [SerializeField]
    private Material[] floorMaterials;

    private List<LevelFloor> _floors;
    private List<LevelWall> _walls;
    private int[] _levelLayout;
    public void GenerateLevel()
    {
        _levelLayout = new int[dimensions.x*dimensions.y];
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        
        MakeFloors();

        MakeEntities();
    }

    private void MakeWalls()
    {
        
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
                
                Vector2Int floorDimensions = new Vector2Int(Random.Range( 1, 10 ), Random.RandomRange( 1,10 ));
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
        NativeHashMap<int, int> meshMaterialMap = new NativeHashMap<int, int>(_floors.Count, Allocator.TempJob);
        
        
        //gather all the meshes/materials used
        foreach ( LevelFloor floor in _floors )
        {
            
            meshList.Add( floor.FloorMesh );
            
            if ( !materialMap.ContainsKey( floor.FloorMaterial ) )
            {
                matList.Add( floor.FloorMaterial );
                materialMap[floor.FloorMaterial] = matList.Count-1;

                meshMaterialMap[meshList.Count - 1] = matList.Count - 1;
            }
            else
            {
                meshMaterialMap[meshList.Count - 1] = materialMap[floor.FloorMaterial];
            }
            
            
        }
        
        //put them into the RenderMeshArray used for ECS
        RenderMeshArray renderMeshArray = new RenderMeshArray(matList.ToArray(), meshList.ToArray());
        RenderMeshDescription renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = filterSettings,
            LightProbeUsage = LightProbeUsage.Off,
        };

        Entity floorEntity = CreateBaseFloorEntity( entityManager, renderMeshArray, renderMeshDescription );
        
        var bounds = new NativeArray<RenderBounds>(meshList.Count, Allocator.TempJob);
        for (int i = 0; i < bounds.Length; ++i)
            bounds[i] = new RenderBounds {Value = meshList[i].bounds.ToAABB()};
        
        LevelSpawnJob spawnJob = new LevelSpawnJob
        {
            Prototype = floorEntity,
            Ecb = ecbJob.AsParallelWriter(),
            MeshCount = meshList.Count,
            MeshBounds = bounds,
            MeshMaterialMap = meshMaterialMap
        };

        var spawnHandle = spawnJob.Schedule(_floors.Count, 128);
        bounds.Dispose(spawnHandle);
        meshMaterialMap.Dispose( spawnHandle );
        
        spawnHandle.Complete();
        
        
        ecbJob.Playback(entityManager);
        ecbJob.Dispose();
        entityManager.DestroyEntity(floorEntity);
        
        
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
    
    
}

public struct LevelFloor
{
    public Mesh FloorMesh;
    public Material FloorMaterial;
    public Vector2 Position;
}

public struct LevelWall
{
    
}
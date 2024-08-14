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
using Unity.VisualScripting;
using Material = UnityEngine.Material;
using Random = UnityEngine.Random;

public partial class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector2Int dimensions;//dimensions of level in pixels
    
    [SerializeField]
    private Vector2Int layoutDimensions;//dimensions of level in room nodes

    [SerializeField]
    private Material[] floorMaterials;

    [SerializeField]
    private Material wallMaterial;
    
    private List<LevelFloor> _floors;
    private List<LevelWall> _walls;
    private LevelRoom[] _rooms;
    
    
    private NativeArray<int> _levelLayout;
    
    
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ( !_levelLayout.IsCreated )
            return;
        
        Vector3 size = Vector3.one / GameSettings.PixelsPerUnit;
        for ( int x = 0; x < dimensions.x; x++ )
        {
            for ( int y = 0; y < dimensions.y; y++ )
            {
                int index = x + y * dimensions.x;
                if ( _levelLayout[index] > 0 )
                {
                    LevelRoom room = _rooms[_levelLayout[index] - 1];
                    Vector3 pos = new Vector3(x,y)/GameSettings.PixelsPerUnit;
                    
                    Gizmos.color = room.DebugColor;
                    Gizmos.DrawCube( pos, size );
                    //Gizmos.DrawWireCube( pos,size );
                }
                else if ( IsBorder( x, y ) )
                {
                    Gizmos.color = Color.black;
                    Vector3 pos = new Vector3(x,y)/GameSettings.PixelsPerUnit;
                    Gizmos.DrawCube( pos, size );
                }
            }
        }
    }
#endif

    private bool IsBorder( int x, int y )
    {
        if ( x == 0 || x == dimensions.x - 1 )
            return true;
        if ( y == 0 || y == dimensions.y - 1 )
            return true;
        return false;
    }

    public void GenerateLevel()
    {
        InitializeLevel();
        
        //MakeFloors();
        //MakeWalls();
        //MakeEntities();
    }

    private void InitializeLevel()
    {
        int minSize = 20;
        int count = layoutDimensions.x * layoutDimensions.y;

        if ( _levelLayout.IsCreated )
            _levelLayout.Dispose();
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        
        
        //create the rooms and seed them into the level layout

        int buffer = 50;
        int xOffset = buffer;
        
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            int yOffset = buffer;

            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                Vector2Int roomOrigin = new Vector2Int(xOffset, yOffset);
                Vector2Int roomSize = new Vector2Int( Random.Range( minSize, buffer ), Random.Range( minSize, buffer ) );
                LevelRoom room = new LevelRoom(index+1, roomOrigin, roomSize, Random.Range( 2,6 ));
                
                _rooms[index] = room;
                
                yOffset += buffer*2;
            }
            
            xOffset += buffer*2;
        }
        
        //create the level array aand seed it with the rooms  
        dimensions = new Vector2Int(buffer*layoutDimensions.x*2 + buffer , buffer*layoutDimensions.y*2 + buffer );
        _levelLayout = new NativeArray<int>(dimensions.x*dimensions.y, Allocator.Persistent);
        for(int i = 0; i < _rooms.Length; i++)
        {
            LevelRoom room = _rooms[i];
            DrawBox( room.Origin.x, room.Origin.y, room.Size.x, room.Size.y, ref room );
        }
        
    }

    private void DrawBox( int xOrigin, int yOrigin, int width, int height, ref LevelRoom room )
    {
        int start = xOrigin + yOrigin * dimensions.x;

        for ( int x = 0; x < width; x++ )
        {
            for ( int y = 0; y < height; y++ )
            {
                int index = x + y * dimensions.x;
                _levelLayout[start + index] = room.Id;
            }
        }

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
                   PointField = solidPointField,
                   Position = position
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
                    FloorMaterial = floorMaterials[Random.Range( 0, floorMaterials.Length )],
                    Position = position
                };
                
                _floors.Add( newFloor  );
            }
        }
    }

    private void OnDestroy()
    {
        if ( _levelLayout.IsCreated )
        {
            _levelLayout.Dispose();
        }
            
    }
}


public struct LevelRoom
{
    private int _wallThickness;
    private int _id;
    private Color _debugColor;
    private Vector2Int _size;
    private Vector2Int _origin;

    public Vector2Int Size => _size;
    public Vector2Int Origin => _origin;
    public int WallThickness => _wallThickness;
    public int Id => _id;
    public Color DebugColor => _debugColor;
    

    public LevelRoom( int id, Vector2Int origin, Vector2Int size, int wallThickness )
    {
        _id = id;
        _origin = origin;
        _size = size;
        _wallThickness = wallThickness;
        _debugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
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
    public Mesh Mesh;
    public Material Material;
    public Vector2 Position;
    public int[] PointField;
}
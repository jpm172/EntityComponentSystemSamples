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

    [SerializeField] private int seed;
    
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
/*
        Gizmos.color = Color.black;
        for ( int i = 0; i < layoutDimensions.y; i++)
        {
            LevelRoom room = _rooms[i * layoutDimensions.x];
            Vector3 pos = new Vector3(room.Origin.x, room.Origin.y)/GameSettings.PixelsPerUnit;
            Gizmos.DrawLine( pos, pos + (Vector3.right*100) );
        }
        */
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
        //Random.seed = seed;
        InitializeLevel();
        
        //MakeFloors();
        //MakeWalls();
        //MakeEntities();
    }

    private void InitializeLevel()
    {
        int minSize = 20;
        int maxSize = 60;
        int count = layoutDimensions.x * layoutDimensions.y;

        if ( _levelLayout.IsCreated )
            _levelLayout.Dispose();
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        
        
        //create the rooms and seed them into the level layout

        int buffer = 20;
        int xOffset = buffer;
        
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            int yOffset = buffer;

            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                
                //Vector2Int roomSize = new Vector2Int( Random.Range( minSize, maxSize+1 ), Random.Range( minSize, maxSize+1 ) );
                Vector2Int roomSize = new Vector2Int( minSize, minSize  );
                Vector2Int roomOrigin = GetRandomAlignedRoomOrigin( x, y, xOffset , yOffset, buffer, minSize, roomSize );
                LevelRoom room = new LevelRoom(index+1, roomOrigin, roomSize, Random.Range( 2,6 ));
                
                _rooms[index] = room;
                
                yOffset += maxSize + (buffer*2);
            }
            
            xOffset += maxSize + (buffer*2);
        }
        
        //create the level array aand seed it with the rooms  
        dimensions = new Vector2Int((maxSize*layoutDimensions.x) + (buffer*2*layoutDimensions.x) *2 , (maxSize*layoutDimensions.y) + (buffer*2*layoutDimensions.y) * 2 );
        _levelLayout = new NativeArray<int>(dimensions.x*dimensions.y, Allocator.Persistent);
        for(int i = 0; i < _rooms.Length; i++)
        {
            LevelRoom room = _rooms[i];
            DrawBox( room.Origin.x, room.Origin.y, room.Size.x, room.Size.y, ref room );
        }
        
    }


    private Vector2Int GetRandomAlignedRoomOrigin(int x, int y, int xOffset, int yOffset, int buffer, int minSize, Vector2Int size)
    {
        Vector2Int shift = new Vector2Int(0,0);

        if ( x == 0 )
        {
            shift.y = Random.Range( -buffer, buffer + 1 );
        }
        else
        {
            int index = ( x - 1 ) + y * layoutDimensions.x;
            LevelRoom leftNeighbor = _rooms[index];
            int distance = yOffset - leftNeighbor.Origin.y;
            
            if ( distance >= 0 )
            {
                int downShift = -(distance + (size.y - minSize));
                
                //equal to the distance need to have the tops of the two rects align, plus anything past the minimum size requirement
                int upShift = leftNeighbor.Size.y - (distance + size.y) + (size.y - minSize);

                shift.y = Random.Range( downShift, upShift + 1 );
            }
            else
            {
                int downShift = -(distance + (size.y - minSize));
                //equal to the distance need to have the tops of the two rects align, plus anything past the minimum size requirement
                int upShift = leftNeighbor.Size.y - (distance + size.y) + (size.y - minSize);

                shift.y = Random.Range( downShift, upShift + 1 );
            }
        }

        if ( y == 0 )
        {
            shift.x = Random.Range( -buffer, buffer + 1 );
        }


        int xResult = Mathf.Clamp( xOffset + shift.x, xOffset - buffer, xOffset + buffer );
        int yResult = Mathf.Clamp( yOffset + shift.y, yOffset - buffer, yOffset + buffer );
        
        
        return new Vector2Int(xOffset, yOffset) + shift;
        //return new Vector2Int(xResult, yResult) ;
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
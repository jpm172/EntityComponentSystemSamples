using System;
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


    //seeding variables
    private int _minRoomSeedSize = 20;
    private int _maxRoomSeedSize = 60;
    private int _seedBuffer = 20;
    
    //dijkstas variables
    private Dictionary<int, List<LevelEdge>> _levelGraph;//stores the edge weights used for dijsktras
    private int minEdgeWeight = 1;
    [SerializeField]
    private int maxEdgeWeight = 10;
    

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

        Gizmos.color = Color.red;
        for ( int i = 0; i < _rooms.Length; i++ )
        {
            if ( _levelGraph.ContainsKey( i ) )
            {
                foreach ( LevelEdge e in _levelGraph[i] )
                {
                    Vector3 source = (Vector2)_rooms[e.Source].Origin;
                    Vector3 destination = (Vector2)_rooms[e.Destination].Origin;
                    
                    Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
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
 
        Random.seed = seed;
        
        InitializeLevel();
        
        FindPath();
        
        //GrowRooms();
        //MakeFloors();
        //MakeWalls();
        //MakeEntities();
        
    }

    

    private void FindPath()
    {
        int INF = Int32.MaxValue;
        int[] distance = new int[_rooms.Length];
        LevelEdge[] pathEdges = new LevelEdge[_rooms.Length];
        List<int> path = new List<int>();


        //initialize the lists needed to run dijkstras alg
        for ( int i = 0; i < distance.Length; i++ )
        {
            distance[i] = INF;
        }
        
        //select a starting node
        int startNode = Random.Range( 0, _rooms.Length );
        distance[startNode] = 0;
        
        //run dijkstras algorithm until the path is found
        while ( path.Count < _rooms.Length )
        {
            int min = INF;
            int result = -1;

            //pick the node with the smallest distance to reach
            for ( int i = 0; i < distance.Length; i++ )
            {
                if ( distance[i] < min && !path.Contains( i ) )
                {
                    result = i;
                    min = distance[i];
                }
            }


            path.Add( result );
            distance[result] = min;
            
            //look at each of the edges, update any new connections that are shorter than the ones previously found
            foreach ( LevelEdge e in _levelGraph[result] )
            {
                //LevelRoom destination = _rooms[e.Destination];
                //int index = (int) ((int)e.Destination.GraphPosition.x + e.Destination.GraphPosition.y * _width);

                if ( min + e.Weight < distance[e.Destination] )
                {
                    distance[e.Destination] = min + e.Weight;
                    pathEdges[e.Destination] = e;
                }
            }
        }
        
        //clear all path information in the graph
        _levelGraph.Clear();
        
        //update the graph to only contain the edges from the shortest path we just found
        foreach ( LevelEdge e in pathEdges )
        {
            //the starting node will not be assigned a path, which will show up as an edge with all values == 0, so ignore it
            if ( e.Destination != e.Source )
            {
                SetNeighbors( e.Source, e.Destination, e.Weight );
                SetPathInfo( e.Source, e.Destination );
            }
                
        }
        


    }
    
    private void InitializeLevel()
    {
        int count = layoutDimensions.x * layoutDimensions.y;

        if ( _levelLayout.IsCreated )
            _levelLayout.Dispose();
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        _levelGraph = new Dictionary<int, List<LevelEdge>>();
        
        
        //create the rooms and randomly shuffle them around, but while making sure they are still aligned with their neighbors
        int xOffset = _seedBuffer;
        
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            int yOffset = _seedBuffer;

            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                
                //set the room's initial variables
                Vector2Int roomSize = new Vector2Int( Random.Range( _minRoomSeedSize, _maxRoomSeedSize+1 ), Random.Range(_minRoomSeedSize, _maxRoomSeedSize+1 ) );
                Vector2Int graphPosition = new Vector2Int(x,y);
                Vector2Int roomOrigin = GetRandomAlignedRoomOrigin( x, y, xOffset , yOffset, roomSize );
                int wallThickness = Random.Range( 2, 6 );
                LevelMaterial mat = GetRandomRoomMaterial();
                LevelGrowthType growthType = LevelGrowthType.Normal;
                
                LevelRoom room = new LevelRoom(index+1, graphPosition, roomOrigin, roomSize, mat, wallThickness, growthType);
                _rooms[index] = room;

                //set the room's neighbors in preparation of dijkstras algorithm
                if ( x > 0 )
                {
                    int neighborIndex = ( x - 1 ) + y * layoutDimensions.x;
                    SetNeighbors( index, neighborIndex, Random.Range( minEdgeWeight, maxEdgeWeight+1 ) );
                }
                if ( y > 0 )
                {
                    int neighborIndex = x + (y - 1 ) * layoutDimensions.x;
                    SetNeighbors( index, neighborIndex, Random.Range( minEdgeWeight, maxEdgeWeight+1 ) );
                }
                
                yOffset += _maxRoomSeedSize + (_seedBuffer*2);
            }
            
            xOffset += _maxRoomSeedSize + (_seedBuffer*2);
        }
        
        //create the level array and seed it with the rooms  
        dimensions = new Vector2Int((_maxRoomSeedSize*layoutDimensions.x) + (_seedBuffer*2*layoutDimensions.x) , (_maxRoomSeedSize*layoutDimensions.y) + (_seedBuffer*2*layoutDimensions.y) );
        _levelLayout = new NativeArray<int>(dimensions.x*dimensions.y, Allocator.Persistent);
        for(int i = 0; i < _rooms.Length; i++)
        {
            LevelRoom room = _rooms[i];
            DrawBox( room.Origin.x, room.Origin.y, room.Size.x, room.Size.y, ref room );
        }
        
    }

    private void SetPathInfo( int room1, int room2 )
    {
        
    }
    
    private void SetNeighbors( int room1, int room2, int weight )
    {
        //Debug.Log( $"Connecting rooms {room1} and {room2} with weight {weight}" );
        if ( !_levelGraph.ContainsKey( room1 ) )
        {
            _levelGraph[room1] = new List<LevelEdge>();
        }
        _levelGraph[room1].Add( new LevelEdge{Source = room1,Destination = room2, Weight = weight} );
        
        if ( !_levelGraph.ContainsKey( room2 ) )
        {
            _levelGraph[room2] = new List<LevelEdge>();
        }
        _levelGraph[room2].Add( new LevelEdge{Source = room2, Destination = room1, Weight = weight} );
    }
    
    private Vector2Int GetRandomAlignedRoomOrigin(int x, int y, int xOffset, int yOffset,  Vector2Int size)
    {
        Vector2Int shift = new Vector2Int(0,0);

        if ( x == 0 )
        {
            shift.y = Random.Range( -_seedBuffer, _seedBuffer + (_maxRoomSeedSize - size.y)+ 1 );
        }
        else
        {
            int index = ( x - 1 ) + y * layoutDimensions.x;
            LevelRoom leftNeighbor = _rooms[index];
            int distance = yOffset - leftNeighbor.Origin.y;
            
            //equal to the steps needed to have the bottoms of the rects to align, plus anything past the minimum size requirement
            int downShift = -(distance + (size.y - _minRoomSeedSize));
            //equal to the steps needed to have the tops of the two rects align, plus anything past the minimum size requirement
            int upShift = leftNeighbor.Size.y - (distance + size.y) + (size.y - _minRoomSeedSize);
            shift.y = Random.Range( downShift, upShift + 1 );
        }

        if ( y == 0 )
        {
            shift.x = Random.Range( -_seedBuffer, _seedBuffer + (_maxRoomSeedSize - size.x) + 1 );
        }
        else
        {
            int index = x + (y-1) * layoutDimensions.x;
            LevelRoom bottomNeighbor = _rooms[index];
            int distance = xOffset - bottomNeighbor.Origin.x;
            
            //equal to the steps needed to have the left side of the rects to align, plus anything past the minimum size requirement
            int leftShift = -(distance + (size.x - _minRoomSeedSize));
            //equal to the steps needed to have the right side of the two rects align, plus anything past the minimum size requirement
            int rightShift = bottomNeighbor.Size.x - (distance + size.x) + (size.x - _minRoomSeedSize);
            shift.x = Random.Range( leftShift, rightShift + 1 );
        }


        int xResult = Mathf.Clamp( xOffset + shift.x, xOffset - _seedBuffer, xOffset + (_maxRoomSeedSize - size.x) + _seedBuffer );
        int yResult = Mathf.Clamp( yOffset + shift.y, yOffset - _seedBuffer, yOffset + (_maxRoomSeedSize - size.y) + _seedBuffer );
        
        return new Vector2Int(xResult, yResult) ;
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
    
    private LevelMaterial GetRandomRoomMaterial()
    {
        int types = Enum.GetNames(typeof(LevelMaterial)).Length;
        int rand = Random.Range( 0, types-1 );

        switch ( rand )
        {
            case 0:
                return LevelMaterial.Drywall;
            case 1:
                return LevelMaterial.Brick;
            default:
                return LevelMaterial.Drywall;
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

public struct LevelEdge
{
    public int Weight;
    public int Source;
    public int Destination;
}
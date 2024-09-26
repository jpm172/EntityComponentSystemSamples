using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
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
    private List<LevelWallEntity> _walls;
    private List<CompoundShape> _wallShapes;
    private LevelRoom[] _rooms;

    //cell variables
    private NativeHashMap<int, int4> _broadPhaseBounds;
    private NativeHashMap<int, int4> _floorBroadPhase;
    private NativeHashMap<int, LevelCell> _floorNarrowPhase;
    private NativeParallelMultiHashMap<int, LevelWall> _wallNarrowPhase;
    private Dictionary<int2, List<LevelConnection>> _roomConnections;

    private int _nextCellId = 1;
    
    //seeding variables
    public bool useSeed;
    [SerializeField]
    private int _minWallThickness = 1;
    [SerializeField]
    private int _maxWallThickness = 200;
    [SerializeField]
    private int _minRoomSeedSize = 20;
    [SerializeField]
    private int _maxRoomSeedSize = 60;
    [SerializeField]
    private int _seedBuffer = 20;


    //dijkstas variables
    private Dictionary<int, Dictionary<int,int>> _edgeDictionary;//stores the edge weights used for dijsktras
    private int minEdgeWeight = 1;
    [SerializeField]
    private int maxEdgeWeight = 10;
    

    //gizmos variables
    public bool Focus;
    public int FocusOnRoom;
    public bool ShowRoomBounds;
    public bool UseCompoundShapes;

    public bool TestCase;

    public int2[] TestPositions;
    public int2[] TestSizes;
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ( _rooms == null )
            return;

        Gizmos.color = Color.black;
        Vector3 dimPos = new Vector3(dimensions.x, dimensions.y)/2;
        dimPos /= GameSettings.PixelsPerUnit;
        Vector3 dimSize = new Vector3( dimensions.x, dimensions.y ) / GameSettings.PixelsPerUnit;
        Gizmos.DrawWireCube( dimPos, dimSize );
/*
        if ( Focus )
        {
            if ( !_narrowPhaseBounds.ContainsKey( FocusOnRoom ) )
                return;
            
            
            LevelRoom room = _rooms[FocusOnRoom - 1];
            
            NativeParallelMultiHashMap<int, LevelCell>.Enumerator cells = _narrowPhaseBounds.GetValuesForKey( room.Id );

            Vector3 pos;
            Vector3 size;
            while ( cells.MoveNext() )
            {
                LevelCell cell = cells.Current;
                pos = new Vector3(cell.Origin.x, cell.Origin.y) + new Vector3(cell.Size.x, cell.Size.y)/2;
                pos /= GameSettings.PixelsPerUnit;
                size = new Vector3( cell.Size.x, cell.Size.y ) / GameSettings.PixelsPerUnit;
                 
                Gizmos.color = room.DebugColor;
                Gizmos.DrawCube( pos, size );

            }
            
            Gizmos.color = Color.black;
            int4 bounds = _broadPhaseBounds[FocusOnRoom];
                
            pos = new Vector3(bounds.Origin.x, bounds.Origin.y) + new Vector3(bounds.Size.x, bounds.Size.y)/2;
            pos /= GameSettings.PixelsPerUnit;
            size = new Vector3( bounds.Size.x, bounds.Size.y ) / GameSettings.PixelsPerUnit;

            Gizmos.DrawWireCube( pos, size );
            
            return;
        }
        */
        
        foreach ( LevelRoom room in _rooms )
        {
            NativeParallelMultiHashMap<int, LevelWall>.Enumerator cells = _wallNarrowPhase.GetValuesForKey( room.Id );

            Vector3 pos;
            Vector3 size;
            while ( cells.MoveNext() )
            {
                LevelWall wall = cells.Current;
                pos = new Vector3(wall.Origin.x, wall.Origin.y) + new Vector3(wall.Size.x, wall.Size.y)/2;
                pos /= GameSettings.PixelsPerUnit;
                size = new Vector3( wall.Size.x, wall.Size.y ) / GameSettings.PixelsPerUnit;
                 
                Gizmos.color = room.DebugColor * Color.gray;
                Gizmos.DrawCube( pos, size );
            }

            if ( ShowRoomBounds )
            {
                Gizmos.color = Color.black;
                int4 bounds = _broadPhaseBounds[room.Id];
                
                pos = new Vector3(bounds.x, bounds.y) + new Vector3(bounds.Size().x, bounds.Size().y)/2;
                pos /= GameSettings.PixelsPerUnit;
                size = new Vector3( bounds.Size().x, bounds.Size().y ) / GameSettings.PixelsPerUnit;

                Gizmos.DrawWireCube( pos, size );
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube( pos, size );
            }
            
            
        }
        
        foreach ( LevelRoom room in _rooms )
        {
            NativeParallelMultiHashMap<int, LevelWall>.Enumerator cells = _wallNarrowPhase.GetValuesForKey( room.Id );

            Vector3 pos;
            Vector3 size;
            while ( cells.MoveNext() )
            {
                LevelWall wall = cells.Current;
                LevelCell cell = _floorNarrowPhase[wall.WallId];
                pos = new Vector3(cell.Origin.x, cell.Origin.y) + new Vector3(cell.Size.x, cell.Size.y)/2;
                pos /= GameSettings.PixelsPerUnit;
                size = new Vector3( cell.Size.x, cell.Size.y ) / GameSettings.PixelsPerUnit;
                 
                Gizmos.color = room.DebugColor;
                Gizmos.DrawCube( pos, size );
            }
            
            
            
        }

        
        Gizmos.color = Color.yellow;
        foreach ( int2 key in _roomConnections.Keys )
        {
            foreach ( LevelConnection cnct in _roomConnections[key] )
            {
                Gizmos.color = cnct.DebugColor;
                foreach ( int4 cell in cnct.Pieces )
                {
                    int2 cellSize = cell.Size();
                    Vector3 pos = new Vector3(cell.x, cell.y) + new Vector3(cellSize.x, cellSize.y)/2;
                    pos /= GameSettings.PixelsPerUnit;
                    Vector3 size = new Vector3( cellSize.x, cellSize.y ) / GameSettings.PixelsPerUnit;
                
                    Gizmos.DrawWireCube( pos, size );
                }
                
                
            }
        }

        
        Gizmos.color = Color.red;

        foreach ( LevelRoom room in _rooms )
        {
            if ( _edgeDictionary.ContainsKey( room.Id ) )
            {
                int4 roomBounds = _broadPhaseBounds[room.Id];
                Vector3 source = new Vector3(roomBounds.x, roomBounds.y) + new Vector3(roomBounds.Size().x, roomBounds.Size().y)/2;
                foreach (  KeyValuePair<int, int> neighbor in _edgeDictionary[room.Id])
                {
                    int4 neighborBounds = _broadPhaseBounds[neighbor.Key];
                    Vector3 destination = new Vector3(neighborBounds.x, neighborBounds.y)+ new Vector3(neighborBounds.Size().x, neighborBounds.Size().y)/2;;
                    
                    Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
                }
            }
        }

        if ( _wallShapes == null || !UseCompoundShapes )
        {
            return;
        }
        foreach ( CompoundShape cs in _wallShapes )
        {
            cs.DrawGizmos();
        }

    }
#endif
    

    public void GenerateLevel()
    {
        _nextCellId = 1;
        _counter = 0;
        if ( useSeed )
            Random.seed = seed;


        if ( TestCase )
        {
            TestInitializeLevel();
            return;
        }
            
        
        InitializeLevel();
        //VerifySeeds();
        
        
        //GrowRooms();
        //MakeFloors();
        //MakeWalls();
        //MakeEntities();
        
    }

    public void TestPath()
    {
        HasPath( 1 );
    }

    private bool HasPath(int startNode)
    {
        int INF = Int32.MaxValue;
        Dictionary<int, int> pathEdges = new Dictionary<int, int>();
        Dictionary<int, int> distances = new Dictionary<int, int>();
        List<int> path = new List<int>();

        //initialize the lists needed to run dijkstras alg
        distances[startNode] = 0;

        //run dijkstras algorithm until the path is found
        while ( path.Count < _rooms.Length )
        {
            int min = INF;
            int result = -1;

            //pick the node with the smallest distance to reach
            foreach ( int roomId in distances.Keys )
            {
                if ( distances[roomId] < min && !path.Contains( roomId ) )
                {
                    result = roomId;
                    min = distances[roomId];
                }
            }

            if ( result == -1 )
            {
                return false;
            }


            path.Add( result );

            //look at each of the edges, update any new connections that are shorter than the ones previously found
            foreach ( int neighborId in _edgeDictionary[result].Keys )
            {
                int weight = _edgeDictionary[result][neighborId];
                
                if ( !distances.ContainsKey(neighborId) || min + weight < distances[neighborId] )
                {
                    distances[neighborId] = min + weight;
                    pathEdges[neighborId] = result;
                }
                
            }
        }
        
        //reset all path information in the graph
        _edgeDictionary.Clear();
        foreach ( LevelRoom room in _rooms )
        {
            _edgeDictionary[room.Id] = new Dictionary<int, int>();
        }


        foreach ( int roomId in pathEdges.Keys )
        {
            if(roomId == startNode)
                continue;
            
            SetNeighbors( roomId, pathEdges[roomId], distances[roomId] );
        }

        return true;
    }
    

    public void MakeRoomMeshes()
    {
        
        foreach ( LevelRoom room in _rooms )
        {
            //StripMeshConstructor meshConstructor = new StripMeshConstructor();
            //room.Mesh = meshConstructor.ConstructMesh( _levelLayout, dimensions, room );
        }
    }
    
    private void InitializeLevel()
    {
        int count = layoutDimensions.x * layoutDimensions.y;

        if ( _rooms != null )
        {
            CleanUp();
        }
        
        _rooms = new LevelRoom[count];
        _edgeDictionary = new Dictionary<int, Dictionary<int,int>>();
        
        _wallNarrowPhase = new NativeParallelMultiHashMap<int, LevelWall>( _rooms.Length * 10, Allocator.Persistent );
        _floorNarrowPhase = new NativeHashMap<int, LevelCell>( _rooms.Length * 10, Allocator.Persistent );
        
        _floorBroadPhase = new NativeHashMap<int, int4>(_rooms.Length, Allocator.Persistent);
        _broadPhaseBounds = new NativeHashMap<int, int4>(_rooms.Length, Allocator.Persistent);
        _roomConnections = new Dictionary<int2, List<LevelConnection>>();
        
        
        int adjustedMaxSize = _maxRoomSeedSize + ( 2 * _maxWallThickness );
        int adjustedBuffer = _seedBuffer + _maxWallThickness;
        
        int xOffset = adjustedBuffer;
        //create the rooms and randomly shuffle them around, but while making sure they are still aligned with their neighbors
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            int yOffset = adjustedBuffer;

            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                
                //set the room's initial variables
                int wallThickness = Random.Range( _minWallThickness, _maxWallThickness + 1 );
                int2 wallVector = new int2(wallThickness, wallThickness);
                //int wallThickness =  (((x+y)%2 ) * _maxWallThickness ) + _minWallThickness;
                int weight = Random.Range( minEdgeWeight, maxEdgeWeight + 1 );
                
                int2 floorCellSize = new int2( 
                    Random.Range( _minRoomSeedSize, _maxRoomSeedSize  ),
                    Random.Range( _minRoomSeedSize , _maxRoomSeedSize  ) );

                int2 wallCellSize = floorCellSize + wallVector * 2;

                int2 roomSizeRatio = new int2( Random.Range( 1, 11 ), Random.Range( 1, 11 ) );
                int2 graphPosition = new int2(x,y);
                int2 floorOrigin = GetRandomAlignedRoomOrigin( x, y, xOffset , yOffset, floorCellSize );
                int2 wallOrigin = floorOrigin - wallVector;

                LevelMaterial mat = GetRandomRoomMaterial();
                LevelGrowthType growthType = LevelGrowthType.Normal;

                LevelRoom room = new LevelRoom(index+1, graphPosition,  roomSizeRatio, mat, wallThickness, weight, growthType);
                _rooms[index] = room;
                _edgeDictionary[room.Id] = new Dictionary<int, int>();
                
                _floorBroadPhase[room.Id] = new int4(floorOrigin, floorCellSize);
                _broadPhaseBounds[room.Id] = new int4(wallOrigin, wallCellSize);
                AddCell( room, floorOrigin, floorCellSize );

                //add all growth directions to the room
                int4[] xGrow = new[] {new int4( -1, 0,0,0 ), new int4( 0,0, 1, 0 )};
                int4[] yGrow = new[] {new int4( 0, -1,0,0 ), new int4( 0,0, 0, 1 )};
                
                room.XGrowthDirections.AddRange( xGrow );
                room.YGrowthDirections.AddRange( yGrow );

                yOffset += adjustedMaxSize + (adjustedBuffer*2) ;
            }
            
            xOffset += adjustedMaxSize + (adjustedBuffer*2) ;
        }
        
        //create the level array and seed it with the rooms  
        dimensions = new Vector2Int((adjustedMaxSize*layoutDimensions.x) + (adjustedBuffer*2*layoutDimensions.x) , (adjustedMaxSize*layoutDimensions.y) + (adjustedBuffer*2*layoutDimensions.y) );
    }

    private void TestInitializeLevel()
    {
        /*
        layoutDimensions.x = 2;
        layoutDimensions.y = 1;
        int count = layoutDimensions.x * layoutDimensions.y;
        
        if ( _rooms != null )
        {
            CleanUp();
        }
        
        _rooms = new LevelRoom[count];
        _edgeDictionary = new Dictionary<int, Dictionary<int,int>>();

        _narrowPhaseBounds = new NativeParallelMultiHashMap<int, LevelCell>( _rooms.Length * 10, Allocator.Persistent );
        _broadPhaseBounds = new NativeHashMap<int, int4>(_rooms.Length, Allocator.Persistent);
        _roomConnections = new Dictionary<int2, List<LevelConnection>>();
        
        int adjustedMaxSize = _maxRoomSeedSize + ( 2 * _maxWallThickness );
        int adjustedBuffer = _seedBuffer + _maxWallThickness;
        
        //create the rooms and randomly shuffle them around, but while making sure they are still aligned with their neighbors
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                
                //set the room's initial variables
                int wallThickness = Random.Range( _minWallThickness, _maxWallThickness + 1 );
                //int wallThickness =  (((x+y)%2 ) * _maxWallThickness ) + _minWallThickness;
                int weight = Random.Range( minEdgeWeight, maxEdgeWeight + 1 );

                int2 roomSize = TestSizes[index];
                
                int2 roomSizeRatio = new int2( Random.Range( 1, 11 ), Random.Range( 1, 11 ) );
                int2 graphPosition = new int2(x,y);
                int2 roomOrigin = TestPositions[index];

                LevelMaterial mat = GetRandomRoomMaterial();
                LevelGrowthType growthType = LevelGrowthType.Normal;

                LevelRoom room = new LevelRoom(index+1, graphPosition, roomOrigin, roomSize, roomSizeRatio, mat, wallThickness, weight, growthType);
                _rooms[index] = room;
                _edgeDictionary[room.Id] = new Dictionary<int, int>();
                
                _broadPhaseBounds[room.Id] = room.Bounds;
                AddCell( room, roomOrigin, roomSize );

                if ( index+1 == 2 )
                {
                    //AddCell( room, roomOrigin + new int2(0,5), roomSize );
                }
                

                //add all growth directions to the room
                int2[] xGrow = new[] {new int2( -1, 0 ), new int2( 1, 0 )};
                int2[] yGrow = new[] {new int2( 0, -1 ), new int2( 0, 1 )};
                
                room.XGrowthDirections.AddRange( xGrow );
                room.YGrowthDirections.AddRange( yGrow );
            }
            
        }
        
        //create the level array and seed it with the rooms  
        dimensions = new Vector2Int((adjustedMaxSize*layoutDimensions.x) + (adjustedBuffer*2*layoutDimensions.x) , (adjustedMaxSize*layoutDimensions.y) + (adjustedBuffer*2*layoutDimensions.y) );
    */
    }

    private void AddCell(LevelRoom room, int2 origin, int2 size)
    {
        LevelCell newCell = new LevelCell( _nextCellId, origin, size );
       // newCell.Bounds += new int4(-1, -1, 1, 1) * room.WallThickness;
        
        int2 wallVector = new int2(room.WallThickness, room.WallThickness); 
        int2 floorVector = new int2(room.WallThickness, room.WallThickness); //*****DEBUG*********
        LevelWall newWall = new LevelWall(_nextCellId,origin - floorVector, size + wallVector*2, room.WallThickness);


        UpdateBroadPhase( room, newWall );
        _wallNarrowPhase.Add( room.Id, newWall );
        _floorNarrowPhase.Add( _nextCellId, newCell );

        _nextCellId++;
        room.CellCount++;
    }

    private void AddWall( LevelRoom room, LevelWall newWall )
    {
        newWall.WallId = _nextCellId;
        int wt = newWall.Thickness;
        int4 cellBounds = newWall.Bounds + new int4( wt, wt, -wt, -wt );
        LevelCell innerCell = new LevelCell(_nextCellId, cellBounds);
        
        UpdateBroadPhase( room, newWall );
        _wallNarrowPhase.Add( room.Id, newWall );
        _floorNarrowPhase.Add( _nextCellId, innerCell );

        _nextCellId++;
        room.CellCount++;
    }


    public void MakeCompoundShapes()
    {
        _wallShapes = new List<CompoundShape>();

        foreach ( LevelRoom room in _rooms )
        {
            CompoundShape curShape = new CompoundShape();
            foreach ( LevelWall wall in _wallNarrowPhase.GetValuesForKey( room.Id ) )
            {
                int4 floorBounds = _floorNarrowPhase[wall.WallId].Bounds;
                curShape.AddEmptySpace( floorBounds );
                curShape.AddShape( wall.Bounds );
                //curShape.AddShape( wall.Bounds, floorBounds );
            }
            if(room.GraphPosition.Equals(new int2(1,0)))
            {}
            curShape.CreateShape();
            _wallShapes.Add( curShape );
        }
    }

    private void UpdateBroadPhase( LevelRoom room, LevelCell newCell )
    {
        int4 bounds = _broadPhaseBounds[room.Id];
        bounds.xy = math.min( newCell.Bounds.xy, bounds.xy );
        bounds.zw = math.max( newCell.Bounds.zw, bounds.zw );

        _broadPhaseBounds[room.Id] = bounds;
    }

    private void UpdateBroadPhase( LevelRoom room, LevelWall newWall )
    {
        int4 bounds = _broadPhaseBounds[room.Id];
        bounds.xy = math.min( newWall.Bounds.xy, bounds.xy );
        bounds.zw = math.max( newWall.Bounds.zw, bounds.zw );

        _broadPhaseBounds[room.Id] = bounds;
    }
    
    private void SetNeighbors( int room1, int room2, int weight )
    {
        _edgeDictionary[room1][room2] = weight;
        _edgeDictionary[room2][room1] = weight;
    }
    
    
    
    private int2 GetRandomAlignedRoomOrigin(int x, int y, int xOffset, int yOffset,  int2 size)
    {
        int2 shift = new int2(0,0);

        if ( x == 0 )
        {
            shift.y = Random.Range( -_seedBuffer,  _seedBuffer + 1 );
        }
        else
        {
            int index = ( x - 1 ) + y * layoutDimensions.x;
            LevelRoom leftNeighbor = _rooms[index];
            int4 leftBounds = _floorBroadPhase[leftNeighbor.Id];

            int walkable = size.y;
            int walkableNeighbor = leftBounds.Size().y;
            int distance = yOffset - leftBounds.y;
            
            int extraSteps = walkable - _minRoomSeedSize;
            
            //equal to the steps needed to have the bottoms of the rects to align, plus anything past the minimum size requirement
            int downShift = -(distance + extraSteps );
            //equal to the steps needed to have the tops of the two rects align, plus anything past the minimum size requirement
            int upShift = walkableNeighbor - ( distance + walkable ) + extraSteps;
            
            int inclusive = upShift >= 0 ? 1 : -1;
            shift.y = Random.Range( downShift, upShift + inclusive );//todo what if upshift is negative, what does hte +1 need to be?
        }

        if ( y == 0 )
        {
            shift.x = Random.Range( -_seedBuffer, _seedBuffer  + 1 );
        }
        else
        {
            int index = x + (y-1) * layoutDimensions.x;
            LevelRoom bottomNeighbor = _rooms[index];
            int4 bottomBounds = _floorBroadPhase[bottomNeighbor.Id];
            
            int walkable = size.x ;
            int walkableNeighbor = bottomBounds.Size( ).x;
            int distance = xOffset - bottomBounds.x ;

            int extraSteps = walkable - _minRoomSeedSize;

            //equal to the steps needed to have the left side of the rects to align, plus anything past the minimum size requirement
            int leftShift = -(distance + extraSteps);
            //equal to the steps needed to have the right side of the two rects align, plus anything past the minimum size requirement
            int rightShift = walkableNeighbor - (distance + walkable) + extraSteps;

            int inclusive = rightShift >= 0 ? 1 : -1;
            shift.x = Random.Range( leftShift, rightShift + inclusive );
        }


        int xResult = Mathf.Clamp( xOffset + shift.x, xOffset - _seedBuffer , xOffset  + _seedBuffer  );
        int yResult = Mathf.Clamp( yOffset + shift.y, yOffset - _seedBuffer , yOffset  + _seedBuffer  );
        
        
        return new int2(xResult, yResult) ;
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
                
                LevelWallEntity newWall = new LevelWallEntity
                {
                   Mesh = mesh,
                   Material = wallMaterial,
                   PointField = solidPointField,
                   Position = position
                };
                
                //_walls.Add( newWall  );
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
        CleanUp();
            
    }

    private void CleanUp()
    {
        _broadPhaseBounds.Dispose();
        _wallNarrowPhase.Dispose();
        _floorNarrowPhase.Dispose();
        _floorBroadPhase.Dispose();
    }
}

public struct LevelFloor
{
    public Mesh FloorMesh;
    public Material FloorMaterial;
    public Vector2 Position;
}

public struct LevelWallEntity
{
    public Mesh Mesh;
    public Material Material;
    public Vector2 Position;
    public int[] PointField;
}

public struct LevelEdge
{
    public int Source;
    public int Destination;
    public int Weight;


    public override bool Equals( object obj )
    {
        if ( obj == null )
            return false;

        LevelEdge edge = (LevelEdge) obj;

        return ( edge.Source == Source && edge.Destination == Destination );
    }
}

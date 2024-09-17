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
    private List<LevelWall> _walls;
    private LevelRoom[] _rooms;

    //cell variables
    private NativeHashMap<int, IntBounds> _broadPhaseBounds;
    private NativeParallelMultiHashMap<int, LevelCell> _narrowPhaseBounds;
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
    //private Dictionary<int, List<LevelEdge>> _edgeDictionary;//stores the edge weights used for dijsktras
    private Dictionary<int, Dictionary<int,int>> _edgeDictionary;//stores the edge weights used for dijsktras
    private int minEdgeWeight = 1;
    [SerializeField]
    private int maxEdgeWeight = 10;
    

    //gizmos variables
    public bool Focus;
    public int FocusOnRoom;
    public bool ShowRoomBounds;
    public bool UseMeshes;
    public bool UseWireMeshes;

    public bool TestCase;

    public int2[] TestPositions;
    public int2[] TestSizes;
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ( _rooms == null )
            return;
        
/*
        if ( UseMeshes || UseWireMeshes )
        {
            foreach ( LevelRoom room in _rooms )
            {
                if ( UseMeshes )
                {
                    Gizmos.color = room.DebugColor;
                    Gizmos.DrawMesh(  room.Mesh );
                }

                if ( UseWireMeshes )
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireMesh(  room.Mesh );
                }

                if ( _edgeDictionary.ContainsKey( room.Index ) )
                {
                    Gizmos.color = Color.red;
                    foreach ( LevelEdge e in _edgeDictionary[room.Index] )
                    {
                        Vector3 source = new Vector3(_rooms[e.Source].Origin.x, _rooms[e.Source].Origin.y) + new Vector3(_rooms[e.Source].Size.x, _rooms[e.Source].Size.y)/2;
                        Vector3 destination = new Vector3(_rooms[e.Destination].Origin.x, _rooms[e.Destination].Origin.y)+ new Vector3(_rooms[e.Destination].Size.x, _rooms[e.Destination].Size.y)/2;;
                    
                        Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
                    
                    }
                }
                
            }
            return;
        }
        */
        
        Gizmos.color = Color.black;
        Vector3 dimPos = new Vector3(dimensions.x, dimensions.y)/2;
        dimPos /= GameSettings.PixelsPerUnit;
        Vector3 dimSize = new Vector3( dimensions.x, dimensions.y ) / GameSettings.PixelsPerUnit;
        Gizmos.DrawWireCube( dimPos, dimSize );

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
            IntBounds bounds = _broadPhaseBounds[FocusOnRoom];
                
            pos = new Vector3(bounds.Origin.x, bounds.Origin.y) + new Vector3(bounds.Size.x, bounds.Size.y)/2;
            pos /= GameSettings.PixelsPerUnit;
            size = new Vector3( bounds.Size.x, bounds.Size.y ) / GameSettings.PixelsPerUnit;

            Gizmos.DrawWireCube( pos, size );
            
            return;
        }
        
        foreach ( LevelRoom room in _rooms )
        {

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
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube( pos, size );
            }

            if ( ShowRoomBounds )
            {
                Gizmos.color = Color.black;
                IntBounds bounds = _broadPhaseBounds[room.Id];
                
                pos = new Vector3(bounds.Origin.x, bounds.Origin.y) + new Vector3(bounds.Size.x, bounds.Size.y)/2;
                pos /= GameSettings.PixelsPerUnit;
                size = new Vector3( bounds.Size.x, bounds.Size.y ) / GameSettings.PixelsPerUnit;

                Gizmos.DrawWireCube( pos, size );
            }
            
            
        }

        
        Gizmos.color = Color.yellow;
        foreach ( int2 key in _roomConnections.Keys )
        {
            foreach ( LevelConnection cnct in _roomConnections[key] )
            {
                Gizmos.color = cnct.DebugColor;
                foreach ( IntBounds cell in cnct.Pieces )
                {
                    Vector3 pos = new Vector3(cell.Origin.x, cell.Origin.y) + new Vector3(cell.Size.x, cell.Size.y)/2;
                    pos /= GameSettings.PixelsPerUnit;
                    Vector3 size = new Vector3( cell.Size.x, cell.Size.y ) / GameSettings.PixelsPerUnit;
                
                    Gizmos.DrawWireCube( pos, size );
                }
                
                
            }
        }

        
        Gizmos.color = Color.red;

        foreach ( LevelRoom room in _rooms )
        {
            if ( _edgeDictionary.ContainsKey( room.Id ) )
            {
                Vector3 source = new Vector3(room.Origin.x, room.Origin.y) + new Vector3(room.Size.x, room.Size.y)/2;
                foreach (  KeyValuePair<int, int> neighbor in _edgeDictionary[room.Id])
                {

                    int neighborIndex = neighbor.Key -1;
                    Vector3 destination = new Vector3(_rooms[neighborIndex].Origin.x, _rooms[neighborIndex].Origin.y)+ new Vector3(_rooms[neighborIndex].Size.x, _rooms[neighborIndex].Size.y)/2;;
                    
                    Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
                }
            }
        }
        /*
        for ( int i = 0; i < _rooms.Length; i++ )
        {
            if ( _edgeDictionary.ContainsKey( i ) )
            {
                foreach ( LevelEdge e in _edgeDictionary[i] )
                {
                    
                    Vector3 source = new Vector3(_rooms[e.Source].Origin.x, _rooms[e.Source].Origin.y) + new Vector3(_rooms[e.Source].Size.x, _rooms[e.Source].Size.y)/2;
                    Vector3 destination = new Vector3(_rooms[e.Destination].Origin.x, _rooms[e.Destination].Origin.y)+ new Vector3(_rooms[e.Destination].Size.x, _rooms[e.Destination].Size.y)/2;;
                    
                    Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
                    
                }
            }
        }
        */
        
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
        //int[] distance = new int[_rooms.Length];
        //int[] pathEdges = new int[_rooms.Length];
        List<int> path = new List<int>();

        //initialize the lists needed to run dijkstras alg
        distances[startNode] = 0;

        if ( breakPoint )
        {

        }

        
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
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        _edgeDictionary = new Dictionary<int, Dictionary<int,int>>();
        
        _narrowPhaseBounds = new NativeParallelMultiHashMap<int, LevelCell>( _rooms.Length * 10, Allocator.Persistent );
        _broadPhaseBounds = new NativeHashMap<int, IntBounds>(_rooms.Length, Allocator.Persistent);
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
                //int wallThickness =  (((x+y)%2 ) * _maxWallThickness ) + _minWallThickness;
                int weight = Random.Range( minEdgeWeight, maxEdgeWeight + 1 );
                
                int2 roomSize = new int2( 
                    Random.Range( _minRoomSeedSize + (wallThickness*2), _maxRoomSeedSize + (wallThickness*2) + 1 ),
                    Random.Range( _minRoomSeedSize + (wallThickness*2), _maxRoomSeedSize + (wallThickness*2) + 1 ) );
                
                int2 roomSizeRatio = new int2( Random.Range( 1, 11 ), Random.Range( 1, 11 ) );
                int2 graphPosition = new int2(x,y);
                int2 roomOrigin = GetRandomAlignedRoomOrigin( x, y, xOffset , yOffset, wallThickness, roomSize );

                LevelMaterial mat = GetRandomRoomMaterial();
                LevelGrowthType growthType = LevelGrowthType.Normal;
/*
                roomSize += new int2( 
                    Random.Range( _minRoomSeedSize + (wallThickness*2), _maxRoomSeedSize + (wallThickness*2) + 1 ),
                    Random.Range( _minRoomSeedSize + (wallThickness*2), _maxRoomSeedSize + (wallThickness*2) + 1 ) );
                    */
                
                LevelRoom room = new LevelRoom(index+1, graphPosition, roomOrigin, roomSize, roomSizeRatio, mat, wallThickness, weight, growthType);
                _rooms[index] = room;
                _edgeDictionary[room.Id] = new Dictionary<int, int>();
                
                _broadPhaseBounds[room.Id] = room.Bounds;
                AddCell( room, roomOrigin, roomSize );

                //add all growth directions to the room
                int2[] xGrow = new[] {new int2( -1, 0 ), new int2( 1, 0 )};
                int2[] yGrow = new[] {new int2( 0, -1 ), new int2( 0, 1 )};
                
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
        layoutDimensions.x = 2;
        layoutDimensions.y = 1;
        int count = layoutDimensions.x * layoutDimensions.y;
        
        if ( _rooms != null )
        {
            CleanUp();
        }
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        _edgeDictionary = new Dictionary<int, Dictionary<int,int>>();

        _narrowPhaseBounds = new NativeParallelMultiHashMap<int, LevelCell>( _rooms.Length * 10, Allocator.Persistent );
        _broadPhaseBounds = new NativeHashMap<int, IntBounds>(_rooms.Length, Allocator.Persistent);
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
    }

    private void AddCell(LevelRoom room, int2 origin, int2 size)
    {
        LevelCell newCell = new LevelCell( _nextCellId, origin, size );
        UpdateBroadPhase( room, newCell );
        _narrowPhaseBounds.Add( room.Id, newCell );

        _nextCellId++;
        room.CellCount++;
    }
    
    private void AddCell(LevelRoom room, LevelCell newCell)
    {
        newCell.CellId = _nextCellId;
        UpdateBroadPhase( room, newCell );
        _narrowPhaseBounds.Add( room.Id, newCell );

        _nextCellId++;
        room.CellCount++;
    }

    private void UpdateBroadPhase( LevelRoom room, LevelCell newCell )
    {
        IntBounds bounds = _broadPhaseBounds[room.Id];
        bounds.Bounds.xy = math.min( newCell.Bounds.Bounds.xy, bounds.Bounds.xy );
        bounds.Bounds.zw = math.max( newCell.Bounds.Bounds.zw, bounds.Bounds.zw );

        _broadPhaseBounds[room.Id] = bounds;
    }

    private void SetNeighbors( int room1, int room2, int weight )
    {

        _edgeDictionary[room1][room2] = weight;
        _edgeDictionary[room2][room1] = weight;
        

        //_edgeDictionary[room1].Add( new LevelEdge{Source = room1,Destination = room2, Weight = weight} );
        //_edgeDictionary[room2].Add( new LevelEdge{Source = room2, Destination = room1, Weight = weight} );
    }
    
    
    
    private int2 GetRandomAlignedRoomOrigin(int x, int y, int xOffset, int yOffset, int wallThickness,  int2 size)
    {
        
        int2 shift = new int2(0,0);
        
        int adjustedMaxSize = _maxRoomSeedSize + ( 2 * _maxWallThickness );

        if ( x == 0 )
        {
            shift.y = Random.Range( -(_seedBuffer+wallThickness),  _seedBuffer + wallThickness + (adjustedMaxSize - size.x)+ 1 );
        }
        else
        {
            int index = ( x - 1 ) + y * layoutDimensions.x;
            LevelRoom leftNeighbor = _rooms[index];
            
            int walkable = size.y - 2 * wallThickness;
            int walkableNeighbor = leftNeighbor.Size.y - 2 * leftNeighbor.WallThickness;
            int distance = (yOffset + wallThickness) - (leftNeighbor.Origin.y + leftNeighbor.WallThickness);
            
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
            shift.x = Random.Range( -(_seedBuffer+wallThickness), _seedBuffer + wallThickness + (adjustedMaxSize - size.x) + 1 );
        }
        else
        {
            int index = x + (y-1) * layoutDimensions.x;
            LevelRoom bottomNeighbor = _rooms[index];
            
            int walkable = size.x - 2 * wallThickness;
            int walkableNeighbor = bottomNeighbor.Size.x - 2 * bottomNeighbor.WallThickness;
            int distance = (xOffset + wallThickness) - (bottomNeighbor.Origin.x + bottomNeighbor.WallThickness);

            int extraSteps = walkable - _minRoomSeedSize;

            //equal to the steps needed to have the left side of the rects to align, plus anything past the minimum size requirement
            int leftShift = -(distance + extraSteps);
            //equal to the steps needed to have the right side of the two rects align, plus anything past the minimum size requirement
            int rightShift = walkableNeighbor - (distance + walkable) + extraSteps;

            int inclusive = rightShift >= 0 ? 1 : -1;
            shift.x = Random.Range( leftShift, rightShift + inclusive ); 
            
            
        }


        int xResult = Mathf.Clamp( xOffset + shift.x, xOffset - _seedBuffer - wallThickness, xOffset + (adjustedMaxSize - size.x) + _seedBuffer + wallThickness );
        int yResult = Mathf.Clamp( yOffset + shift.y, yOffset - _seedBuffer - wallThickness, yOffset + (adjustedMaxSize - size.y) + _seedBuffer + wallThickness );
        
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


    private bool VerifySeeds()
    {

        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                LevelRoom curRoom = _rooms[index];
                if ( x > 0 )
                {
                    int neighborIndex = ( x - 1 ) + y * layoutDimensions.x;
                    LevelRoom leftNeighbor = _rooms[neighborIndex];
                    if ( !IsAlignedWithRoom( curRoom, leftNeighbor, true ) )
                        return false;
                }

                if (  x < layoutDimensions.x-1 )
                {
                    int neighborIndex = ( x + 1 ) + y * layoutDimensions.x;
                    LevelRoom rightNeighbor = _rooms[neighborIndex];
                    if ( !IsAlignedWithRoom( curRoom, rightNeighbor, true ) )
                        return false;
                }

                if ( y > 0 )
                {
                    int neighborIndex = x + (y-1) * layoutDimensions.x;
                    LevelRoom bottomNeighbor = _rooms[neighborIndex];
                    if ( !IsAlignedWithRoom( curRoom, bottomNeighbor, false ) )
                        return false;
                }
                
                if ( y < layoutDimensions.y-1 )
                {
                    int neighborIndex = x + (y+1) * layoutDimensions.x;
                    LevelRoom topNeighbor = _rooms[neighborIndex];
                    if ( !IsAlignedWithRoom( curRoom, topNeighbor, false ) )
                        return false;
                }
            }
        }
        
        return true;
    }

    private bool IsAlignedWithRoom( LevelRoom room1, LevelRoom room2, bool xAxis )
    {
        int count = 0;

        if ( xAxis )
        {
            for ( int y = room1.Origin.y; y < room1.Origin.y + room1.Size.y; y++ )
            {
                if ( y >= room2.Origin.y && y < room2.Origin.y + room2.Size.y )
                {
                    /*
                    Vector3 from = new Vector3(room1.Origin.x, y)/GameSettings.PixelsPerUnit;
                    Vector3 to = new Vector3(room2.Origin.x, y)/GameSettings.PixelsPerUnit;
                    Debug.DrawLine( from, to, Color.red, 20 );
                    */
                    count++;
                }
            }
        }
        else
        {
            for ( int x = room1.Origin.x; x < room1.Origin.x + room1.Size.x; x++ )
            {
                if ( x >= room2.Origin.x && x < room2.Origin.x + room2.Size.x )
                {
                    count++;
                }
            }
        }
        
        
        int required = math.min( _minRoomSeedSize + room1.WallThickness * 2,
            _minRoomSeedSize + room2.WallThickness * 2 );

        bool result = count >= required;

        if ( !result )
        {
            Debug.Log($"{room1.GraphPosition} and {room2.GraphPosition} == {count}" );
        }
        
        return result;
    }
    
    private void OnDestroy()
    {
        CleanUp();
            
    }

    private void CleanUp()
    {
        _broadPhaseBounds.Dispose();
        _narrowPhaseBounds.Dispose();
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

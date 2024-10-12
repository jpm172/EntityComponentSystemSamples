using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using Collider = Unity.Physics.Collider;
using Material = UnityEngine.Material;
using Random = UnityEngine.Random;

public partial class LevelGenerator : MonoBehaviour
{

    [SerializeField] private int seed;
    
    [SerializeField]
    private int2 dimensions;//dimensions of level in pixels
    
    [SerializeField]
    private int2 layoutDimensions;//dimensions of level in room nodes

    [SerializeField]
    private Material[] floorMaterials;
    
    [SerializeField]
    private List<LevelMaterial> _textureKeys;
    [SerializeField]
    private List<Texture2D> _textureValues;

    private Dictionary<LevelMaterial, Texture2D> _textureDict;

    [SerializeField]
    private Material wallMaterial;
    
    private List<LevelFloor> _floors;
    private List<LevelWall> _walls;
    private LevelRoom[] _rooms;
    
    
    private NativeArray<int> _levelLayout;
    private NativeArray<RoomInfo> _roomInfo; //holds room info needed in jobs
    private List<LevelMaterial> _matertialsUsed;
    
    private Dictionary<int2, List< LevelConnectionManager>> _roomConnections;
    //seeding variables
    public bool useSeed;
    [SerializeField]
    private int _minWallThickness = 1;
    [SerializeField]
    private int _maxWallThickness = 5;
    [SerializeField]
    private int _minRoomSeedSize = 20;
    [SerializeField]
    private int _maxRoomSeedSize = 60;
    [SerializeField]
    private int _seedBuffer = 20;

    //dijkstas variables
    private Dictionary<int, Dictionary<int,int>> _edgeDictionary;//stores the edge weights used for dijsktras
    private NativeArray<int> _adjacencyMatrix;//stores the edge weights for burst dijsktras
    private int minEdgeWeight = 1;
    [SerializeField]
    private int maxEdgeWeight = 10;
    

    //gizmos variables
    public bool UseMeshes;
    public bool ShowFloorMesh;
    public bool ShowWallMesh;
    public bool UseWireMeshes;
    public bool UseConnections;
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ( !_levelLayout.IsCreated )
            return;

        if ( UseMeshes || UseWireMeshes )
        {
            foreach ( LevelRoom room in _rooms )
            {
                if ( UseMeshes )
                {
                    if ( ShowFloorMesh )
                    {
                        Gizmos.color = room.DebugColor;
                        Gizmos.DrawMesh(  room.FloorMesh );
                    }

                    if ( ShowWallMesh )
                    {
                        Gizmos.color = room.DebugColor *new Color(.3f, .3f, .3f,1);
                        Gizmos.DrawMesh( room.WallMesh );
                    }
                    
                }

                if ( UseWireMeshes )
                {
                    Gizmos.color = Color.black;
                    if ( ShowFloorMesh )
                    {
                        Gizmos.DrawWireMesh(  room.FloorMesh );
                    }
                    if ( ShowWallMesh )
                    {
                        Gizmos.DrawWireMesh( room.WallMesh );
                    }
                }

                if ( _edgeDictionary.ContainsKey( room.Id ) )
                {
                    Gizmos.color = Color.red;
                    int4 roomBounds = _rooms[room.Index].Bounds;
                    Vector3 source = new Vector3(roomBounds.x, roomBounds.y) + new Vector3(roomBounds.Size().x, roomBounds.Size().y)/2;
                    foreach (  KeyValuePair<int, int> neighbor in _edgeDictionary[room.Id])
                    {
                        int4 neighborBounds = _rooms[neighbor.Key-1].Bounds;
                        Vector3 destination = new Vector3(neighborBounds.x, neighborBounds.y)+ new Vector3(neighborBounds.Size().x, neighborBounds.Size().y)/2;;
                    
                        Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
                    }
                }
            }
            
            
            
            return;
        }
        
        Vector3 size = Vector3.one / GameSettings.PixelsPerUnit;
        for ( int x = 0; x < dimensions.x; x++ )
        {
            for ( int y = 0; y < dimensions.y; y++ )
            {
                int index = x + y * dimensions.x;
                if ( _levelLayout[index] > 0 )
                {
                    if ( _levelLayout[index] > _rooms.Length )
                    {
                        LevelRoom room = _rooms[_levelLayout[index] - _rooms.Length - 1];
                        Gizmos.color = room.DebugColor *new Color(.3f, .3f, .3f,1);
                    }
                    else
                    {
                        LevelRoom room = _rooms[_levelLayout[index] - 1];
                        Gizmos.color = room.DebugColor;
                    }
                    
                    Vector3 pos = new Vector3(x,y)/GameSettings.PixelsPerUnit;
                    
                    
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

        
        foreach ( int2 key in _roomConnections.Keys )
        {
            if(!UseConnections)
                break;
            
            foreach ( LevelConnectionManager cnct in _roomConnections[key] )
            {
                Gizmos.color = cnct.DebugColor;
                foreach ( int4 cell in cnct.Pieces )
                {
                    int2 cellSize = cell.Size();
                    Vector3 pos = new Vector3(cell.x, cell.y) + new Vector3(cellSize.x-1, cellSize.y -1)/2;
                    pos /= GameSettings.PixelsPerUnit;
                    size = new Vector3( cellSize.x, cellSize.y ) / GameSettings.PixelsPerUnit;
                    if ( cnct.GetLargestDimension() >= _minRoomSeedSize )
                    {
                        Gizmos.DrawCube( pos, size );
                    }
                    else
                    {
                        Gizmos.DrawWireCube( pos, size );
                    }
                    
                }
            }
        }
        
        Gizmos.color = Color.red;
        foreach ( LevelRoom room in _rooms )
        {
            if ( _edgeDictionary.ContainsKey( room.Id ) )
            {
                int4 roomBounds = _rooms[room.Index].Bounds;
                Vector3 source = new Vector3(roomBounds.x, roomBounds.y) + new Vector3(roomBounds.Size().x, roomBounds.Size().y)/2;
                foreach (  KeyValuePair<int, int> neighbor in _edgeDictionary[room.Id])
                {
                    int4 neighborBounds = _rooms[neighbor.Key-1].Bounds;
                    Vector3 destination = new Vector3(neighborBounds.x, neighborBounds.y)+ new Vector3(neighborBounds.Size().x, neighborBounds.Size().y)/2;;
                    
                    Gizmos.DrawLine( source/GameSettings.PixelsPerUnit, destination/GameSettings.PixelsPerUnit );
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
        _counter = 0;
        if ( useSeed )
            Random.seed = seed;

        CreateTextureDictionary();
        InitializeLevel();
        GrowRooms();
        MakeDoorways();
        
        //MakeRoomMeshes();
        MakeFloors();
        MakeWalls();
        MakeEntities();
    }

    private void CreateTextureDictionary()
    {
        _textureDict = new Dictionary<LevelMaterial, Texture2D>();
        for ( int i = 0; i < _textureKeys.Count; i++ )
        {
            _textureDict[_textureKeys[i]] = _textureValues[i];
        }
    }
    
    /// <summary>
    /// Makes doorways between the rooms corresponding to the path found from dijkstras
    /// </summary>
    public void MakeDoorways()
    {
        HashSet<int2> connectionsMade = new HashSet<int2>();
        foreach ( LevelRoom room in _rooms )
        {
            foreach ( KeyValuePair<int, int> cnct in _edgeDictionary[room.Id] )
            {
                int2 key = new int2(math.min(room.Id, cnct.Key), math.max(room.Id, cnct.Key));
                if ( !connectionsMade.Contains( key ) )
                {
                    connectionsMade.Add( key );
                    MakeDoorway( key );
                }
            }
        }
    }

    private void MakeDoorway( int2 key )
    {
        int largest = -1;
        int index = -1;
        LevelConnectionManager result = null;
        for ( int i = 0; i <_roomConnections[key].Count; i++ )
        {
            LevelConnectionManager cnct = _roomConnections[key][i];
            if ( cnct.GetLargestDimension() >= _minRoomSeedSize )
            {
                //use the bounds of the connection to solve race condition
                if ( result == null || largest < cnct.Hash )
                {
                    largest = cnct.Hash;
                    result = cnct;
                    index = i;
                }
            }
        }
        
        PlaceDoorAtConnection( result, out List<LevelConnectionManager> cncts);
        _roomConnections[key].RemoveAt( index );
        _roomConnections[key].AddRange( cncts );
        
    }

    private void PlaceDoorAtConnection( LevelConnectionManager cnct, out List<LevelConnectionManager> newCncts )
    {
        
        NativeArray<int4> pieces = cnct.GetRandomSpan(_minRoomSeedSize, out int4 doorBounds, out newCncts);

        LevelConvertToFloorJob convertJob = new LevelConvertToFloorJob
        {
            LevelLayout = _levelLayout,
            LevelDimensions = dimensions,
            Pieces = pieces,
            RoomCount = _rooms.Length
        };

        JobHandle convertHandle = convertJob.Schedule( pieces.Length, 1 );
        convertHandle.Complete();

        pieces.Dispose();
    }
    
    private bool BurstHasPath(int startNode)
    {
        NativeArray<int> jobPath = new NativeArray<int>(_rooms.Length, Allocator.TempJob);
        NativeReference<bool> success = new NativeReference<bool>(false, Allocator.TempJob);
        NativeArray<int> jobDistances = new NativeArray<int>(_rooms.Length, Allocator.TempJob);
        NativeArray<bool> sptSet = new NativeArray<bool>(_rooms.Length, Allocator.TempJob);

        DijkstrasPathJob pathJob = new DijkstrasPathJob
        {
            AdjacencyMatrix = _adjacencyMatrix,
            StartRoom = startNode,
            RoomCount = _rooms.Length,
            Success = success,
            Path = jobPath,
            Distances = jobDistances,
            sptSet = sptSet
        };

        JobHandle pathHandle = pathJob.Schedule();
        pathHandle.Complete();

        if ( !success.Value )
        {
            success.Dispose();
            jobPath.Dispose();
            sptSet.Dispose();
            jobDistances.Dispose();
            return false;
            
        }
        
        //reset all path information in the graph
        _edgeDictionary.Clear();
        foreach ( LevelRoom room in _rooms )
        {
            _edgeDictionary[room.Id] = new Dictionary<int, int>();
        }
        
        
        for ( int i = 0; i < jobPath.Length; i++ )
        {
            int roomId = i + 1;
            //skip past the start node since it is not assigned a connection in the algorithm
            if(roomId == startNode)
                continue;
            
            SetNeighbors( roomId, jobPath[i]+1, jobDistances[i] );
        }

        success.Dispose();
        jobPath.Dispose();
        sptSet.Dispose();
        jobDistances.Dispose();
        
        
        return true;
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

        //set up the graph to match the path we just found
        foreach ( int roomId in pathEdges.Keys )
        {
            //skip past the start node since it is not assigned a connection in the algorithm
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
            StripMeshConstructor meshConstructor = new StripMeshConstructor();
            room.FloorMesh = meshConstructor.ConstructMesh( _levelLayout, dimensions, room, room.Id );
            meshConstructor = new StripMeshConstructor();
            room.WallMesh = meshConstructor.ConstructMesh( _levelLayout, dimensions, room, room.WallId );
        }
    }
    
    private void InitializeLevel()
    {
        int count = layoutDimensions.x * layoutDimensions.y;
        CleanUp(false);
        
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        _rooms = new LevelRoom[count];
        _edgeDictionary = new Dictionary<int, Dictionary<int,int>>();
        _roomInfo = new NativeArray<RoomInfo>(count, Allocator.Persistent);
        _roomConnections = new Dictionary<int2, List<LevelConnectionManager>>();
        _adjacencyMatrix = new NativeArray<int>(count*count, Allocator.Persistent);
        _matertialsUsed = new List<LevelMaterial>();
        _collidersMade = new List<BlobAssetReference<Collider>>();
        
        _matertialsUsed.Add( LevelMaterial.Indestructible );

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
                if(!_matertialsUsed.Contains( mat ))
                    _matertialsUsed.Add( mat );
                
                LevelGrowthType growthType = LevelGrowthType.Normal;

                int id = index + 1;
                int wallId = id + _rooms.Length;

                LevelRoom room = new LevelRoom(id, wallId, graphPosition, roomOrigin, roomSize, roomSizeRatio, mat, wallThickness, weight, growthType);
                _rooms[index] = room;
                _roomInfo[index] = new RoomInfo(wallThickness, mat);
                _edgeDictionary[id] = new Dictionary<int, int>();
                
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
        dimensions = new int2((adjustedMaxSize*layoutDimensions.x) + (adjustedBuffer*2*layoutDimensions.x) , (adjustedMaxSize*layoutDimensions.y) + (adjustedBuffer*2*layoutDimensions.y) );
        _levelLayout = new NativeArray<int>(dimensions.x*dimensions.y, Allocator.Persistent);
        foreach ( LevelRoom room in _rooms )
        {
            DrawRoomSeed( room );
        }
        CheckInitialConnections();
    }

    private void SetNeighbors( int room1, int room2, int weight )
    {
        _edgeDictionary[room1][room2] = weight;
        _edgeDictionary[room2][room1] = weight;
        
        //update adjacency matrix
        int room1Index = room1 - 1;
        int room2Index = room2 - 1;

        int index1 = ( room1Index * _rooms.Length ) + room2Index;
        int index2 = ( room2Index * _rooms.Length ) + room1Index;

        _adjacencyMatrix[index1] = weight;
        _adjacencyMatrix[index2] = weight;

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

    /// <summary>
    /// Will fetch any connections between the initial room seeds. 
    /// This is to catch the edge case where two seeds are placed butting
    /// up against each other, which is a connection that will not be caught
    /// by the growth algorithm.
    /// </summary>
    private void CheckInitialConnections()
    {
        for ( int x = 0; x < layoutDimensions.x; x++ )
        {
            for ( int y = 0; y < layoutDimensions.y; y++ )
            {
                int index = x + y * layoutDimensions.x;
                LevelRoom room = _rooms[index];
                if ( x > 0 )
                {
                    int leftIndex = (x -1) + y * layoutDimensions.x;
                    LevelRoom leftRoom = _rooms[leftIndex];
                    if ( room.Bounds.Borders( leftRoom.Bounds ) )
                    {
                        //calculate the overlap between the two room's floors
                        int4 thicknessVector = new int4( room.WallThickness, room.WallThickness, -room.WallThickness, -room.WallThickness);
                        int4 leftThicknessVector = new int4( leftRoom.WallThickness, leftRoom.WallThickness, -leftRoom.WallThickness, -leftRoom.WallThickness);
                        int4 room1 = room.Bounds + thicknessVector;
                        int4 room2 = leftRoom.Bounds + leftThicknessVector;
                        
                        int swap = room1.x;
                        room1.x = room2.z;
                        room2.z = swap;
                        LevelConnectionInfo cnct = new LevelConnectionInfo(room.Id, leftRoom.Id, room1.Boolean( room2 ), new int2(-1, 0));
                        AddConnection( cnct, room );
                    }
                }

                if ( y > 0 )
                {
                    int bottomIndex = x + (y -1) * layoutDimensions.x;
                    LevelRoom bottomRoom = _rooms[bottomIndex];
                    if ( room.Bounds.Borders( bottomRoom.Bounds ) )
                    {
                        //calculate the overlap between the two room's floors
                        int4 thicknessVector = new int4( room.WallThickness, room.WallThickness, -room.WallThickness, -room.WallThickness );
                        int4 bottomThicknessVector = new int4( bottomRoom.WallThickness, bottomRoom.WallThickness, -bottomRoom.WallThickness, -bottomRoom.WallThickness );
                        int4 room1 = room.Bounds + thicknessVector;
                        int4 room2 = bottomRoom.Bounds + bottomThicknessVector;

                        int swap = room1.y;
                        room1.y = room2.w;
                        room2.w = swap;

                        LevelConnectionInfo cnct = new LevelConnectionInfo(room.Id, bottomRoom.Id, room1.Boolean( room2 ), new int2(0, -1));
                        AddConnection( cnct, room );
                    }
                }
            }
        }
    }
    
    //Places the initial seeds into the level array
    private void DrawRoomSeed( LevelRoom room )
    {
        int start = room.Origin.x + room.Origin.y * dimensions.x;
        
        for ( int x = 0; x < room.Size.x; x++ )
        {
            for ( int y = 0; y < room.Size.y; y++ )
            {
                int index = x + y * dimensions.x;

                if ( (y < room.WallThickness || x < room.WallThickness ) || (y >= room.Size.y - room.WallThickness || x >= room.Size.x - room.WallThickness))
                {
                    _levelLayout[start + index] = _rooms.Length + room.Id;
                }
                else
                {
                    _levelLayout[start + index] = room.Id;
                }
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

        //NativeStream stream = new NativeStream(1, Allocator.TempJob);
        NativeQueue<WallInfo> wallCells = new NativeQueue<WallInfo>(Allocator.TempJob);
        LevelFetchWallsJob fetchJob = new LevelFetchWallsJob
        {
            LevelLayout  = _levelLayout,
            LevelDimensions = dimensions,
            RoomInfo = _roomInfo,
            WallCells = wallCells.AsParallelWriter(),
            WallRadius = _maxWallThickness
        };
        
        JobHandle fetchHandle = fetchJob.Schedule( _levelLayout.Length, 256 );
        fetchHandle.Complete();

        int binSize = 64;
        int xBins = dimensions.x / binSize + math.sign( dimensions.x % binSize );
        int yBins = dimensions.y / binSize + math.sign( dimensions.y % binSize );
        int binCount = xBins * yBins;

        NativeArray<WallInfo> wallArr = wallCells.ToArray( Allocator.TempJob );
        
        foreach ( LevelMaterial targetMat in _matertialsUsed )
        {
            NativeArray<bool> binTracker = new NativeArray<bool>(binCount, Allocator.TempJob);
            NativeParallelMultiHashMap<int2, int2> binnedWalls = new NativeParallelMultiHashMap<int2, int2>(wallArr.Length, Allocator.TempJob);
            
            LevelBinWallsJob binWallsJob = new LevelBinWallsJob
            {
                WallCells = wallArr,
                BinSize = binSize,
                XBins = xBins,
                TargetMaterial = targetMat,
                BinTracker = binTracker,
                BinnedWalls = binnedWalls.AsParallelWriter()
            };

            JobHandle binHandle = binWallsJob.Schedule( binCount, 1 );
            binHandle.Complete();

            int usedBins = 0;
            foreach ( bool notEmpty in binTracker )
            {
                if ( notEmpty )
                    usedBins++;
            }

            NativeArray<int> pointFields = new NativeArray<int>(usedBins*(binSize*binSize), Allocator.TempJob);
            NativeArray<int> counts = new NativeArray<int>(usedBins, Allocator.TempJob);
            NativeArray<int2> positions = new NativeArray<int2>(usedBins, Allocator.TempJob);

            LevelCreateWallsJob createWallsJob = new LevelCreateWallsJob
            {
                BinnedWalls = binnedWalls,
                BinTracker = binTracker,
                BinSize = binSize,
                XBins = xBins,
                PointFields = pointFields,
                Counts = counts,
                Positions = positions
            };

            JobHandle createHandle = createWallsJob.Schedule( binCount, 1 );
            createHandle.Complete();


            for ( int i = 0; i < usedBins; i++ )
            {
                NativeArray<int> pointField = pointFields.GetSubArray( i * ( binSize * binSize ), ( binSize * binSize ) );
                StripMeshConstructor meshConstructor = new StripMeshConstructor();
                
                //Material mat = new Material( wallMaterial ) {mainTexture = _textureDict[targetMat]};
                Material mat = new Material( wallMaterial );
                mat.SetTexture( "_BaseMap", _textureDict[targetMat] );
                mat.SetVector( "_BlockPosition", new Vector4(positions[i].x, positions[i].y ));
                mat.SetInt( "_BlockSize", binSize );


                LevelWall newWall = new LevelWall
                {
                    Material = mat,
                    Mesh = meshConstructor.ConstructMesh( pointField, binSize, positions[i] ),
                    PointField = pointField.ToArray(),
                    Position = Vector2.zero
                };
                pointField.Dispose();
                _walls.Add( newWall );
            }
            
            binnedWalls.Dispose();
            binTracker.Dispose();
            pointFields.Dispose();
            counts.Dispose();
            positions.Dispose();
        }

        wallArr.Dispose();
        wallCells.Dispose();

        /*
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
        */
    }
    
    private void MakeFloors()
    {
        foreach ( LevelRoom room in _rooms )
        {
            StripMeshConstructor meshConstructor = new StripMeshConstructor();
            room.FloorMesh = meshConstructor.ConstructMesh( _levelLayout, dimensions, room, room.Id );
            LevelFloor newFloor = new LevelFloor
            {
                FloorMesh = room.FloorMesh, 
                FloorMaterial = floorMaterials[Random.Range( 0, floorMaterials.Length )],
                Position = new Vector2(0,0 )
            };

            _floors.Add( newFloor  );
        }
        /*
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
        */
    }

    
    
    private void OnDestroy()
    {
        CleanUp(true);
    }

    private void CleanUp(bool isExit)
    {
        if ( _levelLayout.IsCreated )
        {
            _levelLayout.Dispose();
            _roomInfo.Dispose();
            _adjacencyMatrix.Dispose();

            
            //only dispose of the entities if not exiting (exit == OnDestroy is called)
            //trying to call it during OnDestroy can cause errors since the world also gets disposed
            if ( !isExit )
            {
                ClearLevelEntities();
            }
            
            foreach ( var col in _collidersMade )
            {
                col.Dispose();
            }
            _collidersMade.Clear();
            //removes the materials that were made for the level
            Resources.UnloadUnusedAssets();
                
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
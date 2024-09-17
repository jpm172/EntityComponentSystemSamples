using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class LevelGenerator
{
    private int _cornerLimit = 6;
    private int _counter = 0;
    private int _totalSteps = 0;
    public int2 GrowthOverride;


    public bool breakPoint;

public bool StepWiseGrow;
public int GrowIterations;
public int StepsPerIteration;
public void GrowRooms()
{
    if ( StepWiseGrow )
    {
        //StepMainGrow();
    }
    else
    {

        MainGrow();
        
    }
    
}


private void MainGrow()
{
    _totalSteps = 0;
    bool hasPath = false;
    while ( !hasPath && _totalSteps < 1 )
    {
        LevelRoom room = _rooms[_counter];
        int connections = _edgeDictionary[room.Id].Count;
        GrowRoom( room );


        if ( connections != _edgeDictionary[room.Id].Count )
        {
            hasPath = HasPath( room.Id );
        }
        
        _counter = ( _counter + 1 ) % _rooms.Length;
        _totalSteps++;
    }
    
    //MakeRoomMeshes();
}


private void GrowRoom( LevelRoom room )
{
    
    if ( !room.CanGrow )
        return;
    
    if ( room.GrowthType == LevelGrowthType.Normal )
    {
        int2 growthDirection = GetRandomGrowthDirection( room );
        if ( !GrowthOverride.Equals( int2.zero ) )
            growthDirection = GrowthOverride;

        //for( int n = 0; n < 1; n++ )
        //for ( int n = 0; n < _minRoomSeedSize; n++ )
        for( int n = 0; n < StepsPerIteration; n++ )
        {
            int dirsBefore = room.XGrowthDirections.Count + room.YGrowthDirections.Count;
            
            NormalGrow( room, growthDirection );
            
            if ( room.XGrowthDirections.Count + room.YGrowthDirections.Count != dirsBefore )
                return;
        }
    }
    else if ( room.GrowthType == LevelGrowthType.Mold )
    {
        //TODO implement mold growth
    }
}


private void NormalGrow( LevelRoom room, int2 growthDirection )
{
    
    NativeParallelMultiHashMap<int, LevelCollision> collisionResults = 
        new NativeParallelMultiHashMap<int, LevelCollision>(_nextCellId*_nextCellId, Allocator.TempJob);

    NativeQueue<LevelConnectionInfo> connections = new NativeQueue<LevelConnectionInfo>(Allocator.TempJob);
    

    LevelGrowQueryJob growQueryJob = new LevelGrowQueryJob
    {
        BroadPhaseBounds = _broadPhaseBounds,
        Collisions = collisionResults.AsParallelWriter(),
        NewConnections = connections.AsParallelWriter(),
        NarrowPhaseBounds = _narrowPhaseBounds,
        GrowthDirection = growthDirection,
        RoomId = room.Id
    };

    JobHandle growQueryHandle = growQueryJob.Schedule( room.CellCount*_broadPhaseBounds.Count, 128 );
    growQueryHandle.Complete();


    AddConnections( connections,  room);
    
    connections.Dispose();
    
    NativeQueue<LevelCell> newCells = new NativeQueue<LevelCell>(Allocator.TempJob);
    NativeList<LevelCell> changedCells = new NativeList<LevelCell>(room.CellCount, Allocator.TempJob);
    LevelCheckCollisionsJob checkJob = new LevelCheckCollisionsJob
    {
        Collisions = collisionResults,
        GrowthDirection = growthDirection,
        NarrowPhaseBounds = _narrowPhaseBounds,
        NewCells = newCells.AsParallelWriter(),
        ChangedCells = changedCells.AsParallelWriter(),
        RoomId = room.Id
    };

    
    JobHandle checkHandle = checkJob.Schedule( room.CellCount, 16 );
    checkHandle.Complete();

    foreach ( LevelCell cell in changedCells )
    {
        _narrowPhaseBounds.TryGetFirstValue( room.Id, out LevelCell fc,
            out NativeParallelMultiHashMapIterator<int> it );
        UpdateBroadPhase( room, cell );

        if ( fc.Equals( cell ) )
        {
            _narrowPhaseBounds.SetValue( cell, it  );
            continue;
        }

        while ( _narrowPhaseBounds.TryGetNextValue( out LevelCell c, ref it ) )
        {
            if ( c.Equals( cell ) )
            {
                _narrowPhaseBounds.SetValue( cell, it  );
                break;
            }
        }
    }

    while ( newCells.TryDequeue( out LevelCell newCell ) )
    {
        AddCell( room, newCell );
    }
    
        
    newCells.Dispose();
    changedCells.Dispose();
    collisionResults.Dispose();

    /*
    //NativeQueue<int> newCells = new NativeQueue<int>( Allocator.TempJob);
    NativeQueue<LevelCell> newCells = new NativeQueue<LevelCell>( Allocator.TempJob);
    LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
    {
        GrowthDirection = growthDirection,
        Required = _minRoomSeedSize + (room.WallThickness*2),
        LevelDimensions = dimensions,
        LevelLayout = _levelLayout,
        RoomId = room.Id,
        RoomSize = room.Size,
        RoomOrigin = room.Origin,
        NewCells = newCells.AsParallelWriter()
    };
    
    
    JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 32);
    handle.Complete();

    bool removeDirection = false;
    //if we added cells, playback the results to paint them onto the level
    if ( newCells.Count > 0 )
    {
        NativeQueue<LevelConnection> newNeighbors = new NativeQueue<LevelConnection>(Allocator.TempJob);
        NativeQueue<int2> localMinima = new NativeQueue<int2>(Allocator.TempJob);
        NativeQueue<int2> localMaxima = new NativeQueue<int2>(Allocator.TempJob);
        //NativeParallelMultiHashMap<int2, int> neighborMap = new NativeParallelMultiHashMap<int2, int>(newCells.Length*3, Allocator.TempJob);

        //NativeArray<int> newCellsArray = newCells.ToArray( Allocator.TempJob );
        NativeArray<LevelCell> newCellsArray = newCells.ToArray( Allocator.TempJob );
        
        LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
        {
            LevelLayout = _levelLayout,
            LevelDimensions = dimensions,
            Neighbors = newNeighbors.AsParallelWriter(),
            LocalMinima = localMinima.AsParallelWriter(),
            LocalMaxima = localMaxima.AsParallelWriter(),
            NewCells = newCellsArray,
            RoomId = room.Id,
            RoomBounds = room.Bounds
        };
        
        JobHandle applyHandle = applyJob.Schedule(newCellsArray.Length, 32);
        applyHandle.Complete();

        if ( localMinima.Count + localMaxima.Count > 0 )
        {
            //if(room.Id == 3)
               // Debug.Log( $"{room.Bounds} : {room.Size}" );
            ResizeRoom( room, localMinima, localMaxima );
           // if(room.Id == 3)
                //Debug.Log( $"{room.Bounds} : {room.Size}" );
        }

        localMaxima.Dispose();
        localMinima.Dispose();
        newCellsArray.Dispose();

        
        if ( newNeighbors.Count > 0 )
        {
            HashSet<int> checkedNeighbors = new HashSet<int>();
            NativeArray<LevelConnection> neighborArray = newNeighbors.ToArray( Allocator.TempJob );
            //go through each neighbor, and if it can form a valid connection to the neighbor, mark it as a new edge in the level
            for ( int i = 0; i < neighborArray.Length; i++ )
            {
                int neighborIndex = neighborArray[i].RoomId-1;
                if(checkedNeighbors.Contains( neighborIndex ))
                    continue;

                checkedNeighbors.Add( neighborIndex );
                
                LevelEdge check = new LevelEdge
                {
                    Source = room.Index,
                    Destination = neighborIndex
                };
                if ( !_edgeDictionary[room.Index].Contains( check ) && IsValidConnection(room, _rooms[neighborIndex], neighborArray))
                {
                    SetNeighbors( room.Index, neighborIndex, room.Weight );
                }
            }

            neighborArray.Dispose();
        }
        
        newNeighbors.Dispose();
    }
    else
    {
        removeDirection = true;
    }

    if ( removeDirection )
    {
        //if it didn't grow in the current direction, remove it from the list of valid growth directions
        if ( math.abs( growthDirection.x ) > math.abs( growthDirection.y ) )
        {
            room.XGrowthDirections.Remove( growthDirection );
        }
        else
        {
            room.YGrowthDirections.Remove( growthDirection );
        }
    }

    newCells.Dispose();
    */

}

private void AddConnections( NativeQueue<LevelConnectionInfo> connections, LevelRoom room )
{
    while ( connections.TryDequeue( out LevelConnectionInfo cnct ) )
    {
        if ( !_roomConnections.ContainsKey( cnct.Connections ) )
        {
            _roomConnections[cnct.Connections] = new List<LevelConnection>();
        }
        
        LevelConnection newConnection = new LevelConnection( cnct.Bounds );

        _roomConnections[cnct.Connections].Add( newConnection );
        List<LevelConnection> roomConnections = _roomConnections[cnct.Connections];
        for ( int i = roomConnections.Count-2; i >= 0 ; i-- )
        {
            if ( newConnection.TryMerge( roomConnections[i] ) )
            {
                roomConnections.RemoveAt( i );

                if ( newConnection.GetLargestDimension() >= _minRoomSeedSize )
                {
                    //Debug.Log( "Valid Connection Made" );
                    SetNeighbors( cnct.Connections.x, cnct.Connections.y, room.Weight );
                }
               //roomConnections[i].Pieces.Clear();
            }
        }
    }

}



private int2 GetRandomGrowthDirection( LevelRoom room )
{
    
    bool hasHorizontal = room.XGrowthDirections.Count > 0;
    bool hasVertical = room.YGrowthDirections.Count > 0;

    int rand = 0;
    if ( hasHorizontal && hasVertical )
    {
        int sum = room.SizeRatio.x + room.SizeRatio.y;
        
        
        float xChance = (float)room.SizeRatio.x / sum;
        if ( Random.Range( 0f,1f ) < xChance )
        {
            rand = Random.Range( 0, room.XGrowthDirections.Count );
            return room.XGrowthDirections[rand];
        }
        else
        {
            rand = Random.Range( 0, room.YGrowthDirections.Count );
            return room.YGrowthDirections[rand];
        }
    }
    else if ( hasHorizontal )
    {
        rand = Random.Range( 0, room.XGrowthDirections.Count );
        return room.XGrowthDirections[rand];
    }

    rand = Random.Range( 0, room.YGrowthDirections.Count );
    return room.YGrowthDirections[rand];
    
}




/*

private void StepMainPathGrow()
{
    _totalSteps = 0;
    bool hasPath = false;
    while ( _totalSteps < GrowSteps )
    {
        LevelRoom room = _rooms[_counter];
        int connections = _edgeDictionary[_counter].Count;
        GrowRoomPath( room );


        if ( connections != _edgeDictionary[_counter].Count )
        {
            hasPath = HasPath( _counter );
        }

        _counter = ( _counter + 1 ) % _rooms.Length;
        _totalSteps++;
    }
    MakeRoomMeshes();
}



private void NormalGrowPath( LevelRoom room, int2 growthDirection )
{

    
    //NativeQueue<int> newCells = new NativeQueue<int>( Allocator.TempJob);
    NativeQueue<LevelCell> newCells = new NativeQueue<LevelCell>( Allocator.TempJob);
    LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
    {
        GrowthDirection = growthDirection,
        Required = _minRoomSeedSize + (room.WallThickness*2),
        LevelDimensions = dimensions,
        LevelLayout = _levelLayout,
        RoomId = room.Id,
        RoomSize = room.Size,
        RoomOrigin = room.Origin,
        NewCells = newCells.AsParallelWriter()
    };
    
    
    JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 32);
    handle.Complete();

    bool removeDirection = false;
    //if we added cells, playback the results to paint them onto the level
    if ( newCells.Count > 0 )
    {
        NativeQueue<LevelConnection> newNeighbors = new NativeQueue<LevelConnection>(Allocator.TempJob);
        NativeQueue<int2> localMinima = new NativeQueue<int2>(Allocator.TempJob);
        NativeQueue<int2> localMaxima = new NativeQueue<int2>(Allocator.TempJob);
        //NativeParallelMultiHashMap<int2, int> neighborMap = new NativeParallelMultiHashMap<int2, int>(newCells.Length*3, Allocator.TempJob);

        //NativeArray<int> newCellsArray = newCells.ToArray( Allocator.TempJob );
        NativeArray<LevelCell> newCellsArray = newCells.ToArray( Allocator.TempJob );
        
        LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
        {
            LevelLayout = _levelLayout,
            LevelDimensions = dimensions,
            Neighbors = newNeighbors.AsParallelWriter(),
            LocalMinima = localMinima.AsParallelWriter(),
            LocalMaxima = localMaxima.AsParallelWriter(),
            NewCells = newCellsArray,
            RoomId = room.Id,
            RoomBounds = room.Bounds
        };
        
        JobHandle applyHandle = applyJob.Schedule(newCellsArray.Length, 32);
        applyHandle.Complete();

        if ( localMinima.Count + localMaxima.Count > 0 )
        {
            //if(room.Id == 3)
               // Debug.Log( $"{room.Bounds} : {room.Size}" );
            ResizeRoom( room, localMinima, localMaxima );
           // if(room.Id == 3)
                //Debug.Log( $"{room.Bounds} : {room.Size}" );
        }

        localMaxima.Dispose();
        localMinima.Dispose();
        newCellsArray.Dispose();

        
        if ( newNeighbors.Count > 0 )
        {
            HashSet<int> checkedNeighbors = new HashSet<int>();
            NativeArray<LevelConnection> neighborArray = newNeighbors.ToArray( Allocator.TempJob );
            //go through each neighbor, and if it can form a valid connection to the neighbor, mark it as a new edge in the level
            for ( int i = 0; i < neighborArray.Length; i++ )
            {
                int neighborIndex = neighborArray[i].RoomId-1;
                if(checkedNeighbors.Contains( neighborIndex ))
                    continue;

                checkedNeighbors.Add( neighborIndex );
                
                LevelEdge check = new LevelEdge
                {
                    Source = room.Index,
                    Destination = neighborIndex
                };
                if ( !_edgeDictionary[room.Index].Contains( check ) && IsValidConnection(room, _rooms[neighborIndex], neighborArray))
                {
                    SetNeighbors( room.Index, neighborIndex, room.Weight );
                }
            }

            neighborArray.Dispose();
        }
        
        newNeighbors.Dispose();
    }
    else
    {
        removeDirection = true;
    }

    if ( removeDirection )
    {
        //if it didn't grow in the current direction, remove it from the list of valid growth directions
        if ( math.abs( growthDirection.x ) > math.abs( growthDirection.y ) )
        {
            room.XGrowthDirections.Remove( growthDirection );
        }
        else
        {
            room.YGrowthDirections.Remove( growthDirection );
        }
    }

    newCells.Dispose();
    
}

private bool IsValidConnection(LevelRoom room1, LevelRoom room2, NativeArray<LevelConnection> connections)
{
    
    NativeQueue<LevelConnection> validConnections = new NativeQueue<LevelConnection>(Allocator.TempJob); 
    LevelAnalyzeConnection analyzeJob  = new LevelAnalyzeConnection
    {
        LevelLayout = _levelLayout,
        LevelDimensions = dimensions,
        Connections = connections,
        ValidConnections =  validConnections.AsParallelWriter(),
        RoomId = room1.Id,
        NeighborId = room2.Id,
        Required = _minRoomSeedSize + 2*math.max( room1.WallThickness, room2.WallThickness )
    }; 
    
    JobHandle handle = analyzeJob.Schedule(connections.Length*4, 16);
    handle.Complete();

    bool result = validConnections.Count > 0;
    validConnections.Dispose();

    return result;
}


private void ResizeRoom( LevelRoom room,  NativeQueue<int2> localMinima,  NativeQueue<int2> localMaxima )
{
    int2 minima = room.Bounds.xy;
    int2 maxima = room.Bounds.zw;
    while ( localMinima.TryDequeue( out int2 value ) )
    {
        minima = math.min( value, minima );
    }

    while ( localMaxima.TryDequeue( out int2 value ) )
    {
        maxima = math.max( value, maxima );
    }
    
    room.Bounds = new int4(minima,maxima);
}



private bool IsFinishedGrowing()
{
    foreach ( LevelRoom room in _rooms )
    {
        if ( room.CanGrow )
            return false;
    }

    return true;
}

*/
}

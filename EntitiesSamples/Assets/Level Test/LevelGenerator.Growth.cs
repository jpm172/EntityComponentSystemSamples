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

    public bool StepWiseGrow;
    public int GrowSteps;
    public void GrowRooms()
    {
        if ( StepWiseGrow )
        {
            StepMainPathGrow();
        }
        else
        {
            MainPathGrow();
        }
        
    }

    public void RandomGrowRooms()
    {
        RandomGrow();
    }

    //float startTime = Time.realtimeSinceStartup; Debug.Log( "Random Grow done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );
    private void RandomGrow()
    {
        int roomsToGrow = _totalSteps;
        Debug.Log( roomsToGrow );
        for ( int i = 0; i < roomsToGrow; i++ )
        {
            int index = Random.Range( 0, _rooms.Length );
            LevelRoom room = _rooms[index];

            int growthsPerRoom  = Random.Range( 20, 50 );
            
            for ( int x = 0; x < growthsPerRoom; x++ )
            {
                GrowRoomRandom( room );
            }
        }
    }
    
    
    private void GrowRoomRandom( LevelRoom room )
    {
        if ( !room.CanGrow )
            return;
        
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            NormalGrowRandom( room );
        }
        else if ( room.GrowthType == LevelGrowthType.Mold )
        {
            //TODO implement mold growth
        }
    }
    
    private void NormalGrowRandom( LevelRoom room )
    {
        Debug.Log( "need to setup resizing for random" );
        return;
        int2 growthDirection = GetRandomGrowthDirection( room );
        
        NativeQueue<int> newCells = new NativeQueue<int>( Allocator.TempJob);
        
        LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
        {
            GrowthDirection = growthDirection,
            Required = _minRoomSeedSize + room.WallThickness*2,
            LevelDimensions = dimensions,
            LevelLayout = _levelLayout,
            RoomId = room.Id,
            RoomSize = room.Size,
            RoomOrigin = room.Origin,
            NewCells = newCells.AsParallelWriter()
        };
        
        
        JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 64);
        handle.Complete();

        bool removeDirection = false;
        if ( newCells.Count > 0 )
        {
            //ResizeRoom( room, growthDirection );

            NativeQueue<LevelConnection> newNeighbors = new NativeQueue<LevelConnection>(Allocator.TempJob);
            NativeArray<int> newCellsArray = newCells.ToArray( Allocator.TempJob );
            
            LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                Neighbors = newNeighbors.AsParallelWriter(),
                NewCells = newCellsArray,
                RoomId = room.Id
            };
            
            JobHandle applyHandle = applyJob.Schedule(newCellsArray.Length, 32);
            applyHandle.Complete();
            newNeighbors.Dispose();
            newCellsArray.Dispose();
            
            NativeQueue<int> corners = new NativeQueue<int>(Allocator.TempJob);
            
            LevelAnalyzeNormalRoom analyzeJob = new LevelAnalyzeNormalRoom
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                RoomId = room.Id,
                RoomSize = room.Size,
                RoomOrigin = room.Origin,
                Corners = corners.AsParallelWriter()
            };
            
            JobHandle analyzeHandle = analyzeJob.Schedule(room.Size.x * room.Size.y, 64);
            analyzeHandle.Complete();

            if ( corners.Count > _cornerLimit )
            {
                room.XGrowthDirections.Clear();
                room.YGrowthDirections.Clear();
            }

            corners.Dispose();

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
    
    private void MainPathGrow()
    {
        _totalSteps = 0;
        bool hasPath = false;
        while ( !hasPath  )
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
    
    

    private void GrowRoomPath( LevelRoom room )
    {

        if ( !room.CanGrow )
            return;
        
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            int2 growthDirection = GetRandomGrowthDirection( room );
            for ( int n = 0; n < _minRoomSeedSize; n++ )
            {
                int dirsBefore = room.XGrowthDirections.Count + room.YGrowthDirections.Count;
                
                NormalGrowPath( room, growthDirection );
                
                if ( room.XGrowthDirections.Count + room.YGrowthDirections.Count != dirsBefore )
                    return;
            }
        }
        else if ( room.GrowthType == LevelGrowthType.Mold )
        {
            //TODO implement mold growth
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

    private void NormalGrowPath( LevelRoom room, int2 growthDirection )
    {
        
        NativeQueue<int> newCells = new NativeQueue<int>( Allocator.TempJob);
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

            NativeArray<int> newCellsArray = newCells.ToArray( Allocator.TempJob );
            
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
                ResizeRoom( room, localMinima, localMaxima );
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
    
    
}

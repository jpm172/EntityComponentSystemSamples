using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class LevelGenerator
{
    public int steps;
    private int _cornerLimit = 6;
    private int _counter = 0;
    private int _totalSteps = 0;
    public void GrowRooms()
    {
        //TODO: PATHLESS GROWING: Grow the rooms randomly and everytime a new connection is made, run dijkstras on the connection matrix to see if we're done
        MainPathGrow();
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
        int2 growthDirection = GetRandomGrowthDirection( room );

        int listSize = math.max( room.Size.x, room.Size.y );
        NativeList<int> newCells = new NativeList<int>(listSize, Allocator.TempJob);
        
        LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
        {
            GrowthDirection = growthDirection,
            required = _minRoomSeedSize,
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
        if ( newCells.Length > 0 )
        {
            ResizeRoom( room, growthDirection );

            NativeQueue<int> newNeighbors = new NativeQueue<int>(Allocator.TempJob);
            
            LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                Neighbors = newNeighbors.AsParallelWriter(),
                NewCells = newCells,
                RoomId = room.Id
            };
            
            JobHandle applyHandle = applyJob.Schedule(newCells.Length, 32);
            applyHandle.Complete();
            newNeighbors.Dispose();
            
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
    
    
    private void MainPathGrow()
    {
        _totalSteps = 0;
        bool hasPath = false;
        while ( !hasPath )
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
        
        
    }
    
    

    private void GrowRoomPath( LevelRoom room )
    {

        if ( !room.CanGrow )
            return;
        
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            NormalGrowPath( room );
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

    private void NormalGrowPath( LevelRoom room )
    {
        int2 growthDirection = GetRandomGrowthDirection( room );

        int listSize = math.max( room.Size.x, room.Size.y );
        NativeList<int> newCells = new NativeList<int>(listSize, Allocator.TempJob);
        
        LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
        {
            GrowthDirection = growthDirection,
            required = _minRoomSeedSize,
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
        if ( newCells.Length > 0 )
        {
            ResizeRoom( room, growthDirection );

            NativeQueue<int> newNeighbors = new NativeQueue<int>(Allocator.TempJob);

            LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                Neighbors = newNeighbors.AsParallelWriter(),
                NewCells = newCells,
                RoomId = room.Id
            };
            
            JobHandle applyHandle = applyJob.Schedule(newCells.Length, 32);
            applyHandle.Complete();

            if ( newNeighbors.Count > 0 )
            {
                //go through each neighbor, and if it belongs to a room that is part of the main path, that mark this growth direction as finished
                while ( !newNeighbors.IsEmpty() )
                {
                    int neighborIndex = newNeighbors.Dequeue()-1;
                    

                    LevelEdge check = new LevelEdge
                    {
                        Source = room.Index,
                        Destination = neighborIndex
                    };
                    if ( !_edgeDictionary[room.Index].Contains( check ) )
                    {
                        SetNeighbors( room.Index, neighborIndex, Random.Range( minEdgeWeight, maxEdgeWeight+1 ) );
                    }
                    
                        
                }
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


    private void ResizeRoom( LevelRoom room, int2 growthDirection )
    {
        if ( growthDirection.x < 0 || growthDirection.y < 0 )
        {
            room.Origin += growthDirection;
        }
        
        room.Size += math.abs( growthDirection );
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

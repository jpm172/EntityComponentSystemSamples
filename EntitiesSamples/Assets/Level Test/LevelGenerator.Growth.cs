using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class LevelGenerator
{
    public int GrowIterations = 1; 
    private int counter = 0;
    public void GrowRooms()
    {
        MainPathGrow();
    }

    
    private void MainPathGrow()
    {
        //int counter = 0;
        
        //while ( counter < 5 ) 
        //{
        for ( int i = 0; i < GrowIterations; i++ )
        {
            LevelRoom room = _rooms[counter];

            GrowRoomAlongPath( room );
            counter = ( counter + 1 ) % _rooms.Length;
        }

        //}
    }

    private void GrowRoomAlongPath( LevelRoom room )
    {

        if ( !room.CanGrow )
            return;
        
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            NormalGrowRoom( room );
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

    private void NormalGrowRoom( LevelRoom room )
    {
        //int2 growthDirection = new int2(-1, 0);
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
        //JobHandle handle = growRoomJob.Schedule(4, 32);
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
                    foreach ( LevelEdge edge in _edgeDictionary[room.Index] )
                    {
                        if ( edge.Destination == neighborIndex )
                            removeDirection = true;
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
    
    private void RandomGrow()
    {
        
    }

    private bool IsFinishedMainPath()
    {
        foreach ( LevelRoom room in _rooms )
        {
            if ( room.CanGrow )
                return false;
        }

        return true;
    }
    
    
}

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
        if ( room.XGrowthDirections.Count > 0 )
        {
            int randX = Random.Range( 0, room.XGrowthDirections.Count );
            return room.XGrowthDirections[randX];
        }
        
        int randY = Random.Range( 0, room.YGrowthDirections.Count );
        return room.YGrowthDirections[randY];
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
            LevelDimensions = dimensions,
            LevelLayout = _levelLayout,
            RoomId = room.Id,
            WallThickness = room.WallThickness,
            RoomSize = room.Size,
            RoomOrigin = room.Origin,
            NewCells = newCells.AsParallelWriter()
        };
        
        
        JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 32);
        //JobHandle handle = growRoomJob.Schedule(4, 32);
        handle.Complete();

        if ( newCells.Length > 0 )
        {
            ResizeRoom( room, growthDirection );
            LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
            {
                LevelLayout = _levelLayout,
                NewCells = newCells,
                RoomId = room.Id
            };
            
            JobHandle applyHandle = applyJob.Schedule(newCells.Length, 32);
            applyHandle.Complete();
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

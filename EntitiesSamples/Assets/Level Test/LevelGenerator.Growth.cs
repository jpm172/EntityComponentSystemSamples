using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public partial class LevelGenerator
{
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
            LevelRoom room = _rooms[counter];
            GrowRoomAlongPath(room);
            counter = ( counter + 1 ) %_rooms.Length;
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

    private void NormalGrowRoom( LevelRoom room )
    {
        Vector2Int growthDirection = Vector2Int.left;
        
        LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
        {
            GrowthDirection = growthDirection,
            LevelDimensions = dimensions,
            LevelLayout = _levelLayout,
            RoomId = room.Id,
            RoomSize = room.Size,
            RoomOrigin = room.Origin
        };
        
        
        JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 32);

// Wait for the job to complete
        handle.Complete();
        

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

    private bool IsInBounds( int x, int y )
    {
        if ( x >= 0 && x < dimensions.x )
            return true;
        
        if ( y >= 0 && y < dimensions.y )
            return true;
        
        return false;
    }
    
}

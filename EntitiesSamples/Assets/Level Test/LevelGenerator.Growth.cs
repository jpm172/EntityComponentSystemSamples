using System.Collections;
using System.Collections.Generic;
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
            GrowRoomAlongPath(counter);
            counter = ( counter + 1 ) %_rooms.Length;
        //}
    }

    private void GrowRoomAlongPath( int roomIndex )
    {
        LevelRoom room = _rooms[roomIndex];
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            NormalGrowRoom( roomIndex );
        }
        else if ( room.GrowthType == LevelGrowthType.Mold )
        {
            //TODO implement mold growth
        }
            

    }

    private void NormalGrowRoom( int roomIndex )
    {
        if ( !_rooms[roomIndex].CanGrow )
            return;
        
        
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

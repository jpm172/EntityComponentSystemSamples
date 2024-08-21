using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct MakeMeshStripsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> LevelLayout;
    [ReadOnly] public Vector2Int LevelDimensions;
        
    [ReadOnly] public int RoomId;
    [ReadOnly] public int2 RoomOrigin;
    [ReadOnly] public int2 RoomSize;
    
    public NativeQueue<MeshStrip>.ParallelWriter Strips;
    public void Execute( int index )
    {
        int levelIndex = ((RoomOrigin.x + index) + ( RoomOrigin.y * LevelDimensions.x ));


        bool hasStrip = false;
        int2 stripStart = new int2(0,0);
        for ( int y = 0; y < RoomSize.y; y++ )
        {
            if ( LevelLayout[levelIndex] == RoomId && !hasStrip )
            {
                stripStart = new int2(index, y);
                hasStrip = true;
            }

            if ( LevelLayout[levelIndex] != RoomId && hasStrip )
            {
                Strips.Enqueue( new MeshStrip
                {
                    Start = stripStart,
                    End = new int2(stripStart.x, y-1)
                } );
                hasStrip = false;
            }
            
            levelIndex += LevelDimensions.x;
        }

        if ( hasStrip )
        {
            Strips.Enqueue( new MeshStrip
            {
                Start = stripStart,
                End = new int2(stripStart.x, RoomSize.y)
            } );
        }
        
    }
}

public struct MeshStrip
{
    public int2 Start;
    public int2 End;
}

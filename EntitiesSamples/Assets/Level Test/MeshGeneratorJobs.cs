using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[BurstCompile]
public struct MakeMeshStripsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> LevelLayout;
    [ReadOnly] public int2 LevelDimensions;
        
    [ReadOnly] public int TargetId;
    [ReadOnly] public int2 RoomOrigin;
    [ReadOnly] public int2 RoomSize;
    
    public NativeParallelMultiHashMap<int, MeshStrip>.ParallelWriter Strips;
    public void Execute( int index )
    {
        int levelIndex = ((RoomOrigin.x + index) + ( RoomOrigin.y * LevelDimensions.x ));

        if ( !IsInBounds( RoomOrigin.x + index, RoomOrigin.y ) )
        {
            Debug.Log( "OOB!" );
            return;
        }

        //makes vertical strips
        bool hasStrip = false;
        int2 stripStart = new int2(0,0);
        for ( int y = 0; y < RoomSize.y; y++ )
        {
            if ( IsPartOfRoom( levelIndex ) && !hasStrip )
            {
                stripStart = new int2(index, y);
                hasStrip = true;
            }

            if ( !IsPartOfRoom( levelIndex ) && hasStrip )
            {
                MeshStrip newStrip = new MeshStrip
                {
                    Start = stripStart,
                    End = new int2( stripStart.x, y - 1 )
                };
                Strips.Add( index, newStrip );
                hasStrip = false;
            }
            
            levelIndex += LevelDimensions.x;
        }

        if ( hasStrip )
        {
            MeshStrip newStrip = new MeshStrip
            {
                Start = stripStart,
                End = new int2( stripStart.x, RoomSize.y-1 )
            };
            //Strips.Enqueue(  );
            Strips.Add( index, newStrip );
        }
        
    }

    private bool IsPartOfRoom( int index )
    {
        return LevelLayout[index] == TargetId;
    }
    
    private bool IsInBounds( int x, int y )
    {
        if ( x < 0 || x >= LevelDimensions.x )
            return false;
            
            
        if ( y < 0 || y >= LevelDimensions.y )
            return false;
        
        return true;
    }
    
}

[BurstCompile]
public struct MergeMeshStripsJob : IJobParallelFor
{
    [ReadOnly] public NativeParallelMultiHashMap<int, MeshStrip> Strips;
    
    public NativeParallelMultiHashMap<int, MeshStrip>.ParallelWriter MergedStrips;
    public void Execute( int index )
    {
        if(!Strips.ContainsKey( index ))
            return;

        NativeParallelMultiHashMap<int, MeshStrip>.Enumerator values = Strips.GetValuesForKey( index );
        while ( values.MoveNext() )
        {
            TryMergeStrip( values.Current, index );
        }
    }

    private void TryMergeStrip(  MeshStrip strip, int index )
    {
        //if we can merge with the strip behind this one, then return and dont do anything with this strip
        if ( Strips.ContainsKey( index - 1 ) )
        {
            if ( TryMerge( strip, Strips.GetValuesForKey( index - 1 ) ) )
            {
                return;
            }
        }
        
        int checkIndex = index + 1;
        while ( Strips.ContainsKey( checkIndex ) )
        {
            if ( TryMerge( strip, Strips.GetValuesForKey( checkIndex) ) )
            {
                strip.End.x++;
                checkIndex++;
            }
            else
            {
                MergedStrips.Add( index, strip );
                return;
            }
        }
        MergedStrips.Add( index, strip );
    }
    
    
    private bool TryMerge( MeshStrip strip, NativeParallelMultiHashMap<int, MeshStrip>.Enumerator neighborValues )
    {

        while ( neighborValues.MoveNext() )
        {
            if ( CanMerge( strip, neighborValues.Current ) )
            {
                return true;
            }
        }
        
        return false;
    }

    private bool CanMerge( MeshStrip strip1, MeshStrip strip2 )
    {
        return ( strip1.Start.y == strip2.Start.y ) && ( strip1.End.y == strip2.End.y );
    }
}


[BurstCompile]
public struct MakeMeshStripsPointFieldJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> PointField;

    [ReadOnly] public int BinSize;

    public NativeParallelMultiHashMap<int, MeshStrip>.ParallelWriter Strips;
    public void Execute( int index )
    {
        int pointIndex = index;

        //makes vertical strips
        bool hasStrip = false;
        int2 stripStart = new int2(0,0);
        for ( int y = 0; y < BinSize; y++ )
        {
            if ( PointField[pointIndex] > 0 && !hasStrip )
            {
                stripStart = new int2(index, y);
                hasStrip = true;
            }

            if ( PointField[pointIndex] <= 0 && hasStrip )
            {
                MeshStrip newStrip = new MeshStrip
                {
                    Start = stripStart,
                    End = new int2( stripStart.x, y - 1 )
                };
                Strips.Add( index, newStrip );
                hasStrip = false;
            }
            
            pointIndex += BinSize;
        }

        if ( hasStrip )
        {
            MeshStrip newStrip = new MeshStrip
            {
                Start = stripStart,
                End = new int2( stripStart.x, BinSize-1 )
            };
            Strips.Add( index, newStrip );
        }
        
    }
    
    
}

public struct MeshStrip
{
    public int2 Start;
    public int2 End;
}

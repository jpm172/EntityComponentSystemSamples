using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct LevelGrowQueryJob : IJobParallelFor
{
    
    [ReadOnly] public NativeParallelMultiHashMap<int, LevelCell> NarrowPhaseBounds;
    [ReadOnly] public NativeHashMap<int, IntBounds> BroadPhaseBounds;
    [ReadOnly] public int RoomId;
    [ReadOnly] public int2 GrowthDirection;
    
    public NativeParallelMultiHashMap<int, LevelCollision>.ParallelWriter Collisions;
    public NativeQueue<LevelConncection>.ParallelWriter NewConnections;
    public void Execute( int index )
    {
        int cellIndex = index / BroadPhaseBounds.Count;
        int broadPhaseIndex = (index % BroadPhaseBounds.Count)+1;
        
        //Debug.Log( $"{index} == C{cellIndex}, B{broadPhaseIndex}" );
        
        LevelCell cell = GetCell( cellIndex );
        LevelCell potentialCell = GetPotentialGrowth( cell );

        if ( BroadPhaseBounds[broadPhaseIndex].Overlaps( potentialCell.Bounds ) )
        {
            NarrowPhaseCheck(broadPhaseIndex, potentialCell);
        }

    }
    

    private void NarrowPhaseCheck( int collisionRoomId, LevelCell cell )
    {
        NativeParallelMultiHashMap<int, LevelCell>.Enumerator otherCells = NarrowPhaseBounds.GetValuesForKey( collisionRoomId );

        while ( otherCells.MoveNext() )
        {
            if ( otherCells.Current.Bounds.Overlaps( cell.Bounds ) )
            {
                LevelCollision newCol = new LevelCollision
                {
                    Cell = cell,
                    CollidedWith = otherCells.Current,
                    CollisionRoomId = collisionRoomId
                };
                Collisions.Add( cell.CellId, newCol );

                if ( collisionRoomId != RoomId )
                {
                    LevelConncection newConnect = new LevelConncection
                    {
                        Bounds = cell.Bounds.Boolean( otherCells.Current.Bounds )
                    };
                    NewConnections.Enqueue( newConnect );
                }
            }
        }
    }
    

    private LevelCell GetCell( int index )
    {
        NativeParallelMultiHashMap<int, LevelCell>.Enumerator cells = NarrowPhaseBounds.GetValuesForKey( RoomId );
        
        int counter = 0;
        while ( cells.MoveNext() )
        {
            if ( counter == index )
            {
                return cells.Current;
            }
                
            counter++;
        }
        
        return new LevelCell();
    }

    private LevelCell GetPotentialGrowth( LevelCell cell )
    {

        if ( math.abs( GrowthDirection.x ) > math.abs( GrowthDirection.y ) )
        {
            if ( GrowthDirection.x < 0 )
            {
                cell.Bounds.Bounds.x = cell.Bounds.Bounds.z = cell.Bounds.Bounds.x + GrowthDirection.x;
            }
            else
            {
                cell.Bounds.Bounds.x = cell.Bounds.Bounds.z = cell.Bounds.Bounds.z + GrowthDirection.x;
            }
            
        }
        else
        {
            if ( GrowthDirection.y < 0 )
            {
                cell.Bounds.Bounds.y = cell.Bounds.Bounds.w = cell.Bounds.Bounds.y + GrowthDirection.y;
            }
            else
            {
                cell.Bounds.Bounds.y = cell.Bounds.Bounds.w = cell.Bounds.Bounds.w + GrowthDirection.y;
            }
            
        }
        
        return cell;
    }
        
}
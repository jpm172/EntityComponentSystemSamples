using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct BroadPhaseQueryJob : IJobParallelFor
{
    [ReadOnly] public NativeParallelMultiHashMap<int, LevelCell> NarrowPhaseBounds;
    [ReadOnly] public NativeHashMap<int, IntBounds> BroadPhaseBounds;
    [ReadOnly] public int RoomId;

    public NativeQueue<LevelBroadCollision>.ParallelWriter Collisions;
    
    public void Execute( int index )
    {
        int otherId = index + 1;
        IntBounds otherBounds = BroadPhaseBounds[otherId];
        CheckCollisions( otherId, otherBounds );
    }
    
    
    private void CheckCollisions( int otherId, IntBounds otherBounds )
    {
        NativeParallelMultiHashMap<int, LevelCell>.Enumerator cells = NarrowPhaseBounds.GetValuesForKey( RoomId );
        
        while ( cells.MoveNext() )
        {
            if ( cells.Current.Bounds.Overlaps( otherBounds ) )
            {
                LevelBroadCollision newCol = new LevelBroadCollision
                {
                    CollisionRoomId = otherId,
                    OriginCell = cells.Current
                };
                Collisions.Enqueue( newCol );
            }
        }
    }
    
}
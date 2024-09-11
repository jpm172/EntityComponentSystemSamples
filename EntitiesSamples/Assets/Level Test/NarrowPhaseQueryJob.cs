using Unity.Collections;
using Unity.Jobs;

public struct NarrowPhaseQueryJob : IJobParallelFor
{
    [ReadOnly] public NativeParallelMultiHashMap<int, LevelCell> NarrowPhaseBounds;
    [ReadOnly] public NativeArray<LevelBroadCollision> Collisions;
    [ReadOnly] public IntBounds CellBounds;
    [ReadOnly] public int RoomId;

    public NativeQueue<LevelNarrowCollision>.ParallelWriter NarrowCollisions;
    public void Execute( int index )
    {
        LevelBroadCollision collision = Collisions[index];

        CheckCollisions( collision  );

    }

    private void CheckCollisions( LevelBroadCollision collision )
    {
        NativeParallelMultiHashMap<int, LevelCell>.Enumerator cells = NarrowPhaseBounds.GetValuesForKey( collision.CollisionRoomId );
        
        while ( cells.MoveNext() )
        {
            if ( CellBounds.Overlaps( cells.Current.Bounds ) )
            {
                //NarrowCollisions.Enqueue( newCol );
            }
        }
    }
}
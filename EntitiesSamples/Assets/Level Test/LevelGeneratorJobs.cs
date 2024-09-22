using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


//[BurstCompile]
public struct LevelCheckCollisionsJob : IJobParallelFor
{

    [ReadOnly] public NativeParallelMultiHashMap<int, LevelWall> WallNarrowPhase;
    [ReadOnly] public NativeHashMap<int, LevelCell> FloorNarrowPhase;
    [ReadOnly] public NativeParallelMultiHashMap<int, LevelCollision> Collisions;
    [ReadOnly] public int RoomId;
    [ReadOnly] public int RequiredLength;
    [ReadOnly] public int4 GrowthDirection;

    public NativeQueue<LevelWall>.ParallelWriter NewWalls;
    public NativeList<LevelWall>.ParallelWriter ChangedWalls;
    
    public void Execute( int index )
    {
        LevelWall wall = GetWall( index );
        

        if ( !Collisions.ContainsKey( wall.WallId ) )
        {
            wall.Bounds += GrowthDirection;
            ChangedWalls.AddNoResize( wall );
            return;
        }

        LevelCell floor = FloorNarrowPhase[wall.WallId];
        LevelWall potentialGrowth = GetPotentialGrowth( wall, floor );
        
        ApplyCollision( Collisions.GetValuesForKey( wall.WallId ), potentialGrowth, wall );

    }

    private void ApplyCollision(NativeParallelMultiHashMap<int, LevelCollision>.Enumerator colEnum, LevelWall potentialGrowth, LevelWall wall)
    {
        int4 result = potentialGrowth.Bounds;
        //Debug.Log( result );
        while ( colEnum.MoveNext() )
        {
            result = result.CutOut( colEnum.Current.CollidedWith.Bounds, out int cuts, out int4 cut2 );
            if ( cuts == -1 )
            {
                //a cut value of -1 means the entire cell was cut away
                return;
            }
            if ( cuts == 2 )
            {
                ApplyCollision( colEnum, new LevelWall(-1, cut2, wall.Thickness), wall  );
            }

            if ( GetLength( result ) < RequiredLength )
            {
                return;
            }
        }

        
        result -= ( GrowthDirection.zwxy * (wall.Thickness*2));
        NewWalls.Enqueue( new LevelWall(-1, result, wall.Thickness) );
        
    }

    private int GetLength( int4 wallBounds )
    {
        int2 size = wallBounds.Size();
        if ( GrowthDirection.x != 0 || GrowthDirection.z != 0 )
        {
            return size.y;
        }
        else
        {
            return size.x;
        }
    }
    
    private LevelWall GetWall( int index )
    {
        NativeParallelMultiHashMap<int, LevelWall>.Enumerator cells = WallNarrowPhase.GetValuesForKey( RoomId );
        
        int counter = 0;
        while ( cells.MoveNext() )
        {
            if ( counter == index )
            {
                return cells.Current;
            }
                
            counter++;
        }
        
        return new LevelWall();
    }

    private LevelWall GetPotentialGrowth( LevelWall wall, LevelCell floor )
    {
        int4 newGrowth = wall.Bounds + GrowthDirection;
        //int4 extendedFloor = floor.Bounds + GrowthDirection * ( wall.Thickness + 1 );

        wall.Bounds = newGrowth.Flatten( GrowthDirection );
        //wall.Bounds = wall.Bounds.Boolean( extendedFloor );

        return wall;
    }

}

[BurstCompile]
public struct LevelGrowQueryJob : IJobParallelFor
{
    
    [ReadOnly] public NativeParallelMultiHashMap<int, LevelWall> WallNarrowPhase;
    [ReadOnly] public NativeHashMap<int, LevelCell> FloorNarrowPhase;
    [ReadOnly] public NativeHashMap<int, int4> BroadPhaseBounds;
    [ReadOnly] public int RoomId;
    [ReadOnly] public int4 GrowthDirection;
    
    public NativeParallelMultiHashMap<int, LevelCollision>.ParallelWriter Collisions;
    public NativeQueue<LevelConnectionInfo>.ParallelWriter NewConnections;
    public void Execute( int index )
    {
        int cellIndex = index / BroadPhaseBounds.Count;
        int broadPhaseIndex = (index % BroadPhaseBounds.Count)+1;

        LevelWall wallCell = GetWallCell( cellIndex );
        LevelWall potentialWall = GetPotentialGrowth( wallCell );

        if ( BroadPhaseBounds[broadPhaseIndex].Overlaps( potentialWall.Bounds ) )
        {
            NarrowPhaseCheck(broadPhaseIndex, potentialWall);
        }

    }
    

    private void NarrowPhaseCheck( int collisionRoomId, LevelWall wall )
    {
        NativeParallelMultiHashMap<int, LevelWall>.Enumerator otherWalls = WallNarrowPhase.GetValuesForKey( collisionRoomId );

        while ( otherWalls.MoveNext() )
        {
            if ( otherWalls.Current.Bounds.Overlaps( wall.Bounds ) )
            {
                LevelCollision newCol = new LevelCollision
                {
                    Wall = wall,
                    CollidedWith = otherWalls.Current,
                    CollisionRoomId = collisionRoomId
                };
                Collisions.Add( wall.WallId, newCol );

                
                if ( collisionRoomId != RoomId && WallAlignedWithFloor(wall, otherWalls.Current)  )
                {
                    int4 connectionBounds = GetConnectionBounds( wall, otherWalls.Current);
                    LevelConnectionInfo newInfo = new LevelConnectionInfo(RoomId, collisionRoomId, connectionBounds, GrowthDirection);

                    NewConnections.Enqueue( newInfo );
                }
            }
        }
    }


    //returns true if the floors assigned to each wall can connect with each other
    //Essentially, if the walkable space on each floor overlap at all on an axis, return true
    private bool WallAlignedWithFloor(LevelWall wall, LevelWall otherWall)
    {
        int combinedThickness = wall.Thickness + otherWall.Thickness + 1;
        LevelCell otherFloor = FloorNarrowPhase[otherWall.WallId];
        LevelCell floor = FloorNarrowPhase[wall.WallId];

        
        
        int4 connect = floor.Bounds + GrowthDirection * combinedThickness;
        //Debug.Log( $"{floor} -> {connect} ? {connect.Overlaps( otherFloor.Bounds )}" );
        
        return connect.Overlaps( otherFloor.Bounds );
    }
    
    
    
    //creates the bounds that represents the connection between the two rooms
    //the bounds will overlap both rooms 
    private int4 GetConnectionBounds(LevelWall wall, LevelWall otherWall)
    {
        int4 inverseGrowth = InvertGrowthDirection();
        int combinedThickness = wall.Thickness + otherWall.Thickness + 1;
        LevelCell otherFloor = FloorNarrowPhase[otherWall.WallId];
        LevelCell floor = FloorNarrowPhase[wall.WallId];

        int4 result = floor.Bounds.Flatten( GrowthDirection );
        result += GrowthDirection * (combinedThickness);

        int4 result2 = otherFloor.Bounds.Flatten( inverseGrowth );
        result2 += inverseGrowth * combinedThickness;

        return result.Boolean( result2 );
    }

    private int4 InvertGrowthDirection()
    {
        return -math.sign( GrowthDirection.zwxy );
    }
    
    private LevelWall GetWallCell( int index )
    {
        NativeParallelMultiHashMap<int, LevelWall>.Enumerator cells = WallNarrowPhase.GetValuesForKey( RoomId );
        
        int counter = 0;
        while ( cells.MoveNext() )
        {
            if ( counter == index )
            {
                return cells.Current;
            }
                
            counter++;
        }
        
        return new LevelWall();
    }

    
    
    private LevelWall GetPotentialGrowth( LevelWall wall )
    {
        int4 newGrowth = wall.Bounds + GrowthDirection;
        wall.Bounds = newGrowth.Flatten( GrowthDirection );

        return wall;
    }
        
}





public struct LevelCollision
{
    public LevelWall Wall;
    public LevelWall CollidedWith;
    public int CollisionRoomId;
}


/*
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

public struct LevelNarrowCollision
{
    public int CollisionRoomId;
    public LevelCell OriginCell;
    public LevelCell CollisionCell;
}

public struct LevelBroadCollision
{
    public int CollisionRoomId;
    public LevelCell OriginCell;
}
*/

/*
    [BurstCompile]
    public struct LevelGrowRoomJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public Vector2Int LevelDimensions;
        
        [ReadOnly] public int RoomId;
        [ReadOnly] public int2 RoomOrigin;
        [ReadOnly] public int2 RoomSize;
        [ReadOnly] public int2 GrowthDirection;
        [ReadOnly] public int Required;

        public NativeQueue<LevelCell>.ParallelWriter NewCells;
        public void Execute(int index)
        {
            int boundsX = index % RoomSize.x;
            int boundsY = index / RoomSize.x;
            
            //int levelIndex = (RoomOrigin.x + ( RoomOrigin.y * LevelDimensions.x )) + (boundsX + (boundsY*LevelDimensions.x));
            int levelIndex = RoomOrigin.x + boundsX  +  ( (RoomOrigin.y + boundsY) * LevelDimensions.x );
            
            
            if ( LevelLayout[levelIndex] != RoomId )
                return;
            
            //int x = (levelIndex % LevelDimensions.x) + GrowthDirection.x;
            //int y = (levelIndex / LevelDimensions.x) + GrowthDirection.y;
            int x = RoomOrigin.x + boundsX + GrowthDirection.x;
            int y =  RoomOrigin.y + boundsY + GrowthDirection.y;
            
            int checkIndex = x + (y * LevelDimensions.x);

            if ( IsInBounds( x, y ) && LevelLayout[checkIndex] == 0 && IsValidGrowthCell( x, y, Required ) )
            {
                LevelCell c = new LevelCell
                {
                    Cell = new int2(x,y),
                    Index = checkIndex
                };
                //NewCells.Enqueue( checkIndex );
                NewCells.Enqueue( c );
            }
        }
        

        //returns true if:
        //-the cell we started at belongs to the room
        //-it has at least (threshold) empty cells perpendicular to it that are connected to the room within (distance) units
        private bool IsValidGrowthCell(int x, int y, int threshold)
        {
            int2 perpendicular = math.abs( GrowthDirection.yx );
            int2 startPos = new int2(x,y) - GrowthDirection;//the original cell we started at before adding grow direction

            int countBelow = 0;
            for ( int i = 1; i <= threshold; i++ )
            {
                int2 curPos = startPos - (perpendicular * i);
                int index = curPos.x + curPos.y * LevelDimensions.x;

                if ( !IsInBounds( curPos.x, curPos.y ) || LevelLayout[index] != RoomId )
                {
                    break;
                }
                
                curPos += GrowthDirection;
                index = curPos.x + curPos.y * LevelDimensions.x;

                if ( IsInBounds( curPos.x, curPos.y ) && LevelLayout[index] == 0 )
                {
                    countBelow++;
                    if ( countBelow + 1 >= threshold )
                        return true;
                }
                else
                {
                    break;
                }
            }

            int countAbove = 0;
            for ( int i = 1; i <= threshold; i++ )
            {
                int2 curPos = startPos + (perpendicular * i);
                int index = curPos.x + curPos.y * LevelDimensions.x;

                if ( !IsInBounds( curPos.x, curPos.y ) || LevelLayout[index] != RoomId )
                {
                    break;
                }
                
                curPos += GrowthDirection;
                index = curPos.x + curPos.y * LevelDimensions.x;

                if ( IsInBounds( curPos.x, curPos.y ) && LevelLayout[index] == 0 )
                {
                    countAbove++;
                    if ( countAbove + countBelow + 1 >= threshold )
                        return true;
                }
                else
                {
                    break;
                }
            }

            return false;
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
    public struct LevelApplyGrowthResultJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public int4 RoomBounds;
        [ReadOnly] public int RoomId;
        [ReadOnly] public Vector2Int LevelDimensions;

        [ReadOnly] public NativeArray<LevelCell> NewCells;
        
        public NativeQueue<LevelConnection>.ParallelWriter Neighbors;
        public NativeQueue<int2>.ParallelWriter LocalMinima;
        public NativeQueue<int2>.ParallelWriter LocalMaxima;
        public void Execute(int index)
        {
            //int levelIndex = NewCells[index];
            LevelCell levelCell = NewCells[index];
            //LevelLayout[levelIndex] = RoomId;
            LevelLayout[levelCell.Index] = RoomId;
            
            
            //int x = levelCell.Index % LevelDimensions.x;
            //int y = levelCell.Index / LevelDimensions.x;

            int x = levelCell.Cell.x;
            int y = levelCell.Cell.y;
            
            int2 origin = new int2(x,y);
            CheckBounds( origin );
            
            CheckNeighbor( x + 1, y, origin );
            CheckNeighbor( x - 1, y, origin );
            CheckNeighbor( x, y + 1, origin );
            CheckNeighbor( x, y - 1, origin );

        }

        private void CheckBounds( int2 cell )
        {
            if ( cell.x < RoomBounds.x || cell.y < RoomBounds.y )
            {
                LocalMinima.Enqueue( math.min( cell, RoomBounds.xy ) );
            }
            
            if ( cell.x > RoomBounds.z || cell.y > RoomBounds.w )
            {
                LocalMaxima.Enqueue( math.max( cell, RoomBounds.zw ) );
            }
            
        }

        private void CheckNeighbor( int x, int y, int2 origin )
        {
            if ( !IsInBounds( x, y ) )
                return;

            int index = x + y * LevelDimensions.x;
            if ( LevelLayout[index] > 0 && LevelLayout[index] != RoomId )
            {
                LevelConnection connect = new LevelConnection{Origin = origin, RoomId = LevelLayout[index]};
                Neighbors.Enqueue( connect );
            }

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
    public struct LevelAnalyzeConnection : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public Vector2Int LevelDimensions;

        [ReadOnly] public int Required;
        [ReadOnly] public int RoomId;
        [ReadOnly] public int NeighborId;
        [ReadOnly] public NativeArray<LevelConnection> Connections;
        
        
        public NativeQueue<LevelConnection>.ParallelWriter ValidConnections;

        public void Execute(int index)
        {
            int arrIndex = index / 4;
            if ( Connections[arrIndex].RoomId != NeighborId )
                return;
            
            int2 cell = Connections[arrIndex].Origin;
            int dir = index % 4;

            if ( dir == 0 && GetHorizontalSpan( cell, -1 ) >= Required )
            {
                ValidConnections.Enqueue( Connections[arrIndex] );
            }
            else if ( dir == 1 && GetHorizontalSpan( cell, 1 ) >= Required )
            {
                ValidConnections.Enqueue( Connections[arrIndex] );
            }
            else if ( dir == 2 && GetVerticalSpan( cell, -1 ) >= Required )
            {
                ValidConnections.Enqueue( Connections[arrIndex] );
            }
            else if ( dir == 3 && GetVerticalSpan( cell, 1 ) >= Required )
            {
                ValidConnections.Enqueue( Connections[arrIndex] );
            }

        }


        private float GetHorizontalSpan( int2 cell, int dir )
        {
            int2 curCell = cell;
            bool foundCell = true;
            float distance = 0;
            while ( foundCell && distance < Required )
            {
                foundCell = false;
                for ( int y = -1; y <= 1; y++ )
                {
                    if ( IsRoomCell( curCell.x + dir, curCell.y + y ) && RadiusCheck( curCell.x + dir, curCell.y + y ) )
                    {
                        curCell.x += dir;
                        curCell.y += y;
                        distance = math.distance( cell, curCell );
                        foundCell = true;
                    }
                }
                
            }
            
            return distance;
        }

        private float GetVerticalSpan( int2 cell, int dir )
        {
            int2 curCell = cell;
            bool foundCell = true;
            float distance = 0;
            while ( foundCell && distance < Required )
            {
                foundCell = false;
                for ( int x = -1; x <= 1; x++ )
                {
                    if ( IsRoomCell( curCell.x + x, curCell.y + dir ) && RadiusCheck( curCell.x + x, curCell.y + dir ) )
                    {
                        curCell.x += x;
                        curCell.y += dir;
                        distance = math.distance( cell, curCell );
                        foundCell = true;
                    }
                }
                
            }
            
            return distance;
        }
        
        private bool RadiusCheck(int cellX, int cellY)
        {
            for ( int x = -1; x <= 1; x++ )
            {
                for ( int y = -1; y <= 1; y++ )
                {
                    if ( IsNeighborCell( cellX + x, cellY + y ) )
                        return true;
                }
            }

            return false;
        }
        
        private bool IsRoomCell( int x, int y )
        {
            if ( !IsInBounds( x, y ) )
                return false;

            int index = x + y * LevelDimensions.x;

            return LevelLayout[index] == RoomId;
        }
        
        private bool IsNeighborCell( int x, int y )
        {
            if ( !IsInBounds( x, y ) )
                return false;

            int index = x + y * LevelDimensions.x;

            return LevelLayout[index] == NeighborId;
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
    public struct LevelAnalyzeNormalRoom : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public Vector2Int LevelDimensions;
        
        [ReadOnly] public int RoomId;
        [ReadOnly] public int2 RoomOrigin;
        [ReadOnly] public int2 RoomSize;


        public NativeQueue<int>.ParallelWriter Corners;
        public void Execute(int index)
        {

            int boundsX = index % RoomSize.x;
            int boundsY = index / RoomSize.x;
            
            int levelIndex = (RoomOrigin.x + ( RoomOrigin.y * LevelDimensions.x )) + (boundsX + (boundsY*LevelDimensions.x));
            
            if ( LevelLayout[levelIndex] != RoomId )
                return;
            
            int x = levelIndex % LevelDimensions.x;
            int y = levelIndex / LevelDimensions.x;

            int checkIndex = x + y * LevelDimensions.x;

            if ( IsInBounds( x, y ) && LevelLayout[checkIndex] == RoomId && IsCorner( x,y ) )
            {
                Corners.Enqueue( 1 );
            }
        }


        private bool IsCorner( int x, int y )
        {
            int cnt = 0;

            cnt += IsNotRoom( x - 1, y - 1 );
            cnt += IsNotRoom( x - 1, y + 1 );
            cnt += IsNotRoom( x + 1, y - 1 );
            cnt += IsNotRoom( x + 1, y + 1 );

            return cnt == 1;
        }
        
        private int IsNotRoom( int x, int y )
        {
            if ( !IsInBounds( x, y ) )
                return 1;

            int index = x + y * LevelDimensions.x;
            if ( LevelLayout[index] != RoomId )
            {
                return 1;
            }

            return 0;
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
    public struct LevelPaintWallsJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;
        
        [ReadOnly] public Vector2Int LevelDimensions;
        
        [ReadOnly] public int RoomId;
        [ReadOnly] public int WallId;
        [ReadOnly] public int WallThickness;
        [ReadOnly] public int2 RoomOrigin;
        [ReadOnly] public int2 RoomSize;
        
        public void Execute(int index)
        {

            int boundsX = index % RoomSize.x;
            int boundsY = index / RoomSize.x;
            
            int levelIndex = (RoomOrigin.x + ( RoomOrigin.y * LevelDimensions.x )) + (boundsX + (boundsY*LevelDimensions.x));
            
            if ( LevelLayout[levelIndex] != RoomId )
                return;
            
            int x = levelIndex % LevelDimensions.x;
            int y = levelIndex / LevelDimensions.x;
            

            if ( IsWall( x, y ) )
            {
                LevelLayout[levelIndex] = WallId;
            }
            
        }


        private bool IsWall( int cellX, int cellY )
        {
            for ( int x = -WallThickness; x <= WallThickness; x++ )
            {
                for ( int y = -WallThickness; y <= WallThickness; y++ )
                {
                    if ( !IsInBounds( cellX + x, cellY + y ) )
                        return true;
                    int index = ( cellX + x ) + ( cellY + y ) * LevelDimensions.x;
                    if ( LevelLayout[index] != RoomId && LevelLayout[index] != WallId )
                        return true;
                }
            }

            return false;
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

    public struct LevelSpawnUnmanagedJob : IJobParallelFor
    {
        public Entity Prototype;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        
        [ReadOnly] public NativeArray<RenderBounds> MeshBounds;

        [ReadOnly] public NativeHashMap<int, EntityRenderInfo> EntityRenderMap;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);
            EntityRenderInfo info = EntityRenderMap[index];
            
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(info)});

            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(info.MaterialIndex, info.MeshIndex));
            Ecb.SetComponent(index, e, MeshBounds[info.MeshIndex]);
        }

        public float4x4 ComputeTransform(EntityRenderInfo info)
        {

            float4x4 M = float4x4.TRS(
                new float3(info.Position),
                quaternion.identity,
                new float3(1));

            return M;
        }

    }
    */


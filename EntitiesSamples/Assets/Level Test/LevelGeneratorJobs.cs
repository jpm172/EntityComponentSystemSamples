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
    public struct LevelGrowRoomJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public Vector2Int LevelDimensions;
        
        [ReadOnly] public int RoomId;
        [ReadOnly] public int WallThickness;
        [ReadOnly] public int WallId;
        [ReadOnly] public int2 RoomOrigin;
        [ReadOnly] public int2 RoomSize;
        [ReadOnly] public int2 GrowthDirection;
        [ReadOnly] public int Required;

        public NativeQueue<LevelCell>.ParallelWriter NewCells;
        public void Execute(int index)
        {
            int boundsX = index % RoomSize.x;
            int boundsY = index / RoomSize.x;
            
            int levelIndex = RoomOrigin.x + boundsX  +  ( (RoomOrigin.y + boundsY) * LevelDimensions.x );
            
            
            if ( LevelLayout[levelIndex] != WallId )
                return;
            
            int x = RoomOrigin.x + boundsX + GrowthDirection.x;
            int y =  RoomOrigin.y + boundsY + GrowthDirection.y;
            
            int checkIndex = x + (y * LevelDimensions.x);

            if ( IsInBounds( x, y ) && LevelLayout[checkIndex] == 0 && IsValidGrowthCell( x, y, Required ) )
            {
                int2 cell =new int2(x, y);
                
                LevelCell c = new LevelCell
                {
                    Cell = cell,
                    IsFloorCell = IsFloorCell( cell ),
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

                if ( !IsInBounds( curPos.x, curPos.y ) || LevelLayout[index] != WallId )
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

                if ( !IsInBounds( curPos.x, curPos.y ) || LevelLayout[index] != WallId )
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
        
        private bool IsFloorCell(int2 cell)
        {
            int2 perpendicular = math.abs( GrowthDirection.yx );
            cell -= GrowthDirection;
            for ( int x = -WallThickness; x <= WallThickness; x++ )
            {
                for ( int y = -WallThickness; y <= WallThickness; y++ )
                {
                    int2 pos = cell + new int2( x, y )*perpendicular;
                    int index = pos.x + pos.y * LevelDimensions.x;
                    if ( !IsInBounds( pos.x, pos.y ) || LevelLayout[index] != WallId )
                        return false;
                }
            }

            int2 bl = new int2(-1, -1);
            int2 br = new int2(1, -1);
            int2 tl = new int2(-1, 1);
            int2 tr = new int2(1, 1);


            cell += GrowthDirection;
            
            for ( int i = -WallThickness; i <= WallThickness; i++ )
            {
                int2 pos = cell + perpendicular*i;
                int index = pos.x + pos.y * LevelDimensions.x;
                if ( IsInBounds( pos.x, pos.y ) && LevelLayout[index] > 0 && LevelLayout[index] != WallId && LevelLayout[index] != RoomId )
                    return false;
            }



            return true;
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

public struct LevelCell
{
    public int2 Cell;
    public bool IsFloorCell;
    public int Index;
}

    [BurstCompile]
    public struct LevelApplyGrowthResultJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public int4 RoomBounds;
        [ReadOnly] public int2 GrowthDirection;
        [ReadOnly] public int RoomId;
        [ReadOnly] public int WallId;
        [ReadOnly] public int WallThickness;
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
            LevelLayout[levelCell.Index] = WallId;

            
            int2 floorCheck = levelCell.Cell - GrowthDirection * (WallThickness+1);
            int2 wallCheck = levelCell.Cell - GrowthDirection * WallThickness;
            int fcIndex = floorCheck.x + floorCheck.y * LevelDimensions.x;
            int wcIndex = wallCheck.x + wallCheck.y * LevelDimensions.x;
            
            /*
            if ( LevelLayout[fcIndex] == RoomId )
                LevelLayout[wcIndex] = RoomId;
                */
                
            
            if ( levelCell.IsFloorCell )
            {
                LevelLayout[wcIndex] = RoomId;
            }
            
            
            
            //int x = levelCell.Index % LevelDimensions.x;
            //int y = levelCell.Index / LevelDimensions.x;

            int x = levelCell.Cell.x;
            int y = levelCell.Cell.y;
            
            int2 origin = new int2(x,y);
            CheckBounds( origin );
            /*
            CheckNeighbor( x + 1, y, origin );
            CheckNeighbor( x - 1, y, origin );
            CheckNeighbor( x, y + 1, origin );
            CheckNeighbor( x, y - 1, origin );
            */

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


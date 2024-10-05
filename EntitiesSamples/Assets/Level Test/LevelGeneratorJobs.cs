using System;
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


    [BurstCompile]
    public struct LevelGrowRoomJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public int2 LevelDimensions;
        
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
            for ( int i = -WallThickness; i <= WallThickness; i++ )
            {
                //check the cell we grew from to see if it is near the edge of the room
                int2 wallCheck = cell - GrowthDirection + perpendicular*i;
                int index = wallCheck.x + wallCheck.y * LevelDimensions.x;
                if ( !IsInBounds( wallCheck.x, wallCheck.y ) || LevelLayout[index] != WallId )
                    return false;
                
                //check the new cell to see if it is in contact with another room
                int2 cornerCheck = cell + perpendicular*i;
                int cornerIndex = cornerCheck.x + cornerCheck.y * LevelDimensions.x;
                if ( IsInBounds( cornerCheck.x, cornerCheck.y ) && IsOtherRoom( cornerIndex ) )
                    return false;
            }



            return true;
        }

        private bool IsOtherRoom( int index )
        {
            return LevelLayout[index] > 0 && LevelLayout[index] != WallId && LevelLayout[index] != RoomId;
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
        [ReadOnly] public int2 LevelDimensions;

        [ReadOnly] public NativeArray<LevelCell> NewCells;
        
        public NativeQueue<int2>.ParallelWriter LocalMinima;
        public NativeQueue<int2>.ParallelWriter LocalMaxima;
        public void Execute(int index)
        {
            LevelCell levelCell = NewCells[index];
            LevelLayout[levelCell.Index] = WallId;

            
            if ( levelCell.IsFloorCell )
            {
                int2 wallCheck = levelCell.Cell - GrowthDirection * WallThickness;
                int wcIndex = wallCheck.x + wallCheck.y * LevelDimensions.x;
                LevelLayout[wcIndex] = RoomId;
            }
            
            int2 origin = levelCell.Cell;
            CheckBounds( origin );
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
    }

    [BurstCompile]
    public struct LevelCheckForConnectionsJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public NativeArray<RoomInfo> RoomInfo;
        [ReadOnly] public int2 GrowthDirection;
        [ReadOnly] public int RoomId;
        [ReadOnly] public int WallId;
        [ReadOnly] public int2 LevelDimensions;

        [ReadOnly] public NativeArray<LevelCell> NewCells;
        
        public NativeQueue<LevelConnectionInfo>.ParallelWriter Neighbors;
        
        public void Execute(int index)
        {
            LevelCell levelCell = NewCells[index];


            int2 origin = levelCell.Cell;

            CheckNeighbor( origin.x + 1, origin.y, origin, index );
            CheckNeighbor( origin.x - 1, origin.y, origin, index );
            CheckNeighbor( origin.x, origin.y + 1, origin, index );
            CheckNeighbor( origin.x, origin.y - 1, origin, index );
        }
        

        private void CheckNeighbor( int x, int y, int2 origin, int streamIndex )
        {
            if ( !IsInBounds( x, y ) )
                return;

            int index = x + y * LevelDimensions.x;
            if ( LevelLayout[index] > 0 )
            {
                TryAddConnection( index, new int2(x, y), origin, streamIndex );
            }
        }

        private void TryAddConnection( int index, int2 other, int2 origin, int streamIndex )
        {
            bool isOther = LevelLayout[index] > 0 && LevelLayout[index] != WallId && LevelLayout[index] != RoomId;
            if ( !isOther )
                return;
            
            int otherId = (LevelLayout[index] % ( RoomInfo.Length + 1 ) ) + 1;

            int thickness = RoomInfo[RoomId - 1].WallThickness;
            int otherThickness = RoomInfo[otherId -1].WallThickness;
            
            int2 dir = other - origin;
            int2 thicknessVector = new int2(thickness, thickness) * -dir;
            int2 otherThicknessVector = new int2(otherThickness + 1, otherThickness + 1) * dir ;
            
            if ( !math.abs( dir ).Equals(  math.abs( GrowthDirection ) ) )
            {
                thicknessVector -= GrowthDirection * thickness;
                otherThicknessVector -= GrowthDirection * thickness;
            }
            
            int2 check1 = origin + thicknessVector;
            int2 check2 = origin + otherThicknessVector;

            int index1 = check1.x + check1.y * LevelDimensions.x;
            int index2 = check2.x + check2.y * LevelDimensions.x;

            if ( LevelLayout[index1] != RoomId || LevelLayout[index2] != otherId )
                return;
            
            //LevelConnectionInfo connect = new LevelConnectionInfo{Origin = origin, RoomId = otherId};
            int4 bounds = new int4(math.min( check1,check2 ), math.max( check1, check2 ));
            LevelConnectionInfo connect = new LevelConnectionInfo(RoomId, otherId, bounds, dir );
            Neighbors.Enqueue( connect );
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
    public struct LevelConvertToFloorJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public int RoomCount;
        [ReadOnly] public NativeArray<int4> Pieces;
        [ReadOnly] public int2 LevelDimensions;
        
        public void Execute(int index)
        {
            int4 bounds = Pieces[index];
            int2 size = bounds.Size();

            for ( int x = 0; x < size.x; x++ )
            {
                for ( int y = 0; y < size.y; y++ )
                {
                    int levelIndex = ( bounds.x + x ) + ( ( bounds.y + y ) * LevelDimensions.x );
                    int value = LevelLayout[levelIndex];
                    LevelLayout[levelIndex] = math.select( value, value - RoomCount, value > RoomCount );
                }  
            }
           
        }
        
    }

    [BurstCompile]
    public struct LevelFetchWallsJob : IJobParallelFor
    {
        [ReadOnly]public NativeArray<int> LevelLayout;
        [ReadOnly] public NativeArray<RoomInfo> RoomInfo;
        
        [ReadOnly] public int2 LevelDimensions;
        [ReadOnly] public int WallRadius;
        
        public NativeQueue<WallInfo>.ParallelWriter WallCells;
        public void Execute(int index)
        {
            int x = index % LevelDimensions.x;
            int y = index / LevelDimensions.x;

            //todo: this works to enforce a minimum border, but need to handle the edge case for walls against the border of the level
            /*
            if ( LevelLayout[index] == 0 && IsBorderingCell( x, y ) )
            {
                WallCells.Enqueue( new WallInfo
                {
                    Material = LevelMaterial.Indestructible,
                    Position = new int2(x,y)
                } );
            }
            */
                
            //skip past empty space/floor cells
            if ( LevelLayout[index] <= RoomInfo.Length )
                return;

            int wallId = LevelLayout[index];
            int roomIndex = wallId - RoomInfo.Length - 1;

            LevelMaterial mat = GetStrongestMaterialInRadius( x, y, RoomInfo[roomIndex], wallId );
            WallCells.Enqueue( new WallInfo
            {
                Material = mat,
                Position = new int2(x,y)
            } );

        }


        private bool IsBorderingCell( int startX, int startY )
        {
            int thickness = 3;

            for ( int x = -thickness; x <= thickness; x++ )
            {
                for ( int y = -thickness; y <= thickness; y++ )
                {
                    int xPos = startX + x;
                    int yPos = startY + y;

                    if ( !IsInBounds( xPos, yPos ) )
                        continue;

                    int index = xPos + yPos * LevelDimensions.x;


                    if(LevelLayout[index] > 0 && LevelLayout[index] <= RoomInfo.Length)
                        return true;
                }
            }

            return false;
        }
        
        private LevelMaterial GetStrongestMaterialInRadius(int startX, int startY, RoomInfo info, int wallId)
        {
            int thickness = info.WallThickness;
            LevelMaterial result = info.WallMaterial;

            for ( int x = -thickness; x <= thickness; x++ )
            {
                for ( int y = -thickness; y <= thickness; y++ )
                {
                    int xPos = startX + x;
                    int yPos = startY + y;

                    if ( !IsInBounds( xPos, yPos ) )
                        return LevelMaterial.Indestructible;

                    int index = xPos + yPos * LevelDimensions.x;
                    
                    if(LevelLayout[index] == 0)
                        return LevelMaterial.Indestructible;


                    int otherIndex = LevelLayout[index] - RoomInfo.Length - 1;
                    if ( LevelLayout[index] > RoomInfo.Length && LevelLayout[index] != wallId && result < RoomInfo[otherIndex].WallMaterial )
                        result = RoomInfo[otherIndex].WallMaterial;
                }
            }
            

            return result;
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
    public struct LevelBinWallsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<WallInfo> WallCells;
        [ReadOnly] public LevelMaterial TargetMaterial;
        
        [ReadOnly] public int BinSize;
        [ReadOnly] public int XBins;

        public NativeArray<bool> BinTracker;
        public NativeParallelMultiHashMap<int2, int2>.ParallelWriter BinnedWalls;
        public void Execute( int index )
        {
            int binX = index % XBins;
            int binY = index / XBins;
            int2 key = new int2(binX, binY);

            foreach ( WallInfo wall in WallCells )
            {
                if ( wall.Material != TargetMaterial )
                    continue;
                
                int x = wall.Position.x / BinSize;
                int y = wall.Position.y / BinSize;

                if (  x == binX && y == binY   )
                {
                    BinnedWalls.Add( key, wall.Position );
                    BinTracker[index] = true;
                }
            }
            
        }
    }



public struct LevelCreateWallsJob : IJobParallelFor
{
    [ReadOnly] public NativeParallelMultiHashMap<int2, int2> BinnedWalls;
    [ReadOnly] public NativeArray<bool> BinTracker;

    [ReadOnly] public int BinSize;
    [ReadOnly] public int XBins;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> PointFields;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<int> Counts;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<int2> Positions;
    public void Execute( int index )
    {
        
        if(!BinTracker[index])
            return;

        
        int startIndex = 0;
        int countsIndex = 0;
        for ( int i = 0; i < index; i++ )
        {
            if ( BinTracker[i] )
            {
                startIndex += BinSize * BinSize;
                countsIndex++;
            }
                
        }
        
        int binX = index % XBins;
        int binY = index / XBins;
        int2 key = new int2(binX, binY);

        int2 origin = key * BinSize;
        Positions[countsIndex] = origin;
        
        NativeParallelMultiHashMap<int2, int2>.Enumerator enumerator =  BinnedWalls.GetValuesForKey( key );

        while ( enumerator.MoveNext() )
        {
            int2 pos = enumerator.Current - origin;
            int posIndex = startIndex + pos.x + pos.y * BinSize;
            PointFields[posIndex] = 1;
            Counts[countsIndex]++;
        }

    }
}

public struct WallInfo
{
    public int2 Position;
    public LevelMaterial Material;
}

[BurstCompile]
    public struct LevelAnalyzeNormalRoom : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> LevelLayout;
        [ReadOnly] public int2 LevelDimensions;
        
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
    public struct DijkstrasPathJob : IJob
    {
        [ReadOnly] public int StartRoom;
        [ReadOnly] public int RoomCount;
        private static readonly int INF = Int32.MaxValue;
        [ReadOnly] public NativeArray<int> AdjacencyMatrix;
        
        public NativeArray<int> Path;
        public NativeArray<int> Distances;
        public NativeArray<bool> sptSet;//shortest path tree set

        public NativeReference<bool> Success;


        
        public void Execute()
        {
            for ( int i = 0; i < Distances.Length; i++ )
            {
                Distances[i] = INF;
                sptSet[i] = false;
            }
            Distances[StartRoom - 1] = 0;
            
            

            for ( int i = 0; i < RoomCount; i++ )
            {
                int u = MinDistance();

                if ( u == -1 )
                    return;
                
                sptSet[u] = true;

                for ( int v = 0; v < RoomCount; v++ )
                {
                    // Update dist[v] only if is not in
                    // sptSet, there is an edge from u
                    // to v, and total weight of path
                    // from src to v through u is smaller
                    // than current value of dist[v]
                    int index = ( u * RoomCount ) + v;
                    if ( !sptSet[v] && AdjacencyMatrix[index] != 0
                                    && Distances[u] != INF
                                    && Distances[u] + AdjacencyMatrix[index] < Distances[v] )
                    {
                        Distances[v] = Distances[u] + AdjacencyMatrix[index];
                        Path[v] = u;
                    }
                        
                }
            }
            
            Success.Value = true;
        }
        
        private int MinDistance()
        {
            // Initialize min value
            int min = INF, min_index = -1;

            for ( int v = 0; v < RoomCount; v++ )
            {
                if (sptSet[v] == false && Distances[v] < min) 
                {
                    min = Distances[v];
                    min_index = v;
                }
            }
                

            return min_index;
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


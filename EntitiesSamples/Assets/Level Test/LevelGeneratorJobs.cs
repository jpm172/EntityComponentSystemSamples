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
        [ReadOnly] public Vector2Int LevelDimensions;
        
        [ReadOnly] public int RoomId;
        [ReadOnly] public int2 RoomOrigin;
        [ReadOnly] public int2 RoomSize;
        [ReadOnly] public int2 GrowthDirection;
        [ReadOnly] public int required;

        public NativeList<int>.ParallelWriter NewCells;
        public void Execute(int index)
        {

            int boundsX = index % RoomSize.x;
            int boundsY = index / RoomSize.x;
            
            int levelIndex = (RoomOrigin.x + ( RoomOrigin.y * LevelDimensions.x )) + (boundsX + (boundsY*LevelDimensions.x));
            
            if ( LevelLayout[levelIndex] != RoomId )
                return;
            
            int x = (levelIndex % LevelDimensions.x) + GrowthDirection.x;
            int y = (levelIndex / LevelDimensions.x) + GrowthDirection.y;

            int checkIndex = x + y * LevelDimensions.x;

            if ( IsInBounds( x, y ) && LevelLayout[checkIndex] == 0 && IsValidGrowthCell( x, y, required, required ) )
            {
                NewCells.AddNoResize( checkIndex );
            }
        }
        

        //returns true if:
        //-the cell we started at belongs to the room
        //-it has at least (threshold) empty cells perpendicular to it that are connected to the room within (distance) units
        private bool IsValidGrowthCell(int x, int y, int distance, int threshold)
        {
            int2 perpendicular = math.abs( GrowthDirection.yx );
            int2 startPos = new int2(x,y) - GrowthDirection;//the original cell we started at before adding grow direction
            
            int count = 0;
            for ( int i = -distance; i <= distance; i++ )
            {
                int2 curPos = startPos + (perpendicular * i);
                int index = curPos.x + curPos.y * LevelDimensions.x;

                if ( !IsInBounds( curPos.x, curPos.y ) || LevelLayout[index] != RoomId )
                {
                    count = 0;
                    continue;
                }
                
                curPos += GrowthDirection;
                index = curPos.x + curPos.y * LevelDimensions.x;

                if ( IsInBounds( curPos.x, curPos.y ) && LevelLayout[index] == 0 )
                {
                    count++;
                    if ( count >= threshold )
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

    [BurstCompile]
    public struct LevelApplyGrowthResultJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public int RoomId;
        [ReadOnly] public Vector2Int LevelDimensions;

        [ReadOnly] public NativeList<int> NewCells;
        
        public NativeQueue<int>.ParallelWriter Neighbors;
        public void Execute(int index)
        {
            int levelIndex = NewCells[index];
            LevelLayout[levelIndex] = RoomId;
            
            int x = levelIndex % LevelDimensions.x;
            int y = levelIndex / LevelDimensions.x;

            CheckNeighbor( x + 1, y );
            CheckNeighbor( x - 1, y );
            CheckNeighbor( x, y + 1 );
            CheckNeighbor( x, y - 1 );

        }

        private void CheckNeighbor( int x, int y )
        {
            if ( !IsInBounds( x, y ) )
                return;

            int index = x + y * LevelDimensions.x;
            if ( LevelLayout[index] > 0 && LevelLayout[index] != RoomId )
            {
                Neighbors.Enqueue( LevelLayout[index] );
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


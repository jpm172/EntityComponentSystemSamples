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

            
            if ( IsInBounds( x, y ) && LevelLayout[checkIndex] == 0  )
            {
                NewCells.AddNoResize( checkIndex );
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

    public struct LevelApplyGrowthResultJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> LevelLayout;

        [ReadOnly] public int RoomId;

        [ReadOnly] public NativeList<int> NewCells;
        public void Execute(int index)
        {
            int levelIndex = NewCells[index];
            LevelLayout[levelIndex] = RoomId;
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


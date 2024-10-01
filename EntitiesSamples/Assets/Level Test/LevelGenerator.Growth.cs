using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class LevelGenerator
{
    private int _cornerLimit = 6;
    private int _counter = 0;
    private int _totalSteps = 0;

    public bool breakPoint = false;
    
    public bool StepWiseGrow;
    public int GrowSteps;
    public void GrowRooms()
    {
        if ( StepWiseGrow )
        {
            StepMainGrow();
        }
        else
        {
            MainGrow();
        }
        
    }

    //float startTime = Time.realtimeSinceStartup; Debug.Log( "Random Grow done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );


    private void StepMainGrow()
    {
        _totalSteps = 0;
        bool hasPath = false;
        while ( _totalSteps < GrowSteps )
        {
            LevelRoom room = _rooms[_counter];
            int connections = _edgeDictionary[room.Id].Count;
            GrowRoom( room );


            if ( connections != _edgeDictionary[room.Id].Count )
            {
                hasPath = HasPath( room.Id );
            }

            _counter = ( _counter + 1 ) % _rooms.Length;
            _totalSteps++;
        }
        MakeRoomMeshes();
    }
    
    private void MainGrow()
    {
        _totalSteps = 0;
        int totalConnections = 0;
        bool hasPath = false;
        
        while ( !hasPath  )
        {
            LevelRoom room = _rooms[_counter];
            
            //add any new connections to the counter after growing the room
            int connections = _edgeDictionary[room.Id].Count;
            GrowRoom( room );
            totalConnections += _edgeDictionary[room.Id].Count - connections;

            if ( totalConnections >= _rooms.Length - 1 ) //only run the pathfinding once we have at least the minimum connections (# of rooms - 1)
            {
                hasPath = BurstHasPath( room.Id );
            }
            

            _counter = ( _counter + 1 ) % _rooms.Length;
            _totalSteps++;
        }
        
        MakeRoomMeshes();
    }
    
    

    private void GrowRoom( LevelRoom room )
    {
        
        if ( !room.CanGrow )
            return;
        
        if ( room.GrowthType == LevelGrowthType.Normal )
        {
            int2 growthDirection = GetRandomGrowthDirection( room );
            
            for ( int n = 0; n < _minRoomSeedSize; n++ )
            {
                int dirsBefore = room.XGrowthDirections.Count + room.YGrowthDirections.Count;
                
                NormalGrow( room, growthDirection );
                
                if ( room.XGrowthDirections.Count + room.YGrowthDirections.Count != dirsBefore )
                    return;
            }
        }
        else if ( room.GrowthType == LevelGrowthType.Mold )
        {
            //TODO implement mold growth
        }
    }

    private int2 GetRandomGrowthDirection( LevelRoom room )
    {
        
        bool hasHorizontal = room.XGrowthDirections.Count > 0;
        bool hasVertical = room.YGrowthDirections.Count > 0;

        int rand = 0;
        if ( hasHorizontal && hasVertical )
        {
            int sum = room.SizeRatio.x + room.SizeRatio.y;
            
            
            float xChance = (float)room.SizeRatio.x / sum;
            if ( Random.Range( 0f,1f ) < xChance )
            {
                rand = Random.Range( 0, room.XGrowthDirections.Count );
                return room.XGrowthDirections[rand];
            }
            else
            {
                rand = Random.Range( 0, room.YGrowthDirections.Count );
                return room.YGrowthDirections[rand];
            }
        }
        else if ( hasHorizontal )
        {
            rand = Random.Range( 0, room.XGrowthDirections.Count );
            return room.XGrowthDirections[rand];
        }

        rand = Random.Range( 0, room.YGrowthDirections.Count );
        return room.YGrowthDirections[rand];
        
    }

    private void NormalGrow( LevelRoom room, int2 growthDirection )
    {
        NativeQueue<LevelCell> newCells = new NativeQueue<LevelCell>( Allocator.TempJob);
        LevelGrowRoomJob growRoomJob = new LevelGrowRoomJob
        {
            GrowthDirection = growthDirection,
            Required = _minRoomSeedSize + (room.WallThickness*2),
            LevelDimensions = dimensions,
            LevelLayout = _levelLayout,
            RoomId = room.Id,
            WallId = room.WallId,
            WallThickness = room.WallThickness,
            RoomSize = room.Size,
            RoomOrigin = room.Origin,
            NewCells = newCells.AsParallelWriter()
        };
        
        
        JobHandle handle = growRoomJob.Schedule(room.Size.x * room.Size.y, 32);
        handle.Complete();

        bool removeDirection = false;
        //if we added cells, playback the results to paint them onto the level
        if ( newCells.Count > 0 )
        {
            NativeQueue<LevelConnectionInfo> newNeighbors = new NativeQueue<LevelConnectionInfo>(Allocator.TempJob);
            NativeQueue<int2> localMinima = new NativeQueue<int2>(Allocator.TempJob);
            NativeQueue<int2> localMaxima = new NativeQueue<int2>(Allocator.TempJob);
            //NativeParallelMultiHashMap<int2, int> neighborMap = new NativeParallelMultiHashMap<int2, int>(newCells.Length*3, Allocator.TempJob);
            
            NativeArray<LevelCell> newCellsArray = newCells.ToArray( Allocator.TempJob );
            
            LevelApplyGrowthResultJob applyJob = new LevelApplyGrowthResultJob
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                GrowthDirection = growthDirection,
                LocalMinima = localMinima.AsParallelWriter(),
                LocalMaxima = localMaxima.AsParallelWriter(),
                NewCells = newCellsArray,
                RoomId = room.Id,
                WallId = room.WallId,
                WallThickness = room.WallThickness,
                RoomBounds = room.Bounds
            };
            
            JobHandle applyHandle = applyJob.Schedule(newCellsArray.Length, 32);
            
            
            LevelCheckForConnectionsJob checkConnectJob = new LevelCheckForConnectionsJob
            {
                LevelLayout = _levelLayout,
                LevelDimensions = dimensions,
                RoomInfo = _roomInfo,
                GrowthDirection = growthDirection,
                Neighbors = newNeighbors.AsParallelWriter(),
                NewCells = newCellsArray,
                RoomId = room.Id,
                WallId = room.WallId,
            };
            
            JobHandle checkConnectHandle = checkConnectJob.Schedule(newCellsArray.Length, 32, applyHandle);
            checkConnectHandle.Complete();

            if ( localMinima.Count + localMaxima.Count > 0 )
            {
                ResizeRoom( room, localMinima, localMaxima );
            }

            localMaxima.Dispose();
            localMinima.Dispose();
            newCellsArray.Dispose();

            
            
            AddConnections( newNeighbors, room );
            
            newNeighbors.Dispose();
        }
        else
        {
            removeDirection = true;
        }

        if ( removeDirection )
        {
            //if it didn't grow in the current direction, remove it from the list of valid growth directions
            if ( math.abs( growthDirection.x ) > math.abs( growthDirection.y ) )
            {
                room.XGrowthDirections.Remove( growthDirection );
            }
            else
            {
                room.YGrowthDirections.Remove( growthDirection );
            }
        }

        newCells.Dispose();
    }


    private void ResizeRoom( LevelRoom room,  NativeQueue<int2> localMinima,  NativeQueue<int2> localMaxima )
    {
        int2 minima = room.Bounds.xy;
        int2 maxima = room.Bounds.zw;
        while ( localMinima.TryDequeue( out int2 value ) )
        {
            minima = math.min( value, minima );
        }

        while ( localMaxima.TryDequeue( out int2 value ) )
        {
            maxima = math.max( value, maxima );
        }

        room.Bounds = new int4(minima,maxima);
    }
    
    
    private void AddConnections( NativeQueue<LevelConnectionInfo> connections, LevelRoom room )
    {

        while ( connections.TryDequeue( out LevelConnectionInfo cnct ) )
        {
            if ( !_roomConnections.ContainsKey( cnct.Connections ) )
            {
                _roomConnections[cnct.Connections] = new List<LevelConnectionManager>();
            }
            
            LevelConnectionManager newConnection = new LevelConnectionManager( cnct.Bounds, cnct.Direction );
            _roomConnections[cnct.Connections].Add( newConnection );
            List<LevelConnectionManager> roomConnections = _roomConnections[cnct.Connections];
            for ( int i = roomConnections.Count-2; i >= 0 ; i-- )
            {
                if ( newConnection.TryMerge( roomConnections[i] ) )
                {
                    roomConnections.RemoveAt( i );
    
                    if ( newConnection.GetLargestDimension() >= _minRoomSeedSize )
                    {
                        SetNeighbors( cnct.Connections.x, cnct.Connections.y, room.Weight );
                    }
                }
            }
        }
    }
    
    private void AddConnection( LevelConnectionInfo cnct, LevelRoom room )
    {

        if ( !_roomConnections.ContainsKey( cnct.Connections ) )
        {
            _roomConnections[cnct.Connections] = new List<LevelConnectionManager>();
        }
        
        LevelConnectionManager newConnection = new LevelConnectionManager( cnct.Bounds, cnct.Direction );

        _roomConnections[cnct.Connections].Add( newConnection );
        if ( newConnection.GetLargestDimension() >= _minRoomSeedSize )
        {
            SetNeighbors( cnct.Connections.x, cnct.Connections.y, room.Weight );
        }
    }
    
    
    
}

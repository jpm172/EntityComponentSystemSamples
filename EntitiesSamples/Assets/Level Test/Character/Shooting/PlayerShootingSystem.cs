using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;

using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;


//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] works for schedule, not run
//[UpdateAfter(typeof(PhysicsSystemGroup))]

//[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast =  true)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
//float startTime = Time.realtimeSinceStartup; Debug.Log( "done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );
public partial struct PlayerShootingSystem : ISystem
{
    private NativeHashMap<int2, BlobAssetReference<Unity.Physics.Collider>> _colliderMap;
    private Random _rng;
    private EntityQuery _playerQuery;
    private static readonly int PointsBuffer = Shader.PropertyToID( "_PointsBuffer" );

    public void OnCreate( ref SystemState state )
    {
        CreateColliderMap( 32 );
        _rng = Random.CreateFromIndex( 100 );
        _playerQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PlayerInputs>().Build(ref state);
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public void OnDestroy( ref SystemState state )
    {
        
        foreach ( BlobAssetReference<Collider> col in _colliderMap.GetValueArray( Allocator.Temp ) )
        {
            col.Dispose();
        }
        _colliderMap.Dispose();
    }

    public void OnUpdate( ref SystemState state )
    {
        
        state.EntityManager.CompleteDependencyBeforeRW<PhysicsWorldSingleton>();
        
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
       NativeList<ShootInfo> hitEntities = new NativeList<ShootInfo>(32, Allocator.TempJob);
       NativeParallelMultiHashMap<ShootInfo,Entity> entityHitMap = new NativeParallelMultiHashMap<ShootInfo,Entity>(32, Allocator.TempJob);
       //Entity player = _playerQuery.ToEntityArray( Allocator.Temp )[0]; //THIS DOESNT WORK IN BUILD
       EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

       
       NativeReference<WeaponInfo> firedWeapon = new NativeReference<WeaponInfo>(Allocator.TempJob);
        
       new PlayerShootJob
       {
           PhysicsWorld = physicsWorld,
           Hits = hitEntities.AsParallelWriter(),
           EntityHitMap = entityHitMap,
           FiredWeapon = firedWeapon,
           RNG = Random.CreateFromIndex( _rng.NextUInt() )
       }.Run();
       
       //state.EntityManager.SetComponentData( player, firedWeapon.Value );

       if ( hitEntities.Length > 0 )
       {
           NativeHashMap<Entity, int> destroyedEntities = new NativeHashMap<Entity, int>(hitEntities.Length, Allocator.TempJob);
           NativeArray<ShootInfo> shootKeys = entityHitMap.GetKeyArray( Allocator.TempJob );
           //NativeHashSet<ShootInfo> check = new NativeHashSet<ShootInfo>(shootKeys.Length, Allocator.Temp);

             
           //foreach ( ShootInfo shootInfo in shootKeys )
           for ( int i = shootKeys.Length-1; i >= 0; i-- )
           {
               ShootInfo shootInfo = shootKeys[i];
               

               Entity entity = shootInfo.Hit.Entity;
               StructureInfo structure = state.EntityManager.GetComponentData<StructureInfo>( entity );
               if ( structure.Material == LevelMaterial.Indestructible )//Entity collider map is organized wrong, this doesnt work
               {
                   i -= shootInfo.Step;
                   continue;
               }

               if ( !destroyedEntities.ContainsKey( entity ) )
                   destroyedEntities.Add( entity, shootInfo.Hit.RigidBodyIndex);
               
               DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( entity );
               LocalToWorld ltw = state.EntityManager.GetComponentData<LocalToWorld>( entity );

               new DestroyStructureJob
               {
                   Data = data,
                   EntityPosition = ltw,
                   Info = shootInfo,
                   FiredWeapon = firedWeapon,
                   PPU = GameSettings.PixelsPerUnit,
                   Dimensions = GameSettings.Dimensions
               }.Run();
           }


           foreach ( Entity entity in destroyedEntities.GetKeyArray( Allocator.Temp ) )
           {
               DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( entity );
               
               BufferData d = state.EntityManager.GetComponentData<BufferData>( entity );
               d.SetBuffer(data.Reinterpret<int>().AsNativeArray().ToArray());
               
               int dim = 32;
                NativeParallelMultiHashMap<int, MeshStrip> colStrips = new NativeParallelMultiHashMap<int, MeshStrip>( data.Length, Allocator.TempJob);
                new MakeColliderStripsJob
                {
                    Data = data,
                    Dimensions = new int2(dim, dim),
                    Strips = colStrips.AsParallelWriter()
                }.Run( dim );

                
                NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(data.Length, Allocator.TempJob);
                
                new MergeColliderStripsJob
                {
                    MergedStrips = mergedStrips.AsParallelWriter(),
                    Strips = colStrips
                }.Run( dim );

                
                
                
                NativeArray<MeshStrip> geometry = mergedStrips.GetValueArray( Allocator.Temp );
                //destroy the structure if there is no geometry
                if ( geometry.Length == 0 )
                {
                    ecb.DestroyEntity( entity );
                    shootKeys.Dispose();
                    destroyedEntities.Dispose();
                    colStrips.Dispose();
                    mergedStrips.Dispose();
                    entityHitMap.Dispose();
                    hitEntities.Dispose();
                    firedWeapon.Dispose();
                    return;//
                }
                
                int count = geometry.Length;

                NativeArray<CompoundCollider.ColliderBlobInstance> childCols 
                    = new NativeArray<CompoundCollider.ColliderBlobInstance>(count, Allocator.Temp);
               
                int counter = 0;

                foreach ( MeshStrip strip in geometry )
                {
                    int2 bottomLeft = strip.Start;
                    int2 topRight = strip.End;

                    float3 position = ( new float3( bottomLeft.x, bottomLeft.y, 0 ) / GameSettings.PixelsPerUnit );

                    BlobAssetReference<Collider> col = _colliderMap[topRight - bottomLeft];
                    CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = col,
                        Entity = entity,
                        CompoundFromChild = new RigidTransform
                        {
                            rot = quaternion.identity,
                            pos = position
                        }
                    };

                    childCols[counter] = newChild;

                    counter++;
                }
                

                //store the old collider in the cleanup component to be disposed later
                ecb.SetComponentEnabled( entity, typeof(OldCollider), true );
                ecb.SetComponent( entity, new OldCollider{Value = physicsWorld.Bodies[destroyedEntities[entity]].Collider} );
                
                //Debug.Log( "create" );

                
                PhysicsCollider physicsCollider = new PhysicsCollider
                {
                    Value = CompoundCollider.Create( childCols )
                };
                ecb.SetComponent( entity, new DestructibleCleanUp{Value = physicsCollider} );
                ecb.SetComponent( entity, physicsCollider );
                
                //geometry.Dispose();
                mergedStrips.Dispose();
                colStrips.Dispose();
               
           }
           

           shootKeys.Dispose();
           destroyedEntities.Dispose();
       }
       
        /*
       if ( hitEntities.Length > 0 )
       {
           foreach ( ShootInfo shootInfo in hitEntities )
           {
                RaycastHit hit = shootInfo.Hit;
                Entity e = hit.Entity;

                StructureInfo structure = state.EntityManager.GetComponentData<StructureInfo>( e );

                if ( structure.Material == LevelMaterial.Indestructible )
                {
                    //Debug.Log( "indestructible" );//
                    firedWeapon.Dispose();
                    hitEntities.Dispose();
                    return;
                }

                DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( e );
                LocalToWorld ltw = state.EntityManager.GetComponentData<LocalToWorld>( e );

                new DestroyStructureJob
                {
                    Data = data,
                    EntityPosition = ltw,
                    Info = shootInfo,
                    FiredWeapon = firedWeapon,
                    PPU = GameSettings.PixelsPerUnit,
                    Dimensions = GameSettings.Dimensions
                }.Run();

                
                BufferData d = state.EntityManager.GetComponentData<BufferData>( e );
                d.SetBuffer(data.Reinterpret<int>().AsNativeArray().ToArray());
                
                MaterialMeshInfo info = state.EntityManager.GetComponentData<MaterialMeshInfo>( e );
                RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(e);
                arr.GetMaterial( info ).SetBuffer( PointsBuffer, d.Buffer );

                int dim = 32;
                NativeParallelMultiHashMap<int, MeshStrip> colStrips = new NativeParallelMultiHashMap<int, MeshStrip>( data.Length, Allocator.TempJob);
                new MakeColliderStripsJob
                {
                    Data = data,
                    Dimensions = new int2(dim, dim),
                    Strips = colStrips.AsParallelWriter()
                }.Run( dim );

                
                NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(data.Length, Allocator.TempJob);
                
                new MergeColliderStripsJob
                {
                    MergedStrips = mergedStrips.AsParallelWriter(),
                    Strips = colStrips
                }.Run( dim );

                
                
                
                NativeArray<MeshStrip> geometry = mergedStrips.GetValueArray( Allocator.Temp );
                //destroy the structure if there is no geometry
                if ( geometry.Length == 0 )
                {
                    ecb.DestroyEntity( e );
                    colStrips.Dispose();
                    mergedStrips.Dispose();
                    hitEntities.Dispose();
                    firedWeapon.Dispose();
                    return;
                }
                
                int count = geometry.Length;

                NativeArray<CompoundCollider.ColliderBlobInstance> childCols 
                    = new NativeArray<CompoundCollider.ColliderBlobInstance>(count, Allocator.Temp);
               
                int counter = 0;

                foreach ( MeshStrip strip in geometry )
                {
                    int2 bottomLeft = strip.Start;
                    int2 topRight = strip.End;

                    float3 position = ( new float3( bottomLeft.x, bottomLeft.y, 0 ) / GameSettings.PixelsPerUnit );

                    BlobAssetReference<Collider> col = _colliderMap[topRight - bottomLeft];
                    CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = col,
                        Entity = e,
                        CompoundFromChild = new RigidTransform
                        {
                            rot = quaternion.identity,
                            pos = position
                        }
                    };

                    childCols[counter] = newChild;

                    counter++;
                }
                

                //store the old collider in the cleanup component to be disposed later
                ecb.SetComponentEnabled( e, typeof(OldCollider), true );
                ecb.SetComponent( e, new OldCollider{Value = physicsWorld.Bodies[hit.RigidBodyIndex].Collider} );
                
                //Debug.Log( "create" );

                
                PhysicsCollider physicsCollider = new PhysicsCollider
                {
                    Value = CompoundCollider.Create( childCols )
                };
                ecb.SetComponent( e, new DestructibleCleanUp{Value = physicsCollider} );
                ecb.SetComponent( e, physicsCollider );
                
                //geometry.Dispose();
                mergedStrips.Dispose();
                colStrips.Dispose();
           }
        }
        */

        
       entityHitMap.Dispose();
        hitEntities.Dispose();
        firedWeapon.Dispose();
    }

    private void CreateColliderMap( int binSize )
    {
        _colliderMap = new NativeHashMap<int2, BlobAssetReference<Collider>>(binSize*binSize, Allocator.Persistent);
        
        for ( int x = 0; x < binSize; x++ )
        {
            for ( int y = 0; y < binSize; y++ )
            {
                int2 key = new int2( x, y );
                int2 bottomLeft = new int2(0,0);
                int2 topRight = new int2(x,y);
                float3 center = (new float3( topRight.x, bottomLeft.y + topRight.y, 0 ) /(2*GameSettings.PixelsPerUnit) );
                float3 size = new float3(topRight-bottomLeft + new int2(1,1), GameSettings.PixelsPerUnit)/ (GameSettings.PixelsPerUnit);
                BoxGeometry newBox = new BoxGeometry
                {
                    Center = center,
                    Size = size,
                    Orientation = quaternion.identity
                };
            
            
                BlobAssetReference<Unity.Physics.Collider> col =
                    Unity.Physics.BoxCollider.Create( newBox, CollisionFilter.Default, Unity.Physics.Material.Default );
                _colliderMap.Add( key, col );
            }
        }
        
    }
}


[BurstCompile]
public struct CreateCollidersJob :IJob
{
    [ReadOnly] public NativeHashMap<int2, BlobAssetReference<Collider>> ColliderMap;
    public EntityCommandBuffer ECB;
    public NativeArray<MeshStrip> geometry;
    public PhysicsWorldSingleton PhysicsWorld;
    public Entity e;
    public RaycastHit Hit;
    public float PPU;
    public void Execute( )
    {
        int count = geometry.Length;

        NativeArray<CompoundCollider.ColliderBlobInstance> childCols 
            = new NativeArray<CompoundCollider.ColliderBlobInstance>(count, Allocator.Temp);
        NativeList<BlobAssetReference<Unity.Physics.Collider>> colsMade = new NativeList<BlobAssetReference<Unity.Physics.Collider>>(count, Allocator.Temp);
        int counter = 0;
        foreach ( MeshStrip strip in geometry )
        {
            
            int2 bottomLeft =  strip.Start;
            int2 topRight = strip.End;
            
            float3 center = (new float3(bottomLeft.x + topRight.x, bottomLeft.y + topRight.y, 0 ) /(2*PPU) );
            //float3 position = (new float3(bottomLeft.x , bottomLeft.y , 0 ) / PPU );
            
            float3 size = new float3(topRight-bottomLeft + new int2(1,1), PPU)/ (PPU);
            
            BoxGeometry newBox = new BoxGeometry
            {
                Center = center,
                Size = size,
                Orientation = quaternion.identity
            };
            
            
            BlobAssetReference<Unity.Physics.Collider> col =
                Unity.Physics.BoxCollider.Create( newBox, CollisionFilter.Default, Unity.Physics.Material.Default );
            colsMade.Add( col );
            
            
            //BlobAssetReference<Unity.Physics.Collider> col = ColliderMap[topRight - bottomLeft];
            CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
            {
                Collider = col,
                Entity = e,
                CompoundFromChild = new RigidTransform
                {
                    rot = quaternion.identity,
                    pos = float3.zero
                    //pos = position
                }
            };
            
            childCols[counter] = newChild;

            counter++;
        }
        
        //ECB.SetComponentEnabled( e, typeof(OldCollider), true );
        ECB.SetComponentEnabled( e, ComponentType.ReadWrite<OldCollider>(), true );
        ECB.SetComponent( e, new OldCollider{Value = PhysicsWorld.Bodies[Hit.RigidBodyIndex].Collider} );

        PhysicsCollider physicsCollider = new PhysicsCollider
        {
            Value = CompoundCollider.Create( childCols )
        };
        ECB.SetComponent( e, new DestructibleCleanUp{Value = physicsCollider} );
        ECB.SetComponent( e, physicsCollider );
        
        foreach ( BlobAssetReference<Unity.Physics.Collider> col in colsMade )
        {
            col.Dispose();
        }

    }
}



[BurstCompile]
public struct MakeColliderStripsJob : IJobParallelFor
{
    [ReadOnly] public DynamicBuffer<DestructibleData> Data;
    [ReadOnly] public int2 Dimensions;
    

    public NativeParallelMultiHashMap<int, MeshStrip>.ParallelWriter Strips;
    public void Execute( int index )
    {
        int levelIndex =index;

        //makes vertical strips
        bool hasStrip = false;
        int2 stripStart = new int2(0,0);
        for ( int y = 0; y < Dimensions.y; y++ )
        {
            if ( IsSolid( levelIndex ) && !hasStrip )
            {
                stripStart = new int2(index, y);
                hasStrip = true;
            }

            if ( !IsSolid( levelIndex ) && hasStrip )
            {
                MeshStrip newStrip = new MeshStrip
                {
                    Start = stripStart,
                    End = new int2( stripStart.x, y - 1 )
                };
                Strips.Add( index, newStrip );
                hasStrip = false;
            }
            
            levelIndex += Dimensions.x;
        }

        if ( hasStrip )
        {
            MeshStrip newStrip = new MeshStrip
            {
                Start = stripStart,
                End = new int2( stripStart.x, Dimensions.y-1 )
            };
            //Strips.Enqueue(  );
            Strips.Add( index, newStrip );
        }
        
    }

    private bool IsSolid( int index )
    {
        return Data[index].Value > 0;
    }

}

[BurstCompile]
public struct MergeColliderStripsJob : IJobParallelFor
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
public struct DestroyStructureJob : IJob
{
    public DynamicBuffer<DestructibleData> Data;
    public LocalToWorld EntityPosition;
    public ShootInfo Info;
    public NativeReference<WeaponInfo> FiredWeapon;
    public float PPU;
    public int2 Dimensions;
    public void Execute( )
    {
        /*
        float3 relativeHit = Info.Hit.Position - EntityPosition.Position;
        float width = 2; //32/16
        
        
        int hitX = (int)((relativeHit.x / width)*32);
        int hitY = (int)((relativeHit.y / width)*32);
        */

        int x1 = (int) ( ( Info.Start.x / PPU ) * Dimensions.x );
        int y1 = (int) ( ( Info.Start.y / PPU ) * Dimensions.y );
        int x2 = (int)((Info.End.x / PPU) * Dimensions.x);
        int y2 = (int) ( ( Info.End.y / PPU ) * Dimensions.y );
        //int2 start = new int2( (int)((Info.Start.x / PPU) * Dimensions.x), (int)((Info.Start.y / PPU)*Dimensions.y) );
        //int2 end = new int2( (int)((Info.End.x / PPU) * Dimensions.x), (int)((Info.End.y / PPU)*Dimensions.y) );
        
        DestroyLine(  x1, y1, x2, y2 );

        //Debug.Log( $"{start} -> {end}" );

        /*
        for ( int i = 0; i < Data.Length; i++ )
        {
            int x = i % 32;
            int y = i / 32;

            if ( math.distance( new float2( x, y ), new float2( hitX, hitY ) ) < FiredWeapon.Value.DestroyRadius )
            {
                DestructibleData d = Data[i];
                d.Value = 0;
                Data[i] = d;
            }
            
        }
        */
    }

    private void DestroyLine(  int x1, int y1, int x2, int y2 )
    {
        if ( math.abs( x2 - x1 ) > math.abs( y2 - y1 ) )
        {
            DestroyHorizontal(  x1, y1, x2, y2 );
        }
        else
        {
            DestroyVertical( x1, y1, x2, y2 );
        }
    }

    private void DestroyHorizontal(  int x0, int y0, int x1, int y1 )
    {
        
        float3 relativeHit = Info.Hit.Position - EntityPosition.Position;
        float width = 2; //32/16
        
        
        int hitX = (int)((relativeHit.x / width)*32);
        int hitY = (int)((relativeHit.y / width)*32);
        //Debug.Log( $"{hitX}, {hitY} | {x0}, {y0} -> {x1}, {y1}" );
        
        int dx = x1 - x0;
        int dy = y1- y0;

        int xDir = 1;
        if ( dx < 0 )
            xDir = -1;
        dx = math.abs( dx );
        
        int dir = 1;
        if ( dy < 0 )
            dir = -1;
        dy *= dir;


        if ( dx == 0 )
            return;
        
        int y = hitY;
        int p = 2 * dy - dx;
        for ( int x = hitX; x < 32 && x >= 0; x+= xDir )
        {
            
            if ( IsInBounds( x, y ) )
            {
                int index =  x + y* 32;
                DestructibleData d = Data[index];
                d.Value = 0;
                Data[index] = d;
            }
            

            if ( p >= 0 )
            {
                y += dir;
                p -= 2 * dx;
                if ( IsInBounds( x, y ) )
                {
                    int index =  x + y* 32;
                    DestructibleData d = Data[index];
                    d.Value = 0;
                    Data[index] = d;
                }
            }

            p += 2 * dy;

        }
        
        
    }

    private bool IsInBounds( int x, int y )
    {
        if ( (x < 0 || x >= 32) || (y < 0 || y >= 32) )
            return false;

        return true;
    }
    
    private void DestroyVertical( int x0, int y0, int x1, int y1 )
    {
        float3 relativeHit = Info.Hit.Position - EntityPosition.Position;
        float width = 2; //32/16
        
        
        int hitX = (int)((relativeHit.x / width)*32);
        int hitY = (int)((relativeHit.y / width)*32);


        int dx = x1 - x0;
        int dy = y1- y0;

        int yDir = 1;
        if ( dy < 0 )
            yDir = -1;
        dy = math.abs( dy );
        
        int dir = 1;
        if ( dx < 0 )
            dir = -1;
        dx *= dir;

        
        if ( dy != 0 )
        {
            int x = hitX;
            int p = 2 * dy - dx;
            for ( int y = hitY; y < 32 && y >= 0; y+= yDir )
            {
                
                if ( IsInBounds( x, y ) )
                {
                    int index =  x  + y* 32;
                    DestructibleData d = Data[index];
                    d.Value = 0;
                    Data[index] = d;
                }
                

                if ( p >= 0 )
                {
                    x += dir;
                    p -= 2 * dy;
                    if ( IsInBounds( x, y ) )
                    {
                        int index =  x + y* 32;
                        DestructibleData d = Data[index];
                        d.Value = 0;
                        Data[index] = d;
                    }
                }

                p += 2 * dx;

            }
        }
    }
    
}

//[BurstCompile]
public partial struct PlayerShootJob : IJobEntity
{
    public PhysicsWorldSingleton PhysicsWorld;
    public NativeList<ShootInfo>.ParallelWriter Hits;
    public Random RNG;

    public NativeReference<WeaponInfo> FiredWeapon;
    public NativeParallelMultiHashMap<ShootInfo, Entity> EntityHitMap;
    
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };
    
    private void Execute( in LocalTransform transform, in PlayerInputs input, in WeaponInfo weapon )
    {
        FiredWeapon.Value = weapon;
        if ( !input.Shoot || FiredWeapon.Value.Timer > 0.1f )
            return;
        
        
        

        WeaponInfo newWeapon = weapon;
        newWeapon.Timer = weapon.FireRate;//
        FiredWeapon.Value = newWeapon;
        NativeList<ShootInfo> allInfo = new NativeList<ShootInfo>(32, Allocator.Temp);
        
        for ( int i = 0; i < FiredWeapon.Value.BulletsPerShot; i++ )
        {
            if(CastRay( transform, ref allInfo, i ))
            {
                Hits.AddRangeNoResize( allInfo );
            }
        }
        
    }

    private bool CastRay( LocalTransform transform, ref NativeList<ShootInfo> allInfo, int key)
    {
        float spread = (FiredWeapon.Value.WeaponSpread/2) * math.TORADIANS;
        //Debug.Log( RNG.NextFloat(-spread, spread) );
        float3 rayEnd = transform.RotateZ( RNG.NextFloat(-spread,spread) ).Right() * FiredWeapon.Value.Range;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = CastFilter
        };
        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(32, Allocator.Temp);
        NativeHashMap<Entity, RaycastHit> hitMap = new NativeHashMap<Entity, RaycastHit>(32, Allocator.Temp);
        
        Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );
        
        if ( PhysicsWorld.CastRay( rayInput, ref allHits ) )
        {
            //since it is possible to hit the same structure twice, make sure to only take the closest hit
            foreach ( RaycastHit hit in allHits )
            {
                
                float dist = math.distance( rayInput.Start, hit.Position );
                if ( !hitMap.ContainsKey( hit.Entity ) )
                {
                    hitMap.Add( hit.Entity, hit );
                }
                else if(dist < math.distance( rayInput.Start, hitMap[hit.Entity].Position ))
                {
                    hitMap[hit.Entity] = hit;
                }
            }

            NativeArray<RaycastHit> result = hitMap.GetValueArray( Allocator.Temp );
            NativeHashSet<int> sorted = new NativeHashSet<int>(result.Length, Allocator.Temp);
            //sort the hits by distance
            for(int i = 0; i < result.Length; i++)
            {
                int minIndex = -1;
                float min = math.INFINITY;
                for ( int x = 0; x < result.Length; x++ )
                {
                    if ( sorted.Contains( x ) )
                        continue;
                    float dist = math.distance( rayInput.Start, result[x].Position );
                    if ( dist < min )
                    {
                        minIndex = x;
                        min = dist;
                    }
                }
                
                sorted.Add( minIndex );

                ShootInfo newInfo = new ShootInfo
                {
                    Start = rayInput.Start,
                    End = rayInput.End,
                    Hit = result[minIndex],
                    Key = key,
                    Step = result.Length - i - 1
                };
                
                allInfo.Add( newInfo );
                EntityHitMap.Add( newInfo, newInfo.Hit.Entity );

                if ( allInfo.Length >= 10 )
                    return true;
            }
            /*
            foreach ( RaycastHit hit in result )
            {
                allInfo.Add(  new ShootInfo
                {
                    Start = rayInput.Start,
                    End = rayInput.End,
                    Hit = hit
                } );

                if ( allInfo.Length == 10 )
                {
                    break;
                }
            }
            */
            return true;
        }
        
        /*
        //shootInfo = new ShootInfo();
        if ( PhysicsWorld.CastRay( rayInput, out RaycastHit hit ) )
        {
            //Debug.DrawLine( rayInput.Start, hit.Position, Color.red, .2f );
            ShootInfo shootInfo = new ShootInfo
            {
                Start = rayInput.Start,
                End = rayInput.End,
                Hit = hit
            };
            allInfo.Add( shootInfo );
            return true;
        } 
        //Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );
        */

        return false;
    }
}

public struct ShootInfo: IEquatable<ShootInfo>
{
    public float3 Start;
    public float3 End;
    public RaycastHit Hit;
    public int Key;
    public int Step;

    public bool Equals( ShootInfo other )
    {
        return other.Key == Key;
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
    
    
}

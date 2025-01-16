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
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;


//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] works for schedule, not run
//[UpdateAfter(typeof(PhysicsSystemGroup))]

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
        state.RequireForUpdate<GameConfig>();
        state.RequireForUpdate<PrefabLoadResult>();

    }

    public void OnDestroy( ref SystemState state )
    {
        
        foreach ( BlobAssetReference<Collider> col in _colliderMap.GetValueArray( Allocator.Temp ) )
        {
            col.Dispose();
        }
        _colliderMap.Dispose();
    }

    private NativeHashMap<Entity, int> ModifyHitStructures( NativeParallelMultiHashMap<ShootInfo, Entity> entityHitMap, ref SystemState state, WeaponInfo weapon )
    {
        NativeHashMap<Entity, int> modifiedEntities = new NativeHashMap<Entity, int>( entityHitMap.Count(), Allocator.TempJob );
        NativeArray<ShootInfo> shootKeys = entityHitMap.GetKeyArray( Allocator.Temp );
        NativeReference<bool> modified = new NativeReference<bool>(false, Allocator.TempJob);
        
        for ( int i = shootKeys.Length -1; i >= 0; )
        {
            
            ShootInfo shootInfo = shootKeys[i];

            int hits = entityHitMap.CountValuesForKey( shootInfo );
            for ( int x = 0; x < hits; x++ )
            {
                shootInfo = shootKeys[i];
                Entity entity = shootInfo.Hit.Entity;

                StructureInfo structure = state.EntityManager.GetComponentData<StructureInfo>( entity );
                DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( entity );
                LocalToWorld ltw = state.EntityManager.GetComponentData<LocalToWorld>( entity );
                NativeReference<ShootInfo> shootRef = new NativeReference<ShootInfo>(shootInfo, Allocator.TempJob);
                modified.Value = false;
                new DestroyStructureJob
                {
                    Data = data,
                    Structure = structure,
                    EntityPosition = ltw,
                    Info = shootRef,
                    Weapon = weapon,
                    Modified = modified,
                    PPU = GameSettings.PixelsPerUnit,
                    Dimensions = GameSettings.Dimensions
                }.Run();
                
                if ( modified.Value && !modifiedEntities.ContainsKey( entity ) )
                    modifiedEntities.Add( entity, shootInfo.Hit.RigidBodyIndex);

                if ( shootRef.Value.Penetration <= 0 )
                {
                    i -= hits - shootInfo.Step;
                    shootRef.Dispose();
                    break;
                }

                //if this shot still has more structures to run through, pass its penetration onto the next shot
                if ( x < hits - 1 && i != 0 )
                {
                    ShootInfo nextShoot = shootKeys[i - 1];
                    nextShoot.Penetration = shootRef.Value.Penetration;
                    shootKeys[i - 1] = nextShoot;
                }

                i--;
                shootRef.Dispose();
            }
            
            
        }

        //shootKeys.Dispose();

        modified.Dispose();
        return modifiedEntities;
    }


    private NativeParallelMultiHashMap<ShootInfo, Entity> FireWeapon(PhysicsWorldSingleton physicsWorld, LocalTransform transform, WeaponInfo weapon)
    {
        NativeParallelMultiHashMap<ShootInfo, Entity> entityHitMap = new NativeParallelMultiHashMap<ShootInfo, Entity>();
        //NativeParallelMultiHashMap<ShootInfo, Entity> entityHitMap = new NativeParallelMultiHashMap<ShootInfo, Entity>(8*weapon.BulletsPerShot, Allocator.TempJob);

        if ( weapon.IsExplosion )
        {
            entityHitMap = new NativeParallelMultiHashMap<ShootInfo, Entity>(4*360, Allocator.TempJob);
            new ExplosionJob
            {
                EntityHitMap = entityHitMap.AsParallelWriter(),
                PhysicsWorld = physicsWorld,
                RandomSeed = _rng.NextUInt(),
                Transform = transform,
                Weapon = weapon
            }.Schedule( 360, 45 ).Complete();
            
        }
        else
        {
            entityHitMap = new NativeParallelMultiHashMap<ShootInfo, Entity>(8*weapon.BulletsPerShot, Allocator.TempJob);
            new ParallelPlayerShootJob
            {
                EntityHitMap = entityHitMap.AsParallelWriter(),
                PhysicsWorld = physicsWorld,
                RandomSeed = _rng.NextUInt(),
                Transform = transform,
                Weapon = weapon
            }.Schedule( weapon.BulletsPerShot, 1 ).Complete();
        }
        

        return entityHitMap;
    }

    private void ThrowProjectile(ref SystemState state, LocalTransform transform, WeaponInfo weapon, PlayerInputs inputs)
    {
        var config = SystemAPI.GetSingleton<GameConfig>();
        
        var configEntity = SystemAPI.GetSingletonEntity<GameConfig>();
        if (!SystemAPI.HasComponent<PrefabLoadResult>(configEntity))
        {
            return;
        }

        var prefabLoadResult = SystemAPI.GetComponent<PrefabLoadResult>(configEntity);
        var entity = state.EntityManager.Instantiate(prefabLoadResult.PrefabRoot);
#if UNITY_EDITOR
        state.EntityManager.SetName( entity, "Grenade" );
#endif

        //state.EntityManager.Instantiate( config.GrenadeReference )
        float3 upwardForce = new float3( 0, 0, -4 );
        float3 throwHeight = new float3(0,0,-1);
        float3 velocity = ( transform.Right() * weapon.ThrowForce ) + upwardForce;
        state.EntityManager.SetComponentData( entity, new ProjectileInfo
        {
            Velocity = velocity,
            Z = 1, 
            Drag = 1
        } );
        LocalTransform pt = state.EntityManager.GetComponentData<LocalTransform>( entity );
        state.EntityManager.SetComponentData(entity, pt.WithPosition( transform.Position + throwHeight ));
    }
    
    public void OnUpdate( ref SystemState state )
    {
        state.EntityManager.CompleteDependencyBeforeRW<PhysicsWorldSingleton>();
        EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        foreach ( var (transform, fuze, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Fuze>>().WithEntityAccess() )
        {
            fuze.ValueRW.Timer -= SystemAPI.Time.DeltaTime;

            if ( fuze.ValueRW.Timer <= 0 )
            {
                WeaponInfo weaponInfo = new WeaponInfo
                {
                    ExplosionRadius = fuze.ValueRO.ExplosionRadius,
                    Penetration = fuze.ValueRO.Penetration,
                    IsExplosion = true
                };

                NativeParallelMultiHashMap<ShootInfo, Entity> entityHitMap = FireWeapon( physicsWorld, transform.ValueRO, weaponInfo );
                ProcessHits( entityHitMap, ref state, ecb, physicsWorld, weaponInfo );
                entityHitMap.Dispose();
                ecb.DestroyEntity( entity );
            }
            
        }

        foreach ( var (transform, input, weapon, player) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerInputs>, RefRW<WeaponInfo>>().WithEntityAccess())
        {
            weapon.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if ( !input.ValueRO.Shoot || weapon.ValueRW.Timer > 0 )
                continue;
            weapon.ValueRW.Timer = weapon.ValueRW.FireRate;

            if ( weapon.ValueRO.Type == WeaponType.Throwable )
            {
                ThrowProjectile(ref state, transform.ValueRO,  weapon.ValueRO, input.ValueRO);
                return;
            }
            
            
            
            //NativeParallelMultiHashMap<ShootInfo,Entity> entityHitMap = new NativeParallelMultiHashMap<ShootInfo,Entity>(32, Allocator.TempJob);
            NativeParallelMultiHashMap<ShootInfo,Entity> entityHitMap = FireWeapon(physicsWorld, transform.ValueRO, weapon.ValueRO);


            if ( !entityHitMap.IsEmpty )
            {
                NativeHashMap<Entity, int> modifiedEntities = ModifyHitStructures( entityHitMap, ref state, weapon.ValueRO );
                
                foreach ( Entity entity in modifiedEntities.GetKeyArray( Allocator.Temp ) )
                {
                    DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( entity );
               
                    BufferData d = state.EntityManager.GetComponentData<BufferData>( entity );
                    d.SetBuffer(data.Reinterpret<int>().AsNativeArray().ToArray());
                  
                    StructureInfo structure = state.EntityManager.GetComponentData<StructureInfo>( entity );
                    //int dim = 32;
                    int width = structure.Size.x;
                    int loopCount = math.max( width / 4, 1 );
                    NativeParallelMultiHashMap<int, MeshStrip> colStrips = new NativeParallelMultiHashMap<int, MeshStrip>( data.Length, Allocator.TempJob);
                    new MakeColliderStripsJob
                    {
                        Data = data,
                        //Dimensions = new int2(dim, dim),
                        Dimensions = structure.Size,
                        Strips = colStrips.AsParallelWriter()
                    }.Schedule( width, loopCount ).Complete();

                    NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(data.Length, Allocator.TempJob);

                    new MergeColliderStripsJob
                    {
                        MergedStrips = mergedStrips.AsParallelWriter(),
                        Strips = colStrips
                    }.Schedule( width, loopCount ).Complete();

                    NativeArray<MeshStrip> geometry = mergedStrips.GetValueArray( Allocator.Temp );
                    //destroy the structure if there is no geometry
                    if ( geometry.Length == 0 )
                    {
                        ecb.DestroyEntity( entity );
                        colStrips.Dispose();
                        mergedStrips.Dispose();
                        continue;
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
                    ecb.SetComponent( entity, new OldCollider{Value = physicsWorld.Bodies[modifiedEntities[entity]].Collider} );
                    
                    PhysicsCollider physicsCollider = new PhysicsCollider
                    {
                        Value = CompoundCollider.Create( childCols )
                    };
                    ecb.SetComponent( entity, new DestructibleCleanUp{Value = physicsCollider} );
                    ecb.SetComponent( entity, physicsCollider );
                    
                    mergedStrips.Dispose();
                    colStrips.Dispose();
                    
                }

                //shootKeys.Dispose();
                modifiedEntities.Dispose();
                

            }

            entityHitMap.Dispose();
        }    //
    }


    private void ProcessHits(NativeParallelMultiHashMap<ShootInfo,Entity> entityHitMap, ref SystemState state, EntityCommandBuffer ecb, PhysicsWorldSingleton physicsWorld, WeaponInfo weapon)
    {

        NativeHashMap<Entity, int> modifiedEntities = ModifyHitStructures( entityHitMap, ref state, weapon );
        
        foreach ( Entity entity in modifiedEntities.GetKeyArray( Allocator.Temp ) )
        {
            DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( entity );
       
            BufferData d = state.EntityManager.GetComponentData<BufferData>( entity );
            d.SetBuffer(data.Reinterpret<int>().AsNativeArray().ToArray());
          
            StructureInfo structure = state.EntityManager.GetComponentData<StructureInfo>( entity );

            int width = structure.Size.x;
            int loopCount = math.max( width / 4, 1 );
            NativeParallelMultiHashMap<int, MeshStrip> colStrips = new NativeParallelMultiHashMap<int, MeshStrip>( data.Length, Allocator.TempJob);
            new MakeColliderStripsJob
            {
                Data = data,
                Dimensions = structure.Size,
                Strips = colStrips.AsParallelWriter()
            }.Schedule( width, loopCount ).Complete();

            NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(data.Length, Allocator.TempJob);

            new MergeColliderStripsJob
            {
                MergedStrips = mergedStrips.AsParallelWriter(),
                Strips = colStrips
            }.Schedule( width, loopCount ).Complete();

            NativeArray<MeshStrip> geometry = mergedStrips.GetValueArray( Allocator.Temp );
            //destroy the structure if there is no geometry
            if ( geometry.Length == 0 )
            {
                ecb.DestroyEntity( entity );
                colStrips.Dispose();
                mergedStrips.Dispose();
                continue;
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
            ecb.SetComponent( entity, new OldCollider{Value = physicsWorld.Bodies[modifiedEntities[entity]].Collider} );
            
            PhysicsCollider physicsCollider = new PhysicsCollider
            {
                Value = CompoundCollider.Create( childCols )
            };
            ecb.SetComponent( entity, new DestructibleCleanUp{Value = physicsCollider} );
            ecb.SetComponent( entity, physicsCollider );
            
            mergedStrips.Dispose();
            colStrips.Dispose();
            
        }

        //shootKeys.Dispose();
        modifiedEntities.Dispose();
        

        
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
        int levelIndex = index;

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
    [ReadOnly] public StructureInfo Structure;
    public DynamicBuffer<DestructibleData> Data;
    public LocalToWorld EntityPosition;
    public NativeReference<ShootInfo> Info;
    public NativeReference<bool> Modified;

    public WeaponInfo Weapon;
    public float PPU;
    public int2 Dimensions;
    public void Execute( )
    {


        int x0 = (int) ( ( Info.Value.Start.x / PPU ) * Dimensions.x );
        int y0 = (int) ( ( Info.Value.Start.y / PPU ) * Dimensions.y );
        int x1 = (int)((Info.Value.End.x / PPU) * Dimensions.x);
        int y1 = (int) ( ( Info.Value.End.y / PPU ) * Dimensions.y );

        DestroyLine(  x0, y0, x1, y1 );
        
    }

    private void DestroyLine(  int x0, int y0, int x1, int y1 )
    {
        if ( math.abs( x1 - x0 ) > math.abs( y1 - y0 ) )
        {
            DestroyHorizontal(  x0, y0, x1, y1 );
        }
        else
        {
            DestroyVertical( x0, y0, x1, y1 );
        }
    }

    private void DestroyHorizontal(  int x0, int y0, int x1, int y1 )
    {
        ShootInfo curInfo = Info.Value;
        
        //float3 worldPos = new float3(Structure.Bounds.x, Structure.Bounds.y, 0)/PPU;
        float3 relativeHit = Info.Value.Hit.Position - EntityPosition.Position;
        //float3 relativeHit = Info.Value.Hit.Position - worldPos;
        
        
        //float width = 2; //32/16
        float width = Structure.Size.x / PPU;
        float height = Structure.Size.y / PPU;

        
        //int hitX = (int)math.clamp ( math.round((relativeHit.x / width)*32) , 0, 31 );
        //int hitY = (int)math.clamp ( math.round((relativeHit.y / width)*32) , 0, 31 );
        int hitX = (int)math.clamp ( math.round((relativeHit.x / width)*Structure.Size.x) , 0, Structure.Size.x -1 );
        int hitY = (int)math.clamp ( math.round((relativeHit.y / height)*Structure.Size.y) , 0, Structure.Size.y - 1 );

        int dx = x1 - x0;
        int dy = y1- y0;
        
        //Debug.Log( upper + " vs " + (int)((Weapon.Range * (1 - curInfo.Hit.Fraction))*PPU) );
        int xDir = 1;
        if ( dx < 0 )
        {
            xDir = -1;
        }
            
        dx = math.abs( dx );
        
        int dir = 1;
        if ( dy < 0 )
            dir = -1;
        dy *= dir;


        if ( dx == 0 )
            return;

        float range = ( Weapon.IsExplosion ) ? Weapon.ExplosionRadius : Weapon.Range;
        
        int remainingRange = (int)((range * (1 - curInfo.Hit.Fraction))*PPU) ;
        int limit = math.min( remainingRange, Structure.Size.x );
        
        int x = hitX;
        int y = hitY;
        int p = 2 * dy - dx;


        for ( int i = 0; i < limit; i++ )
        {
            if ( IsInBounds( x, y ) )
            {
                //int index =  x + y* 32;
                int index =  x + y* Structure.Size.x;
                DestructibleData d = Data[index];
                if ( d.Value > 0 )
                {
                    Modified.Value = true;
                    d.Value = 0;
                    Data[index] = d;
                    curInfo.Penetration--;
                    if ( curInfo.Penetration <= 0 )
                    {
                        Info.Value = curInfo;
                        return;
                    }
                }
            } 
            

            if ( p >= 0 )
            {
                y += dir;
                p -= 2 * dx;
                if ( IsInBounds( x, y ) )
                {
                    Modified.Value = true;
                    //int index =  x + y* 32;
                    int index =  x + y* Structure.Size.x;
                    DestructibleData d = Data[index];
                    d.Value = 0;
                    Data[index] = d;
                }
            }

            x += xDir;
            p += 2 * dy;
        }
        /*
        for ( int x = hitX; x < upper && x >= 0; x+= xDir )
        {
            
            if ( IsInBounds( x, y ) )
            {
                int index =  x + y* 32;
                DestructibleData d = Data[index];
                if ( d.Value > 0 )
                {
                    d.Value = 0;
                    Data[index] = d;
                    curInfo.Penetration--;
                    if ( curInfo.Penetration <= 0 )
                    {
                        Info.Value = curInfo;
                        return;
                    }
                }
                
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
        */
        
        Info.Value = curInfo;
    }

    
    
    private void DestroyVertical( int x0, int y0, int x1, int y1 )
    {
        ShootInfo curInfo = Info.Value;
        
        //float3 worldPos = new float3(Structure.Bounds.x, Structure.Bounds.y, 0)/PPU;
        float3 relativeHit = Info.Value.Hit.Position - EntityPosition.Position;
        
        
        float width = Structure.Size.x / PPU;
        float height = Structure.Size.y / PPU;
        
        int hitX = (int)math.clamp ( math.round((relativeHit.x / width)*Structure.Size.x) , 0, Structure.Size.x -1 );
        int hitY = (int)math.clamp ( math.round((relativeHit.y / height)*Structure.Size.y) , 0, Structure.Size.y - 1 );

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


        if ( dy == 0 )
            return;
        
        float range = ( Weapon.IsExplosion ) ? Weapon.ExplosionRadius : Weapon.Range;
        int remainingRange = (int)((range * (1 - curInfo.Hit.Fraction))*PPU) ;
        int limit = math.min( remainingRange, Structure.Size.y );
        
        int x = hitX;
        int y = hitY;
        int p = 2 * dx - dy;
        for(int i = 0; i < limit; i++)
        {
            
            if ( IsInBounds( x, y ) )
            {
                int index =  x  + y* Structure.Size.x;
                DestructibleData d = Data[index];
                if ( d.Value > 0 )
                {
                    Modified.Value = true;
                    d.Value = 0;
                    Data[index] = d;
                    curInfo.Penetration--;
                    if ( curInfo.Penetration <= 0 )
                    {
                        Info.Value = curInfo;
                        return;
                    }
                }
            }
            

            if ( p >= 0 )
            {
                x += dir;
                p -= 2 * dy;
                if ( IsInBounds( x, y ) )
                {
                    int index =  x + y* Structure.Size.x;
                    DestructibleData d = Data[index];
                    if ( d.Value > 0 )
                    {
                        Modified.Value = true;
                        d.Value = 0;
                        Data[index] = d;
                    }
                    
                }
            }

            y += yDir;
            p += 2 * dx;
        }
        
        Info.Value = curInfo;
        
    }
    
    private bool IsInBounds( int x, int y )
    {
        if ( (x < 0 || x >= Structure.Size.x) || (y < 0 || y >= Structure.Size.y) )
            return false;

        return true;
    }
    
}

[BurstCompile]
public partial struct PlayerShootJob : IJobEntity
{
    public PhysicsWorldSingleton PhysicsWorld;
    //public NativeList<ShootInfo>.ParallelWriter Hits;
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




        WeaponInfo newWeapon = weapon;
        newWeapon.Timer = weapon.FireRate;//
        FiredWeapon.Value = newWeapon;
        //NativeList<ShootInfo> allInfo = new NativeList<ShootInfo>(32, Allocator.Temp);
        
        for ( int i = 0; i < FiredWeapon.Value.BulletsPerShot; i++ )
        {
            CastRay( transform,  i );
        }
        
    }

    private bool CastRay( LocalTransform transform, int key)
    {
        float spread = (FiredWeapon.Value.WeaponSpread/2) * math.TORADIANS;
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
                int maxIndex = -1;
                float max = -math.INFINITY;
                for ( int x = 0; x < result.Length; x++ )
                {
                    if ( sorted.Contains( x ) )
                        continue;
                    float dist = math.distance( rayInput.Start, result[x].Position );
                    if ( dist > max )
                    {
                        maxIndex = x;
                        max = dist;
                    }
                }
                
                sorted.Add( maxIndex );

                ShootInfo newInfo = new ShootInfo
                {
                    Start = rayInput.Start,
                    End = rayInput.End,
                    Hit = result[maxIndex],
                    Key = key,
                    Step = i
                };//
                

                EntityHitMap.Add( newInfo, newInfo.Hit.Entity );

                /*
                if ( allInfo.Length >= 32 )
                    return true;
                */
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

        return false;
    }
}


[BurstCompile]
public struct ParallelPlayerShootJob : IJobParallelFor
{
    [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
    public uint RandomSeed;
    
    public LocalTransform Transform;
    public WeaponInfo Weapon;

    public NativeParallelMultiHashMap<ShootInfo, Entity>.ParallelWriter EntityHitMap;

    private static readonly int MaxHitCount = 8;
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };
    
    public void Execute( int index )
    {
        CastRay( Transform,  index );
    }
    
     private void CastRay( LocalTransform transform, int key)
    {
        Random RNG = Random.CreateFromIndex( RandomSeed + (uint)key );
        float spread = Weapon.WeaponSpread * math.TORADIANS;
        float3 rayEnd = transform.RotateZ( RNG.NextFloat(-spread,spread) ).Right() * Weapon.Range;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = CastFilter
        };
        
        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>(32, Allocator.Temp);
        
        
        Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );
        
        if ( PhysicsWorld.CastRay( rayInput, ref allHits ) )
        {
            NativeHashMap<Entity, RaycastHit> hitMap = new NativeHashMap<Entity, RaycastHit>(allHits.Length, Allocator.Temp);
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

                if ( result[minIndex].Material.CustomTags == (byte)LevelMaterial.Indestructible )
                {
                    return;
                }
                
                sorted.Add( minIndex );

                ShootInfo newInfo = new ShootInfo
                {
                    Start = rayInput.Start,
                    End = rayInput.End,
                    Hit = result[minIndex],
                    Penetration = Weapon.Penetration,
                    Key = key,
                    //Step = result.Length - i - 1 //since this implies that all hits will be added, this stops working with the early out functionality
                    Step = i
                };
                

                EntityHitMap.Add( newInfo, newInfo.Hit.Entity );

                if ( i + 1 >= MaxHitCount )
                    return;
            }
        }
    }
    
}

[BurstCompile]
public struct ExplosionJob : IJobParallelFor
{
    [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
    public uint RandomSeed;

    public LocalTransform Transform;
    public WeaponInfo Weapon;

    public NativeParallelMultiHashMap<ShootInfo, Entity>.ParallelWriter EntityHitMap;

    private static readonly int MaxHitCount = 4;
    private static readonly float3 XY = new float3(1,1,0);
    private static readonly CollisionFilter CastFilter = new CollisionFilter
    {
        CollidesWith = ~(uint) ( 1 << 6 ),
        BelongsTo = ~(uint) ( 1 << 6 )
    };

    public void Execute( int index )
    {
        

        CastRay( Transform, index );
    }

    private void CastRay( LocalTransform transform, int key )
    {
        float angle =  key * math.TORADIANS;
        float3 rayEnd = transform.RotateZ( angle ).Right() * Weapon.ExplosionRadius;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position * XY,
            End = (transform.Position + rayEnd)*XY,
            Filter = CastFilter
        };
        NativeList<RaycastHit> allHits = new NativeList<RaycastHit>( 32, Allocator.Temp );


        Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );
        if ( PhysicsWorld.CastRay( rayInput, ref allHits ) )
        {
            NativeHashMap<Entity, RaycastHit> hitMap = new NativeHashMap<Entity, RaycastHit>( allHits.Length, Allocator.Temp );
            //since it is possible to hit the same structure twice, make sure to only take the closest hit
            foreach ( RaycastHit hit in allHits )
            {
                float dist = math.distance( rayInput.Start, hit.Position );
                if ( !hitMap.ContainsKey( hit.Entity ) )
                {
                    hitMap.Add( hit.Entity, hit );
                }
                else if ( dist < math.distance( rayInput.Start, hitMap[hit.Entity].Position ) )
                {
                    hitMap[hit.Entity] = hit;
                }
            }

            NativeArray<RaycastHit> firstClosestHits = hitMap.GetValueArray( Allocator.Temp );
            NativeHashSet<int> sorted = new NativeHashSet<int>( firstClosestHits.Length, Allocator.Temp );
            //sort the hits by distance
            for ( int i = 0; i < firstClosestHits.Length; i++ )
            {
                int minIndex = -1;
                float min = math.INFINITY;
                for ( int x = 0; x < firstClosestHits.Length; x++ )
                {
                    if ( sorted.Contains( x ) )
                        continue;
                    float dist = math.distance( rayInput.Start, firstClosestHits[x].Position );
                    if ( dist < min )
                    {
                        minIndex = x;
                        min = dist;
                    }
                }

                if ( firstClosestHits[minIndex].Material.CustomTags == (byte)LevelMaterial.Indestructible )
                {
                    return;
                }
                
                sorted.Add( minIndex );

                ShootInfo newInfo = new ShootInfo
                {
                    Start = rayInput.Start,
                    End = rayInput.End,
                    Hit = firstClosestHits[minIndex],
                    Penetration = Weapon.Penetration,
                    Key = key,
                    Step = i
                }; 

                //result.Add( newInfo );
                EntityHitMap.Add( newInfo, newInfo.Hit.Entity );

                
                if ( i+1 >= MaxHitCount )
                    return;
                
            }
        }
    }
}



public struct ShootInfo: IEquatable<ShootInfo>
{
    public float3 Start;
    public float3 End;
    public RaycastHit Hit;
    public float Penetration;
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

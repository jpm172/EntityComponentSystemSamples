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
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = UnityEngine.Collider;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;


//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] works for schedule, not run
//[UpdateAfter(typeof(PhysicsSystemGroup))]

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast =  true)]
public partial struct PlayerShootingSystem : ISystem
{
    private EntityQuery _playerQuery;
    public void OnCreate( ref SystemState state )
    {
        _playerQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PlayerInputs>().Build(ref state);
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

       NativeList<RaycastHit> hitEntities = new NativeList<RaycastHit>(5, Allocator.TempJob);
       Entity player = _playerQuery.ToEntityArray( Allocator.Temp )[0];
       EntityCommandBuffer ecb = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>()
           .CreateCommandBuffer();
       
       NativeReference<WeaponInfo> firedWeapon = new NativeReference<WeaponInfo>(Allocator.TempJob);
       new PlayerShootJob
       {
           PhysicsWorld = physicsWorld,
           Hits = hitEntities.AsParallelWriter(),
           FiredWeapon = firedWeapon
       }.Run();
       
       
       ecb.SetComponent( player, firedWeapon.Value );
       
       if ( hitEntities.Length > 0 )
        {
            foreach ( RaycastHit hit in hitEntities )
            {
                Entity e = hit.Entity;
                DynamicBuffer<DestructibleData> data = state.EntityManager.GetBuffer<DestructibleData>( e );
                LocalToWorld ltw = state.EntityManager.GetComponentData<LocalToWorld>( e );
                
                new DestroyStructureJob
                {
                    Data = data,
                    EntityPosition = ltw,
                    Hit = hit,
                    FiredWeapon = firedWeapon
                }.Run();

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
                NativeList<BlobAssetReference<Unity.Physics.Collider>> colsMade = new NativeList<BlobAssetReference<Unity.Physics.Collider>>(count, Allocator.Temp);
                int counter = 0;
                foreach ( MeshStrip strip in geometry )
                {
                    
                    int2 bottomLeft =  strip.Start;
                    int2 topRight = strip.End;

                    float3 center = new float3(bottomLeft.x + topRight.x, bottomLeft.y + topRight.y, 0 ) /(2*GameSettings.PixelsPerUnit);
                    float3 size = new float3(topRight-bottomLeft + new int2(1,1), GameSettings.PixelsPerUnit)/ (GameSettings.PixelsPerUnit);
                    BoxGeometry newBox = new BoxGeometry
                    {
                        Center = center,
                        Size = size,
                        Orientation = quaternion.identity
                    };
                    
                    
                    BlobAssetReference<Unity.Physics.Collider> col =
                        Unity.Physics.BoxCollider.Create( newBox, CollisionFilter.Default, Unity.Physics.Material.Default );
                    colsMade.Add( col );
    
                    CompoundCollider.ColliderBlobInstance newChild = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = col,
                        Entity = e,
                        CompoundFromChild = new RigidTransform
                        {
                            rot = quaternion.identity,
                            pos = float3.zero
                        }
                    };
                    
                    childCols[counter] = newChild;

                    counter++;
                }

                //store the old collider in the cleanup component to be disposed later
                //ecb.AppendToBuffer( e, new ColliderBufferElement {Value = physicsWorld.Bodies[hit.RigidBodyIndex].Collider} );
                ecb.SetComponentEnabled( e, typeof(OldCollider), true );//
                ecb.SetComponent( e, new OldCollider{Value = physicsWorld.Bodies[hit.RigidBodyIndex].Collider} );
                

                PhysicsCollider physicsCollider = new PhysicsCollider//
                {
                    Value = CompoundCollider.Create( childCols )
                };
                ecb.SetComponent( e, new DestructibleCleanUp{Value = physicsCollider} );
                ecb.SetComponent( e, physicsCollider );

                foreach ( BlobAssetReference<Unity.Physics.Collider> col in colsMade )
                {
                    col.Dispose();
                }

                mergedStrips.Dispose();
                colStrips.Dispose();
                colsMade.Dispose();
            }
        }
        
       
        hitEntities.Dispose();
        firedWeapon.Dispose();
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
    public RaycastHit Hit;
    public NativeReference<WeaponInfo> FiredWeapon;
    public void Execute( )
    {
        float3 relativeHit = Hit.Position - EntityPosition.Position;
        float width = 2; //32/16
        int hitX = (int)((relativeHit.x / width)*32);
        int hitY = (int)((relativeHit.y / width)*32);

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
    }
}

[BurstCompile]
public partial struct PlayerShootJob : IJobEntity
{
    private static readonly float Range = 30;
    public PhysicsWorldSingleton PhysicsWorld;
    public NativeList<RaycastHit>.ParallelWriter Hits;

    public NativeReference<WeaponInfo> FiredWeapon;
    
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
        //newWeapon.Timer = 1;
        FiredWeapon.Value = newWeapon;
        
        if(CastRay( transform, out RaycastHit hit ))
        {
            Hits.AddNoResize( hit );
        }

    }

    private bool CastRay( LocalTransform transform, out RaycastHit hit)
    {
        float3 rayEnd = transform.Right() * Range;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = CastFilter
        };
        
        
        
        if ( PhysicsWorld.CastRay( rayInput, out hit ) )
        {
            Debug.DrawLine( rayInput.Start, hit.Position, Color.red, .2f );
            return true;
        } 
        Debug.DrawLine( rayInput.Start, rayInput.End, Color.blue, .2f );

        return false;
    }
}

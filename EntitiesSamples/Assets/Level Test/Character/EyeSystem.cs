using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using RaycastHit = Unity.Physics.RaycastHit;


[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct EyeSystem : ISystem
{
    private static readonly CollisionFilter _rayFilter = new CollisionFilter
    {
        CollidesWith = ~(uint)( 1 << 6 ),
        BelongsTo = ~(uint)( 1 << 6 )
    };
    
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }
    
    public void OnUpdate( ref SystemState state )
    {
        
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        /*
        new ClearFogJob()
        {
            PhysicsWorld = physicsWorld,
            e = state.EntityManager
        }.Schedule();
        */
        
        
        foreach (
            var (transformComp, ltwComp, eyeComp, info, entity)
            in SystemAPI.Query<RefRO<LocalTransform>, RefRO<LocalToWorld>, RefRO<EyeComponent>, RefRO<MaterialMeshInfo>>()
                .WithEntityAccess()
        )
        {
            EyeComponent eye = eyeComp.ValueRO;
            LocalTransform transform = transformComp.ValueRO;
            LocalToWorld ltw = ltwComp.ValueRO;

            int stepCount =  (int) math.round(eye.Resolution * eye.FOV);

            //the max amount of vertices is if the raycasting step calls FindEdge() every time, which would double the amount of vertices
            int maxVertexCount = (stepCount + 2)*2;

            NativeArray<Vector3> vertices = new NativeArray<Vector3>(maxVertexCount, Allocator.TempJob);
            NativeArray<int> triangles = new NativeArray<int>((maxVertexCount - 2)*3, Allocator.TempJob);
            NativeReference<int> newLength = new NativeReference<int>(Allocator.TempJob);

            new EyePhyicsQueryJob()
            {
                PhysicsWorld = physicsWorld,
                eye = eye,
                transform = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation ),
                Vertices = vertices,
                Triangles = triangles,
                NewLength = newLength,
                RayFilter = _rayFilter
            }.Run();
            
            
            RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            Mesh curMesh = arr.GetMesh( info.ValueRO );


            curMesh.Clear();
            curMesh.vertices = vertices.Slice(0, newLength.Value).ToArray();
            curMesh.triangles = triangles.Slice(0, newLength.Value*3).ToArray();
            curMesh.RecalculateNormals();

            newLength.Dispose();
            vertices.Dispose();
            triangles.Dispose();

        }
    }
    
    private ViewCastInfo CastRay(LocalTransform transform, float angle, EyeComponent eye, PhysicsWorldSingleton physicsWorld)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;
        
        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = _rayFilter
        };

        
        if ( physicsWorld.CastRay( rayInput, out RaycastHit rayHit ) )
        {
            return new ViewCastInfo(true, rayHit.Position, math.distance( rayHit.Position, transform.Position ), angle );
        } 
        
        
        return new ViewCastInfo(false, rayInput.End, eye.ViewDistance, angle );
    }

    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, LocalTransform t, EyeComponent eye, PhysicsWorldSingleton physicsWorld)
    {
        float minAngle = minViewCast.Angle;
        float maxAngle = maxViewCast.Angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for ( int i = 0; i < eye.ResolveIterations; i++ )
        {
            float angle = ( minAngle + maxAngle ) / 2;
            
            ViewCastInfo viewCast = CastRay( t, angle, eye, physicsWorld );

            bool threshold = math.abs( minViewCast.Distance - viewCast.Distance ) > eye.EdgeDistanceThreshold;
            if ( viewCast.Hit == minViewCast.Hit && !threshold )
            {
                minPoint = viewCast.Position;
                minAngle = angle;
            }
            else
            {
                maxPoint = viewCast.Position;
                maxAngle = angle;
            }
            
        }
        
        return new EdgeInfo(minPoint, maxPoint);
    } 
    
}

[BurstCompile]
public struct EyePhyicsQueryJob : IJob
{
    private static readonly float3 float3Forward = new float3( 1, 0, 0 );
    public EyeComponent eye;
    public LocalTransform transform;
    public PhysicsWorldSingleton PhysicsWorld;
    public CollisionFilter RayFilter;
    
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeReference<int> NewLength;

    public void Execute( )
    {
        int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        float degreesPerStep = eye.FOV / stepCount;

        NativeList<float3> viewPoints = new NativeList<float3>(stepCount+1, Allocator.Temp);
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for ( int i = 0; i <= stepCount; i++ )
        {
            float angle = -( eye.FOV / 2 ) + degreesPerStep * i;
            
            ViewCastInfo viewCast = CastRay(  angle );

            if ( i > 0 )
            {
                bool threshold = math.abs( oldViewCast.Distance - viewCast.Distance ) > eye.EdgeDistanceThreshold;
                if ( oldViewCast.Hit != viewCast.Hit || (oldViewCast.Hit && viewCast.Hit && threshold) )
                {
                    EdgeInfo edge = FindEdge( oldViewCast, viewCast );
                    if ( edge.PointA != Vector3.zero )
                    {
                        viewPoints.Add( edge.PointA );
                    }
                    if ( edge.PointB != Vector3.zero )
                    {
                        viewPoints.Add( edge.PointB );
                    }
                }
            }
            
            oldViewCast = viewCast;
            viewPoints.Add( viewCast.Position ); ;
        }

        int vertexCount = viewPoints.Length + 1;
        NewLength.Value = vertexCount;

        Vertices[0] = Vector3.zero;
        for ( int i = 0; i < vertexCount -1; i++ )
        {
            Vertices[i + 1] = transform.InverseTransformPoint( viewPoints[i] )+ float3Forward *eye.CutAway;

            if ( i < vertexCount - 2 )
            {
                Triangles[i * 3] = i + 2;
                Triangles[i * 3 + 1] = i + 1;
                Triangles[i * 3 + 2] = 0;
            }
        }
        
    }

    private ViewCastInfo CastRay( float angle)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = RayFilter
        };

        
        if ( PhysicsWorld.CastRay( rayInput, out RaycastHit rayHit ) )
        {
            
            return new ViewCastInfo(true, rayHit.Position, math.distance( rayHit.Position, transform.Position ), angle );
        } 
        
        
        return new ViewCastInfo(false, rayInput.End, eye.ViewDistance, angle );
    }
    
    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.Angle;
        float maxAngle = maxViewCast.Angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for ( int i = 0; i < eye.ResolveIterations; i++ )
        {
            float angle = ( minAngle + maxAngle ) / 2;
            
            ViewCastInfo viewCast = CastRay( angle );

            bool threshold = math.abs( minViewCast.Distance - viewCast.Distance ) > eye.EdgeDistanceThreshold;
            if ( viewCast.Hit == minViewCast.Hit && !threshold )
            {
                minPoint = viewCast.Position;
                minAngle = angle;
            }
            else
            {
                maxPoint = viewCast.Position;
                maxAngle = angle;
            }
            
        }
        
        return new EdgeInfo(minPoint, maxPoint);
    } 
    
}

[BurstCompile]
public struct EyePhyicsQueryParallelJob : IJobParallelFor
{
    //private static readonly float3 float3Forward = new float3( 1, 0, 0 );
    [ReadOnly] public EyeComponent eye;
    [ReadOnly] public LocalTransform transform;
    [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
    [ReadOnly] public CollisionFilter RayFilter;
    [ReadOnly] public float DegreesPerStep;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> Vertices;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> Triangles;

    public void Execute( int index )
    {
        //int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        //float degreesPerStep = eye.FOV / stepCount;
        
        float angle = -( eye.FOV / 2 ) + DegreesPerStep * index;
        ViewCastInfo viewCast = CastRay(  angle );
        
        if(index == 0)
            Vertices[0] = Vector3.zero;

        int i = index;
        Vertices[i + 1] = transform.InverseTransformPoint( viewCast.Position ); //+ float3Forward *eye.CutAway;
        
                
        if ( i < Vertices.Length - 2 )
        {
            Triangles[i * 3] = i + 2;
            Triangles[i * 3 + 1] = i + 1;
            Triangles[i * 3 + 2] = 0;
        }

    }

    private ViewCastInfo CastRay( float angle)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;

        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = RayFilter
        };

        
        if ( PhysicsWorld.CastRay( rayInput, out RaycastHit rayHit ) )
        {
            return new ViewCastInfo(true, rayHit.Position, math.distance( rayHit.Position, transform.Position ), angle );
        } 
        
        
        return new ViewCastInfo(false, rayInput.End, eye.ViewDistance, angle );
    }
}
public struct ViewCastInfo
{
    public bool Hit;
    public float3 Position;
    public float Distance;
    public float Angle;

    public ViewCastInfo( bool hit, float3 position, float distance, float angle )
    {
        Hit = hit;
        Position = position;
        Distance = distance;
        Angle = angle;
    }
}

public struct EdgeInfo
{
    public Vector3 PointA;
    public Vector3 PointB;

    public EdgeInfo( Vector3 pointA, Vector3 pointB )
    {
        PointA = pointA;
        PointB = pointB;
    }
}

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
            in SystemAPI.Query<RefRO<LocalTransform>, RefRO<LocalToWorld>, RefRW<EyeComponent>, RefRO<MaterialMeshInfo>>()
                .WithEntityAccess()
        )
        {

            EyeComponent eye = eyeComp.ValueRW;
            LocalTransform transform = transformComp.ValueRO;
            LocalToWorld ltw = ltwComp.ValueRO;

            
            

            int stepCount =  (int) math.round(eye.Resolution * eye.FOV);


            LocalTransform t = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation );

            
            
            int vertexCount = stepCount + 2;
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
            NativeArray<int> triangles = new NativeArray<int>((vertexCount - 2)*3, Allocator.TempJob);

            
            new EyePhyicsQueryJob()
            {
                PhysicsWorld = physicsWorld,
                eye = eye,
                transform = t,
                Vertices = vertices,
                Triangles = triangles,
                RayFilter = _rayFilter
            }.Run();
            
            /*
            new EyePhyicsQueryParallelJob()
            {
                PhysicsWorld = physicsWorld,
                eye = eye,
                transform = t,
                Vertices = vertices,
                Triangles = triangles,
                RayFilter = _rayFilter
            }.Schedule( stepCount+1, 8 ).Complete();
            */

            /*
            float degreesPerStep = eye.FOV / stepCount;
            //cast rays
            List<float3> viewPoints = new List<float3>();
            ViewCastInfo oldViewCast = new ViewCastInfo();
            for ( int i = 0; i <= stepCount; i++ )
            {
                float angle = -( eye.FOV / 2 ) + (degreesPerStep * i);
                ViewCastInfo viewCast = CastRay( t, angle, eye, physicsWorld );

                if ( i > 0 )
                {
                    bool threshold = math.abs( oldViewCast.Distance - viewCast.Distance ) > eye.EdgeDistanceThreshold;
                    if ( oldViewCast.Hit != viewCast.Hit || (oldViewCast.Hit && viewCast.Hit && threshold) )
                    //if ( oldViewCast.Hit != viewCast.Hit  )
                    {
                        EdgeInfo edge = FindEdge( oldViewCast, viewCast, t, eye, physicsWorld );
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
                
                
                viewPoints.Add( viewCast.Position );
                oldViewCast = viewCast;
            }
            
            //put ray results into mesh
            int vertexCount = viewPoints.Count + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[( vertexCount - 2 ) * 3];
        
            vertices[0] = Vector3.zero;
            for ( int i = 0; i < vertexCount -1; i++ )
            {
                vertices[i + 1] = t.InverseTransformPoint(viewPoints[i]) + new float3(1,0,0) *eye.CutAway;

                //Debug.DrawLine( vertices[0], vertices[i+1], Color.red, .1f );
                
                if ( i < vertexCount - 2 )
                {
                    triangles[i * 3] = i + 2;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = 0;
                }
            }
            
            //
            */
            RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            Mesh curMesh = arr.GetMesh( info.ValueRO );
            
            
            
            curMesh.Clear();
            curMesh.vertices = vertices.ToArray();
            curMesh.triangles = triangles.ToArray();
            curMesh.RecalculateNormals();
            //curMesh.RecalculateBounds();
            vertices.Dispose();
            triangles.Dispose();
        }
    }
    
    private ViewCastInfo CastRay(LocalTransform transform, float angle, EyeComponent eye, PhysicsWorldSingleton physicsWorld)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;
        //Debug.DrawLine( transform.Position, transform.Position + rayEnd, Color.blue, .1f );
        uint mask = 1 << 6;
        mask = ~mask;
        
        

        CollisionFilter filter = new CollisionFilter
        {
            CollidesWith = mask,
            BelongsTo = mask
        };
        
        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = filter
        };

        
        if ( physicsWorld.CastRay( rayInput, out RaycastHit rayHit ) )
        {
            //Debug.DrawLine( transform.Position, rayHit.Position, Color.blue, .1f );
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
            //if ( viewCast.Hit == minViewCast.Hit  )
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

//[BurstCompile]
public partial struct ClearFogJob : IJobEntity
{

    public PhysicsWorldSingleton PhysicsWorld;

    //private void Execute( ref LocalTransform transform, in EyeComponent eye )
    private void Execute( ref LocalTransform transform, in Entity entity, ref LocalToWorld ltw, in EyeComponent eye )
    {
        //TransformHelpers.ComputeWorldTransformMatrix( e, out float4x4 output, transform,   );
        
        int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        float degreesPerStep = eye.FOV / stepCount;

        LocalTransform t = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation );//convert child transform to world transform, might need to use TransformHelpers.ComputeWorldTransformMatrix
        
        NativeArray<float3> viewPoints = new NativeArray<float3>(stepCount+1, Allocator.Temp);
        for ( int i = 0; i <= stepCount; i++ )
        {
            float angle = -( eye.FOV / 2 ) + degreesPerStep * i;
            
            ViewCastInfo viewCast = CastRay( t, angle, eye );
            viewPoints[i] = viewCast.Position;
        }
/*
        int vertexCount = viewPoints.Length + 1;
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
        NativeArray<int> triangles = new NativeArray<int>((vertexCount - 2)*3, Allocator.Temp);
        
        vertices[0] = Vector3.zero;
        for ( int i = 0; i < vertexCount -1; i++ )
        {
            vertices[i + 1] = viewPoints[i];

            if ( i < vertexCount - 2 )
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        */
    }

    private ViewCastInfo CastRay(LocalTransform transform, float angle, EyeComponent eye)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;
        Debug.DrawLine( transform.Position, transform.Position + rayEnd, Color.blue, .1f );
        uint mask = 1 << 6;
        mask = ~mask;
        
        

        CollisionFilter filter = new CollisionFilter
        {
            CollidesWith = mask,
            BelongsTo = mask
        };
        
        RaycastInput rayInput = new RaycastInput
        {
            Start = transform.Position,
            End = transform.Position + rayEnd,
            Filter = filter
        };

        if ( PhysicsWorld.CastRay( rayInput, out RaycastHit rayHit ) )
        {
            //Debug.DrawLine( transform.Position, rayHit.Position, Color.blue, .1f );
            return new ViewCastInfo(true, rayHit.Position, math.distance( rayHit.Position, transform.Position ), angle );
        } 
        
        return new ViewCastInfo(false, rayInput.End, eye.ViewDistance, angle );
    }
    

    
    
}

[BurstCompile]
public struct EyePhyicsQueryJob : IJob
{
    public EyeComponent eye;
    public LocalTransform transform;
    public PhysicsWorldSingleton PhysicsWorld;
    public CollisionFilter RayFilter;
    
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;

    public void Execute( )
    {
        int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        float degreesPerStep = eye.FOV / stepCount;
        
        //LocalTransform t = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation );//convert child transform to world transform, might need to use TransformHelpers.ComputeWorldTransformMatrix
        
        NativeArray<float3> viewPoints = new NativeArray<float3>(stepCount+1, Allocator.Temp);
        for ( int i = 0; i <= stepCount; i++ )
        {
            float angle = -( eye.FOV / 2 ) + degreesPerStep * i;
            
            ViewCastInfo viewCast = CastRay(  angle );
            viewPoints[i] = viewCast.Position;
        }

        int vertexCount = viewPoints.Length + 1;
        //NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
        //NativeArray<int> triangles = new NativeArray<int>((vertexCount - 2)*3, Allocator.Temp);

        Vertices[0] = Vector3.zero;
        for ( int i = 0; i < vertexCount -1; i++ )
        {
            Vertices[i + 1] = transform.InverseTransformPoint( viewPoints[i] ); //+ new float3(1,0,0) *eye.CutAway;

            //Debug.DrawLine( vertices[0], vertices[i+1], Color.red, .1f );
                
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
        //Debug.DrawLine( transform.Position, transform.Position + rayEnd, Color.blue, .1f );
        uint mask = 1 << 6;
        mask = ~mask;

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
    
    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, LocalTransform t, EyeComponent eye, PhysicsWorldSingleton physicsWorld)
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
                //if ( viewCast.Hit == minViewCast.Hit  )
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
    [ReadOnly] public EyeComponent eye;
    [ReadOnly] public LocalTransform transform;
    [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
    [ReadOnly] public CollisionFilter RayFilter;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> Vertices;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> Triangles;

    public void Execute( int index )
    {
        int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
        float degreesPerStep = eye.FOV / stepCount;
        
        //LocalTransform t = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation );//convert child transform to world transform, might need to use TransformHelpers.ComputeWorldTransformMatrix
        
        //NativeArray<float3> viewPoints = new NativeArray<float3>(stepCount+1, Allocator.Temp);
        //for ( int i = 0; i <= stepCount; i++ )
        //{
            float angle = -( eye.FOV / 2 ) + degreesPerStep * index;
            
            ViewCastInfo viewCast = CastRay(  angle );
            //viewPoints[index] = viewCast.Position;
        //}

        int vertexCount = stepCount+1 + 1;
        //NativeArray<Vector3> vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
        //NativeArray<int> triangles = new NativeArray<int>((vertexCount - 2)*3, Allocator.Temp);

        if(index == 0)
            Vertices[0] = Vector3.zero;

        int i = index;
        //for ( int i = 0; i < vertexCount -1; i++ )
        //{
            Vertices[i + 1] = transform.InverseTransformPoint( viewCast.Position ); //+ new float3(1,0,0) *eye.CutAway;

            //Debug.DrawLine( vertices[0], vertices[i+1], Color.red, .1f );
                
            if ( i < vertexCount - 2 )
            {
                Triangles[i * 3] = i + 2;
                Triangles[i * 3 + 1] = i + 1;
                Triangles[i * 3 + 2] = 0;
            }
        //}
        
    }

    private ViewCastInfo CastRay( float angle)
    {
        float3 rayEnd = transform.RotateZ( angle * math.TORADIANS ).Right() * eye.ViewDistance;
        //Debug.DrawLine( transform.Position, transform.Position + rayEnd, Color.blue, .1f );
        uint mask = 1 << 6;
        mask = ~mask;

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

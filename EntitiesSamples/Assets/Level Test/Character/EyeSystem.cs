using System.Collections;
using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;
using RaycastHit = Unity.Physics.RaycastHit;


[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EyeSystem : ISystem
{
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
            
            /*
            if ( !eye.Initialized )
            {
                
                InitializeEye(ref state, entity, info.ValueRO);
                //Debug.Log( info.ValueRO.MeshID.value );
                eyeComp.ValueRW.Initialized = true;
            }
            */
            
            
            
            int stepCount =  (int) math.round(eye.Resolution * eye.FOV);
            float degreesPerStep = eye.FOV / stepCount;

            LocalTransform t = transform.WithPosition( ltw.Position ).WithRotation( ltw.Rotation );

            //cast rays
            List<float3> viewPoints = new List<float3>();
            for ( int i = 0; i <= stepCount; i++ )
            {
                float angle = -( eye.FOV / 2 ) + degreesPerStep * i;
                ViewCastInfo viewCast = CastRay( t, angle, eye, physicsWorld );
                viewPoints.Add( viewCast.Position );
            }
            
            //put ray results into mesh
            int vertexCount = viewPoints.Count + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[( vertexCount - 2 ) * 3];
        
            vertices[0] = Vector3.zero;
            for ( int i = 0; i < vertexCount -1; i++ )
            {
                vertices[i + 1] = t.InverseTransformPoint(viewPoints[i]);

                Debug.DrawLine( vertices[0], vertices[i+1], Color.red, .1f );
                
                if ( i < vertexCount - 2 )
                {
                    triangles[i * 3] = i + 2;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = 0;
                }
            }
            
            //RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            //
            
            RenderMeshArray arr = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            Mesh curMesh = arr.GetMesh( info.ValueRO );
            
            

            /*
            float width = 5;
            float height = 5;
            
            Vector3[] testVerts = new Vector3[4]
            {
                new Vector3(-width, -height, 0),
                new Vector3(width, -height, 0),
                new Vector3(-width, height, 0),
                new Vector3(width, height, 0)
            };
            
            

            curMesh.SetVertices( testVerts );
            curMesh.RecalculateBounds();
            */
            
            
            curMesh.Clear();
            curMesh.vertices = vertices;
            curMesh.triangles = triangles;
            curMesh.RecalculateNormals();
            curMesh.RecalculateBounds();

            /*
            state.EntityManager.SetComponentData( entity, new RenderBounds
            {
                Value = curMesh.bounds.ToAABB()
            });
            */

        }
    }

    private BatchMeshID InitializeEye(ref SystemState state, Entity entity, MaterialMeshInfo info)
    { 
        Mesh emptyMesh = new Mesh();
        emptyMesh.name = "fuck";
        
        EntitiesGraphicsSystem graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        BatchMeshID id = graphicsSystem.RegisterMesh( emptyMesh );

        state.EntityManager.SetComponentData( entity, new MaterialMeshInfo
        {
            MeshID = id
        } );
        
        return id;
        Mesh[] meshArr = new[] {emptyMesh};
        Material[] matArr = new Material[] {new Material(Shader.Find( "Universal Render Pipeline/Lit" ))};
        
        var renderMeshArray = new RenderMeshArray(matArr, meshArr);
        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = RenderFilterSettings.Default,
            LightProbeUsage = LightProbeUsage.Off,
        };
        
        RenderMeshUtility.AddComponents(
            entity,
            state.EntityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        

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
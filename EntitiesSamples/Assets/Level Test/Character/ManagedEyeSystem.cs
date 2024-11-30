using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

//Registers the meshes and materials needed for the EyeSystem when a new eye is added
//Since registering meshes/materials is a structural change, we only want to do this once per eye
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ManagedEyeSystem : SystemBase
{

    private Material _stencilMat;
    private Material _debugMat;
    private static readonly int Radius = Shader.PropertyToID( "_Radius" );
    private static readonly int Hardness = Shader.PropertyToID( "_Hardness" );
    private static readonly int Strength = Shader.PropertyToID( "_Strength" );
    private static readonly int Center = Shader.PropertyToID( "_Center" );

    protected override void OnCreate()
    {//
        _stencilMat = new Material( Shader.Find( "Universal Render Pipeline/Custom/DotsCutOutFade" ) );
        //_debugMat = new Material(  Shader.Find( "Universal Render Pipeline/Unlit" ) );
        RequireForUpdate<InitializeTag>();
        
    }
    protected override void OnUpdate()
    {
        //
        Entities.WithStructuralChanges().ForEach(
                (ref EyeComponent eye, ref InitializeTag init, in Entity entity, in LocalTransform transform, in LocalToWorld ltw ) =>
                {
                    // Load a mesh from assets
                        var highlightMesh = new Mesh();
                        highlightMesh.MarkDynamic();
                        highlightMesh.name = "Eye Stencil Mesh";

                        
                        // Create a RenderMeshDescription with basic values
                        var renderMeshDescription = new RenderMeshDescription(ShadowCastingMode.Off, false, MotionVectorGenerationMode.Camera, 7);
                        //var renderMeshDescription = new RenderMeshDescription(ShadowCastingMode.Off, false );

                        
                        //set up shader
                        Material newMat = new Material( _stencilMat );
                        newMat.SetFloat(Radius, eye.ViewDistance);
                        newMat.SetFloat(Hardness, eye.Hardness);
                        newMat.SetFloat(Strength, eye.Strength);
                        newMat.SetVector(Center, new Vector4(ltw.Position.x,ltw.Position.y ));
                        
                        // Create a RenderMeshArray with the required mesh and material
                        var renderMeshArray = new RenderMeshArray(new[] { newMat  }, new[] { highlightMesh });

                        
                        /*
                        unsafe
                        {
                            var handle = GCHandle.Alloc(renderMeshArray.Materials[0], GCHandleType.Pinned);
                            var handle2 = GCHandle.Alloc(renderMeshArray.Materials[0], GCHandleType.Pinned);
                            Vector3* ptr = (Vector3*) handle.AddrOfPinnedObject().ToPointer();
                        }
                        */
                        
                        
                        // Create a MaterialMeshInfo instance which maps the first material and mesh from RenderMeshArray
                        var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

                        // Add the rendering Sharedcomponents using the helper class RenderMeshUtility
                        RenderMeshUtility.AddComponents(entity, EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

                        // Add RenderBounds
                        EntityManager.AddComponentData(entity, new RenderBounds() { Value = { Center = highlightMesh.bounds.center, Extents = highlightMesh.bounds.extents } });
                        EntityManager.RemoveComponent<InitializeTag>( entity );
                        
                        /*
                        unsafe
                        {
                            var handle = GCHandle.Alloc(renderMeshArray.Meshes[0].vertices, GCHandleType.Pinned);
                            Vector3* ptr = (Vector3*) handle.AddrOfPinnedObject().ToPointer();
                            handle.Free();
                        }
                        */
                        /*
                        NativeArray<int> testArr = new NativeArray<int>(5, Allocator.TempJob);
                        testArr[0] = 99;
                        unsafe
                        { 
                            int* pointer = (int*)NativeArrayUnsafeUtility.GetUnsafePtr( testArr );
                            *pointer = 5;
                        }
                        testArr.Dispose();
                        */
                        
                }
            )
            .Run();
    }
}

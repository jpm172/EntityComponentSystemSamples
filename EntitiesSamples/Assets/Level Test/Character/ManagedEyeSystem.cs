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

public partial class ManagedEyeSystem : SystemBase
{

    private Material _stencilMat;
    private Material _debugMat;
    private List<BlobAssetReference<EyeComponent>> blobs;
    protected override void OnCreate()
    {//
        blobs = new List<BlobAssetReference<EyeComponent>>();
        //_stencilMat = new Material( Shader.Find( "Universal Render Pipeline/Custom/DotsStencil" ) );
        _stencilMat = new Material( Shader.Find( "Universal Render Pipeline/Custom/DotsCutOutFade" ) );
        _debugMat = new Material(  Shader.Find( "Universal Render Pipeline/Unlit" ) );
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

                        Material newMat = new Material( _stencilMat );
                        //set up shader
                        /*
                        _stencilMat.SetFloat("_Radius", eye.ViewDistance);
                        _stencilMat.SetFloat("_Hardness", eye.Hardness);
                        _stencilMat.SetFloat("_Strength", eye.Strength);
                        _stencilMat.SetVector("_Center", new Vector4(ltw.Position.x,ltw.Position.y ));
                        */
                        newMat.SetFloat("_Radius", eye.ViewDistance);
                        newMat.SetFloat("_Hardness", eye.Hardness);
                        newMat.SetFloat("_Strength", eye.Strength);
                        newMat.SetVector("_Center", new Vector4(ltw.Position.x,ltw.Position.y ));
                        
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
    

    /*
    private void GetPointer(ref EyeComponent eye)
    {
        Vector3[] arr = new Vector3[1];
        // Create a new builder that will use temporary memory to construct the blob asset
        BlobBuilder builder = new BlobBuilder(Allocator.Temp);

        // Construct the root object for the blob asset. Notice the use of `ref`.
        ref EyeComponent marketData = ref builder.ConstructRoot<EyeComponent>();

        // Now fill the constructed root with the data:
        // Apples compare to Oranges in the universally accepted ratio of 2 : 1 .
        builder.SetPointer(ref eye.Pointer, ref arr[0]);

        // Now copy the data from the builder into its final place, which will
        // use the persistent allocator
        var result = builder.CreateBlobAssetReference<EyeComponent>(Allocator.Persistent);

        blobs.Add( result );
        // Make sure to dispose the builder itself so all internal memory is disposed.
        builder.Dispose();
    }
    */
    
    protected override void OnDestroy()
    {
        foreach ( BlobAssetReference<EyeComponent> b in blobs )
        {
            b.Dispose();
        }
    }
}

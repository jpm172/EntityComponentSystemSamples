using System.Collections;
using System.Collections.Generic;
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
    protected override void OnCreate()
    {
        _stencilMat = new Material( Shader.Find( "Universal Render Pipeline/Custom/DotsStencil" ) );
        _debugMat = new Material(  Shader.Find( "Universal Render Pipeline/Lit" ) );
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
                        highlightMesh.name = "Eye Stencil Mesh";

                        // Create a RenderMeshDescription with basic values
                        var renderMeshDescription = new RenderMeshDescription(ShadowCastingMode.Off, false);

                        // Create a RenderMeshArray with the required mesh and material
                        var renderMeshArray = new RenderMeshArray(new[] { _debugMat  }, new[] { highlightMesh });

                        // Create a MaterialMeshInfo instance which maps the first material and mesh from RenderMeshArray
                        var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

                        // Add the rendering Sharedcomponents using the helper class RenderMeshUtility
                        RenderMeshUtility.AddComponents(entity, EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

                        // Add RenderBounds
                        EntityManager.AddComponentData(entity, new RenderBounds() { Value = { Center = highlightMesh.bounds.center, Extents = highlightMesh.bounds.extents } });
                        EntityManager.RemoveComponent<InitializeTag>( entity );
                    
                }
            )
            .Run();
        

    }
    
   
}

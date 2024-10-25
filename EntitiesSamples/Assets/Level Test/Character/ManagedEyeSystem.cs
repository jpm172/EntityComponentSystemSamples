using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public partial class ManagedEyeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        
        //
        Entities.WithStructuralChanges().ForEach(
                (ref EyeComponent eye, in Entity entity, in LocalTransform transform, in LocalToWorld ltw ) =>
                {
                    if ( !eye.Initialized )
                    {
                        Debug.Log( "init" );
                        eye.Initialized = true;
                        // Load a mesh from assets
                        var highlightMesh = new Mesh();
                        highlightMesh.name = "Empty Mesh";

                        float width = 1;
                        float height = 1;
                        Vector3[] vertices = new Vector3[4]
                        {
                            new Vector3(0, 0, 0),
                            new Vector3(width, 0, 0),
                            new Vector3(0, height, 0),
                            new Vector3(width, height, 0)
                        };
                        highlightMesh.vertices = vertices;
                        int[] tris = new int[6]
                        {
                            // lower left triangle
                            0, 2, 1,
                            // upper right triangle
                            2, 3, 1
                        };
                        highlightMesh.triangles = tris;
                        Vector3[] normals = new Vector3[4]
                        {
                            -Vector3.forward,
                            -Vector3.forward,
                            -Vector3.forward,
                            -Vector3.forward
                        };
                        highlightMesh.normals = normals;

                        Vector2[] uv = new Vector2[4]
                        {
                            new Vector2(0, 0),
                            new Vector2(1, 0),
                            new Vector2(0, 1),
                            new Vector2(1, 1)
                        };
                        highlightMesh.uv = uv;
                        //highlightMesh.RecalculateBounds();
                        
                        // Create a RenderMeshDescription with basic values
                        var renderMeshDescription = new RenderMeshDescription(ShadowCastingMode.Off, false);

                        // Create a RenderMeshArray with the required mesh and material
                        var renderMeshArray = new RenderMeshArray(new[] { new Material( Shader.Find( "Universal Render Pipeline/Custom/DotsStencil" ) ),  }, new[] { highlightMesh });

                        // Create a MaterialMeshInfo instance which maps the first material and mesh from RenderMeshArray
                        var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

                        // Add the rendering Sharedcomponents using the helper class RenderMeshUtility
                        RenderMeshUtility.AddComponents(entity, EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

                        // Add RenderBounds
                        EntityManager.AddComponentData(entity, new RenderBounds() { Value = { Center = highlightMesh.bounds.center, Extents = highlightMesh.bounds.extents*5 } });
                    }
                    
                    
                    
                    
                }
            )
            .Run();

        Entities.WithStructuralChanges().ForEach(
            ( ref EyeComponent eye, in MaterialMeshInfo info, in Entity entity, in LocalTransform transform,
                in LocalToWorld ltw ) =>
            {
                /*
                    RenderMeshArray arr = EntityManager.GetSharedComponentManaged<RenderMeshArray>( entity );
                    Mesh curMesh = arr.GetMesh( info );
                    
                    float width = 5;
                    float height = 5;
                    
                    Vector3[] vertices = new Vector3[4]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(width, 0, 0),
                        new Vector3(0, height, 0),
                        new Vector3(width, height, 0)
                    };
                    */

                //curMesh.SetVertices( vertices );
                //curMesh.RecalculateBounds();
                
            } ).Run();
        //.ScheduleParallel();

    }
    
   
}

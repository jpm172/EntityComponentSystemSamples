using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Makes an optimized mesh by preventing duplicate vertices being added when adding quads to the mesh
/// </summary>
public class OptimizedMeshConstructor
{
    Dictionary<Vector3, int> vertexDict;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    
    private List<Vector2> _uvs;
    private List<Vector3> _normals;

    
    
    public OptimizedMeshConstructor()
    {
        vertexDict = new Dictionary<Vector3, int>();
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
    }
    
    
    public Mesh ConstructMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.normals = _normals.ToArray();
        mesh.uv = _uvs.ToArray();
        
        mesh.Optimize();

        return mesh;
    }
    
    
    
    //Adds three new vertices and makes a triangle out of them
    public void AddMeshSection(VertexData vertexA, VertexData vertexB, VertexData vertexC)
    {
        Vector3 normal = VertexUtility.ComputeNormal(vertexA, vertexB, vertexC);
        
        vertexA.Normal = normal;
        vertexB.Normal = normal;
        vertexC.Normal = normal;
            
        int indexA = AddVertex(vertexA);
        int indexB = AddVertex(vertexB);
        int indexC = AddVertex(vertexC);
        
        AddTriangle(indexA, indexB, indexC);
        
    }
    
    private void AddTriangle(int indexA, int indexB, int indexC)
    {
        _triangles.Add(indexA);
        _triangles.Add(indexB);
        _triangles.Add(indexC);
    }

    private int AddVertex(VertexData vertex)
    {
        Vector3 vertexPosition = vertex.Postion;
        if ( vertexDict.ContainsKey( vertexPosition ) )
        {
            return vertexDict[vertexPosition];
        }
        
        
        
        _vertices.Add(vertex.Postion);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        vertexDict.Add( vertexPosition, _vertices.Count - 1 );
        
        return _vertices.Count - 1;
    }
    
    
}

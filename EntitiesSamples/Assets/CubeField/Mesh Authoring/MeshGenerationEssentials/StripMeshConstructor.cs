using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class StripMeshConstructor 
{
    Dictionary<Vector3, int> vertexDict;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    
    private List<Vector2> _uvs;
    private List<Vector3> _normals;


    public StripMeshConstructor()
    {
        vertexDict = new Dictionary<Vector3, int>();
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
    }
    
    public Mesh ConstructMesh( NativeArray<int> levelLayout, Vector2Int dimensions, LevelRoom room )
    {
        NativeParallelMultiHashMap<int, MeshStrip> stripMap = new NativeParallelMultiHashMap<int, MeshStrip>(room.Size.x*room.Size.y, Allocator.TempJob);

        MakeMeshStripsJob stripJob = new MakeMeshStripsJob
        {
            LevelLayout = levelLayout,
            LevelDimensions = dimensions,
            RoomId = room.Id,
            RoomOrigin = room.Origin,
            RoomSize = room.Size,
            Strips = stripMap.AsParallelWriter()
        };
            
        JobHandle applyHandle = stripJob.Schedule(room.Size.x, 16);
        applyHandle.Complete();
        
        NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(room.Size.x*room.Size.y, Allocator.TempJob);
            
        MergeMeshStripsJob mergeJob = new MergeMeshStripsJob
        {
            Strips = stripMap,
            MergedStrips = mergedStrips.AsParallelWriter()
        };
        
        JobHandle mergeHandle = mergeJob.Schedule( room.Size.x, 8 );
        mergeHandle.Complete();
        
        StripsToMesh( mergedStrips, room );
        
        stripMap.Dispose();
        mergedStrips.Dispose();
        
        return FinishMesh();
    }


    private Mesh FinishMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.normals = _normals.ToArray();
        mesh.uv = _uvs.ToArray();

        return mesh;
    }
    
    private void StripsToMesh(NativeParallelMultiHashMap<int, MeshStrip> mergedStrips, LevelRoom room)
    {

        //Debug.Log( strips.Count );
        NativeArray<int> keys = mergedStrips.GetKeyArray( Allocator.TempJob );


        foreach ( int key in keys )
        {
            NativeParallelMultiHashMap<int, MeshStrip>.Enumerator values = mergedStrips.GetValuesForKey( key );
            while ( values.MoveNext() )
            {
                MeshStrip strip = values.Current;
                int2 bottomLeft = room.Origin + strip.Start;
                int2 topRight = room.Origin + strip.End;
                float2[] floorPoints =
                {
                    new float2(bottomLeft.x-.5f, bottomLeft.y-.5f), //bottom left
                    new float2(topRight.x+.5f, bottomLeft.y-.5f), //bottom right
                    new float2(topRight.x+.5f, topRight.y+.5f), //top right
                    new float2(bottomLeft.x-.5f, topRight.y+.5f), //top left
                };
    
                VertexData[] vertices = 
                {
                    CoordinatesToVertex(floorPoints[0].x, floorPoints[0].y), 
                    CoordinatesToVertex(floorPoints[1].x, floorPoints[1].y), 
                    CoordinatesToVertex(floorPoints[2].x, floorPoints[2].y),
                    CoordinatesToVertex(floorPoints[3].x, floorPoints[3].y), 
                };
                AddMeshSection(vertices[0],  vertices[2], vertices[1]);
                AddMeshSection(vertices[0], vertices[3], vertices[2]);
            }
        }

        keys.Dispose();


    }
    
    
    private VertexData CoordinatesToVertex(float x, float y)
    {
        //offset the UVs by .5 to counteract the bottomLeft vector
        float uvX = (x+.5f) / GameSettings.PixelsPerUnit;
        float uvY = (y+.5f) / GameSettings.PixelsPerUnit;
        
        Vector3 position = new Vector3(x, y, 0) / GameSettings.PixelsPerUnit;
        return new VertexData()
        {
            Postion = position,
            Uv = new Vector2(uvX, uvY),
            Normal = Vector3.forward
        };
    }
    
    private void AddMeshSection(VertexData vertexA, VertexData vertexB, VertexData vertexC)
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

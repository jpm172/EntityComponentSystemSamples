using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class StripMeshConstructor 
{
    Dictionary<Vector3, int> vertexDict;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    
    private List<Vector2> _uvs;
    private List<Vector3> _normals;
    private List<BoxGeometry> _collisionQuads;


    public List<BoxGeometry> CollisionQuads => _collisionQuads;

    public StripMeshConstructor()
    {
        vertexDict = new Dictionary<Vector3, int>();
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
        _collisionQuads = new List<BoxGeometry>();
    }
    
    public Mesh ConstructMesh( NativeArray<int> levelLayout, int2 dimensions, LevelRoom room, int targetId )
    {
        NativeParallelMultiHashMap<int, MeshStrip> stripMap = new NativeParallelMultiHashMap<int, MeshStrip>(room.Size.x*room.Size.y, Allocator.TempJob);

        MakeMeshStripsJob stripJob = new MakeMeshStripsJob
        {
            LevelLayout = levelLayout,
            LevelDimensions = dimensions,
            TargetId = targetId,
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
        
        StripsToMesh( mergedStrips, room.Origin );
        
        
        stripMap.Dispose();
        mergedStrips.Dispose();
        
        return FinishMesh();
    }

    public Mesh ConstructMesh( NativeArray<int> pointField, int binSize, int2 blockOrigin )
    {
        NativeParallelMultiHashMap<int, MeshStrip> stripMap = new NativeParallelMultiHashMap<int, MeshStrip>(binSize*binSize, Allocator.TempJob);

        MakeMeshStripsPointFieldJob stripJob = new MakeMeshStripsPointFieldJob
        {
            PointField = pointField,
            BinSize = binSize,
            Strips = stripMap.AsParallelWriter()
        };
            
        JobHandle applyHandle = stripJob.Schedule(binSize, 16);
        applyHandle.Complete();
        
        NativeParallelMultiHashMap<int, MeshStrip> mergedStrips = new NativeParallelMultiHashMap<int, MeshStrip>(binSize*binSize, Allocator.TempJob);
            
        MergeMeshStripsJob mergeJob = new MergeMeshStripsJob
        {
            Strips = stripMap,
            MergedStrips = mergedStrips.AsParallelWriter()
        };
        
        JobHandle mergeHandle = mergeJob.Schedule( binSize, 8 );
        mergeHandle.Complete();
        
        StripsToMesh( mergedStrips, blockOrigin );
        
        
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
    
    private void StripsToMesh(NativeParallelMultiHashMap<int, MeshStrip> mergedStrips, int2 origin)
    {
        NativeArray<int> keys = mergedStrips.GetKeyArray( Allocator.TempJob );


        foreach ( int key in keys )
        {
            NativeParallelMultiHashMap<int, MeshStrip>.Enumerator values = mergedStrips.GetValuesForKey( key );
            while ( values.MoveNext() )
            {
                MeshStrip strip = values.Current;
                int2 bottomLeft = origin + strip.Start;
                int2 topRight = origin + strip.End;
                float2[] floorPoints =
                {
                    new float2(bottomLeft.x-.5f, bottomLeft.y-.5f), //bottom left
                    new float2(topRight.x+.5f, bottomLeft.y-.5f), //bottom right
                    new float2(topRight.x+.5f, topRight.y+.5f), //top right
                    new float2(bottomLeft.x-.5f, topRight.y+.5f), //top left
                };
    
                AddNewBoxCollider( bottomLeft, topRight );
                
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

    private void AddNewBoxCollider( int2 bottomLeft, int2 topRight )
    {
        float thickness = 1 * GameSettings.PixelsPerUnit;
        
        float3 boxCenter = new float3(bottomLeft + topRight,0 );
        boxCenter /= (2*GameSettings.PixelsPerUnit);

        float3 size = new float3(topRight-bottomLeft + new int2(1,1), thickness)/ (GameSettings.PixelsPerUnit);
        
        BoxGeometry newBox = new BoxGeometry
        {
            Center = boxCenter,
            Size = size,
            Orientation = quaternion.identity
        };
        
        _collisionQuads.Add( newBox );
    }
    
    
    private VertexData CoordinatesToVertex(float x, float y)
    {
        //offset the UVs by .5 to counteract the bottomLeft vector
        //float uvX = (x+.5f) / GameSettings.PixelsPerUnit;
        //float uvY = (y+.5f) / GameSettings.PixelsPerUnit;
        
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;
using UnityEngine.Rendering;

public class MeshContructionHelper
{
    //All the properites we need to contruct a mesh in the end
    private List<Vector3> _vertices;
    private List<int> _triangles;
    
    private List<Vector2> _uvs;
    private List<Vector3> _normals;

    private int removed = 0;
    private Mesh currentMesh;
    

    public Mesh CurrentMesh => currentMesh;


    private VertexAttributeDescriptor[] layout = new[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
    };
    
    
    public MeshContructionHelper()
    {
        removed = 0;
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
    }
        
    //This function will actually produce a mesh
    //How exiting!
    public Mesh ConstructMesh()
    {
        Mesh mesh = new Mesh();

        
        //if # of vertices is larger than the buffer, mesh will break
        //mesh.indexFormat = IndexFormat.UInt32;
        
        mesh.vertices = _vertices.ToArray();
        mesh.triangles = _triangles.ToArray();
        mesh.normals = _normals.ToArray();
        mesh.uv = _uvs.ToArray();
        
        mesh.Optimize();
        //Debug.Log(  _vertices.Count );
        currentMesh = mesh;
        currentMesh.MarkDynamic();
        
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


    public void RemoveMeshSection(Vector3[] verticesToRemove)
    {
        List<int> indices = new List<int>();
        int startIndex = IndexOfVertex( verticesToRemove[0], 0 );//_vertices.IndexOf( verticesToRemove[0] );
        while ( startIndex != -1 )
        {
            indices.Add( startIndex );
            startIndex = IndexOfVertex( verticesToRemove[0], startIndex + 1 );
        }

        int result = -1;
        foreach ( int i in indices )
        {
            //Debug.Log( i );
            bool foundMatch = true;
            for ( int n = 1; n < 6; n++ )
            {
                if ( _vertices[i + n] != verticesToRemove[n] )
                {
                    foundMatch = false;
                }
            }

            if ( foundMatch )
            {
                result = i;
                break;
            }
        }
        
        
        
        
        
        _triangles.RemoveRange( result, 6 );
        _vertices.RemoveRange( result, 6 );
        _uvs.RemoveRange( result, 6 );
        _normals.RemoveRange( result, 6 );
        

        //float startTime = Time.realtimeSinceStartup;
        /*
        for ( int i = result; i < _triangles.Count; i++ )
        {
            _triangles[i] -= 6;
        }
        */
        
        //Debug.Log( "Mesh: " + (Time.realtimeSinceStartup - startTime)*1000f + " ms" );

    }

    /// <summary>
    /// Returns the index of the first occurence of the target vertex within the mesh's vertices
    /// Slightly optimised as it will move 6 steps at a time since that is the structure of the quads
    /// Shaved ~50ms off the test case
    /// </summary>
    /// <param name="item">The vertex to search for</param>
    /// <param name="index">What index to begin the search from</param>
    /// <returns></returns>
    private int IndexOfVertex( Vector3 item, int index )
    {
        for ( int i = index; i < _vertices.Count; i+=6 )
        {
            if ( _vertices[i] == item )
            {
                return i;
            }
        }

        return -1;
    }
    

    private void AddTriangle(int indexA, int indexB, int indexC)
    {
        _triangles.Add(indexA);
        _triangles.Add(indexB);
        _triangles.Add(indexC);
    }

    private int AddVertex(VertexData vertex)
    {
        _vertices.Add(vertex.Postion);
        _uvs.Add(vertex.Uv);
        _normals.Add(vertex.Normal);
        return _vertices.Count - 1;
    }
    
    
    //float startTime = Time.realtimeSinceStartup;
    //Debug.Log( "Main done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );
    
    
    public void RemoveMeshSections( Vector3[] verticesToRemove, VertexData[] verticesToAdd  )
    {
        
        
        
        int removeCount = verticesToRemove.Length / 6;
        
        //Debug.Log( "Removing " + removeCount + " and adding " + (verticesToAdd.Length/6) + " quads" );
        
        using ( Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData( currentMesh ) )
        {
            //load all the data needed for the jobs
            Mesh.MeshData data = dataArray[0];

            NativeArray<int> resultIndex = new NativeArray<int>(removeCount, Allocator.TempJob);
            NativeArray<Vector3> oldVertices = new NativeArray<Vector3>(currentMesh.vertexCount, Allocator.TempJob);
            NativeArray<Vector2> oldUvs = new NativeArray<Vector2>(currentMesh.vertexCount, Allocator.TempJob);
            NativeArray<Vector3> targetVertices = new NativeArray<Vector3>(verticesToRemove, Allocator.TempJob);
            NativeArray<VertexData> addedVertices = new NativeArray<VertexData>(verticesToAdd, Allocator.TempJob);
            
            data.GetVertices( oldVertices );
            data.GetUVs( 0, oldUvs );


            //do the first step, find all of the starting indices of the vertices we need to remove
            GetIndicesJob newJob = new GetIndicesJob
            {
                vertices = oldVertices,
                targetVertices = targetVertices,
                result = resultIndex
            };
            
            JobHandle jobHandle = newJob.Schedule(removeCount, 1);
            jobHandle.Complete();


            int newVertexCount = oldVertices.Length - verticesToRemove.Length + verticesToAdd.Length;
            //Debug.Log( "Equal in Size? " + (newVertexCount == _vertices.Count  ));
            
            //Create the mesh minus the removed vertices
            Mesh.MeshDataArray newMeshArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData newMeshData = newMeshArray[0];
            
            newMeshData.subMeshCount = 1;
            newMeshData.SetVertexBufferParams( newVertexCount, layout );
            
            
            NativeArray<VertexData> newMeshVertices = newMeshData.GetVertexData<VertexData>();

            //use merge sort to have average case of nlog(n)
            sort(resultIndex, 0, resultIndex.Length - 1);
            


            int lengthBeforeAdding = newMeshVertices.Length - verticesToAdd.Length;
            RemoveIndicesJob newRemoveJob = new RemoveIndicesJob
            {
                removedIndices = resultIndex,
                oldVertices = oldVertices,
                oldUvs = oldUvs,
                newVertices = newMeshVertices,
                addedVertices = addedVertices,
                addedVerticesStart = lengthBeforeAdding
                
            };

            JobHandle removeJobHandle = newRemoveJob.Schedule(newVertexCount, 16);
            removeJobHandle.Complete();

            
            newMeshData.SetIndexBufferParams(newVertexCount, IndexFormat.UInt16);
            var ib = newMeshData.GetIndexData<ushort>();
            for (ushort i = 0; i < ib.Length; ++i)
                ib[i] = i;
            
            Debug.Log( newVertexCount );
            
           
            //apply mesh and dispose of job info
            newMeshData.SetSubMesh(0, new SubMeshDescriptor(0, ib.Length));
            
            
            Mesh.ApplyAndDisposeWritableMeshData(newMeshArray, currentMesh);
            currentMesh.RecalculateNormals();
            currentMesh.RecalculateBounds();


            oldVertices.Dispose();
            resultIndex.Dispose();
            targetVertices.Dispose();
            addedVertices.Dispose();
            oldUvs.Dispose();
            

        }
        
        //Debug.Log( "Threads done: " +  (Time.realtimeSinceStartup - startTime)*1000f + " ms" );

    }

    
    
    private void merge(NativeArray<int> arr, int l, int m, int r)
    {
        // Find sizes of two
        // subarrays to be merged
        int n1 = m - l + 1;
        int n2 = r - m;

        // Create temp arrays
        int[] L = new int[n1];
        int[] R = new int[n2];
        int i, j;

        // Copy data to temp arrays
        for (i = 0; i < n1; ++i)
            L[i] = arr[l + i];
        for (j = 0; j < n2; ++j)
            R[j] = arr[m + 1 + j];

        // Merge the temp arrays

        // Initial indexes of first
        // and second subarrays
        i = 0;
        j = 0;

        // Initial index of merged
        // subarray array
        int k = l;
        while (i < n1 && j < n2) {
            if (L[i] <= R[j]) {
                arr[k] = L[i];
                i++;
            }
            else {
                arr[k] = R[j];
                j++;
            }
            k++;
        }

        // Copy remaining elements
        // of L[] if any
        while (i < n1) {
            arr[k] = L[i];
            i++;
            k++;
        }

        // Copy remaining elements
        // of R[] if any
        while (j < n2) {
            arr[k] = R[j];
            j++;
            k++;
        }
    }

    // Main function that
    // sorts arr[l..r] using
    // merge()
    void sort(NativeArray<int> arr, int l, int r)
    {
        if (l < r) {

            // Find the middle point
            int m = l + (r - l) / 2;

            // Sort first and second halves
            sort(arr, l, m);
            sort(arr, m + 1, r);

            // Merge the sorted halves
            merge(arr, l, m, r);
        }
    }
    
    
    
    
    //private struct Vertex
    
}





[BurstCompile]
public struct RemoveIndicesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vector3> oldVertices;

    [ReadOnly] 
    public NativeArray<Vector2> oldUvs;
    
    [ReadOnly]
    public NativeArray<VertexData> addedVertices;
    
    public NativeArray<VertexData> newVertices;
    //public NativeArray<Vector2> newUvs;
    
    [ReadOnly]
    public NativeArray<int> removedIndices;

    
    [ReadOnly] public int addedVerticesStart;
    
    
    public void Execute( int index )
    {
        
       
        if ( index >= addedVerticesStart )
        {
            newVertices[index] = addedVertices[index - addedVerticesStart];
            return;
        }
        
        int offset = GetOffset( index );
        
        VertexData newStruct = new VertexData()
        {
            Postion = oldVertices[index + offset],
            Uv = oldUvs[index + offset],
            Normal = Vector3.forward
        };
        newVertices[index] = newStruct;

        //newVertices[index] = new oldVertices[index + offset];
    }

    private int GetOffset( int index )
    {
        int offset = 0;
        int largest = 0;
        for ( int i = 0; i < removedIndices.Length; i++ )
        {
            if ( index + offset >= removedIndices[i] )
            {
                offset += 6;
                largest = removedIndices[i];
            }
            else if ( i > 0 && i < removedIndices.Length-1 && index + 6 >= removedIndices[i] )
            {
                offset += 6;
                largest = removedIndices[i];
            }
            else if ( largest == removedIndices[i] - 6 )
            {
                offset += 6;
                largest = removedIndices[i];
            }
            else
                return offset;
        }
        //Debug.Log( offset );
        return offset;
    }
}


[BurstCompile]
public struct GetIndicesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vector3> vertices;

    public NativeArray<int> result;
    
    [ReadOnly]
    public NativeArray<Vector3> targetVertices;
    
    
    public void Execute( int index )
    {
        Vector3 bottomLeft = targetVertices[index * 6];
        Vector3 topRight = targetVertices[(index * 6)+5];
        for ( int i = 0; i < vertices.Length; i+=6 )
        {
             
            //if ( vertices[i] == initVertex && IsMatch( i, index * 6 ) )
            //only really need to check if the bottom left and top right match, since those vertices cant both be shared with 
            //a neighboring quad
            if ( vertices[i] == bottomLeft && vertices[i+5]  == topRight)
            {
                result[index] = i;
                return;
            }
        }
    }
    
    
    
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// Makes A mesh that is a minimal amount of tris 
/// </summary>
public class MinimalMeshConstructor
{
    Dictionary<Vector3, int> vertexDict;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    
    private List<Vector2> _uvs;
    private List<Vector3> _normals;
    
    
    
    public MinimalMeshConstructor()
    {
        vertexDict = new Dictionary<Vector3, int>();
        _triangles = new List<int>();
        _vertices = new List<Vector3>();
        _uvs = new List<Vector2>();
        _normals = new List<Vector3>();
    }


    public Mesh ConstructMesh(Vector2Int dimensions, Vector2Int blockOrigin, int blockSize,  int[] pointField )
    {

        if ( dimensions.x > dimensions.y )
        {
            Dictionary<int, List < MeshStrip >> strips = MakeHorizontalStrips( blockOrigin, blockSize, pointField );
            BlobStripsToMesh( JoinStrips( strips ) );
        }
        else
        {
            Dictionary<int, List < MeshStrip >> strips = MakeVerticalStrips(  blockOrigin, blockSize, pointField );
            BlobStripsToMesh( JoinStrips( strips ) );
        }

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


    private void BlobStripsToMesh( List<MeshStrip> strips )
    {
        //Debug.Log( strips.Count );
        
        foreach ( MeshStrip strip in strips )
        {
            Vector2 bottomLeft = strip.BottomLeft;
            Vector2 topRight = strip.TopRight;
            Vector2[] floorPoints =
            {
                new Vector2(bottomLeft.x, bottomLeft.y), //bottom left
                new Vector2(topRight.x, bottomLeft.y), //bottom right
                new Vector2(topRight.x, topRight.y), //top right
                new Vector2(bottomLeft.x, topRight.y), //top left
            };
        
            VertexData[] vertices = 
            {
                BlobImageCoordinatesToVertex(floorPoints[0].x, floorPoints[0].y), 
                BlobImageCoordinatesToVertex(floorPoints[1].x, floorPoints[1].y), 
                BlobImageCoordinatesToVertex(floorPoints[2].x, floorPoints[2].y),
                BlobImageCoordinatesToVertex(floorPoints[3].x, floorPoints[3].y), 
            };
            AddMeshSection(vertices[0],  vertices[2], vertices[1]);
            AddMeshSection(vertices[0], vertices[3], vertices[2]);
        }
    }
    
    private void StripsToMesh( List<MeshStrip> strips)
    {
        //Debug.Log( strips.Count );
        
        foreach ( MeshStrip strip in strips )
        {
            Vector2 bottomLeft = strip.BottomLeft;
            Vector2 topRight = strip.TopRight;
            Vector2[] floorPoints =
            {
                new Vector2(bottomLeft.x, bottomLeft.y), //bottom left
                new Vector2(topRight.x, bottomLeft.y), //bottom right
                new Vector2(topRight.x, topRight.y), //top right
                new Vector2(bottomLeft.x, topRight.y), //top left
            };
        
            VertexData[] vertices = 
            {
                ImageCoordinatesToVertex(floorPoints[0].x, floorPoints[0].y), 
                ImageCoordinatesToVertex(floorPoints[1].x, floorPoints[1].y), 
                ImageCoordinatesToVertex(floorPoints[2].x, floorPoints[2].y),
                ImageCoordinatesToVertex(floorPoints[3].x, floorPoints[3].y), 
            };
            AddMeshSection(vertices[0],  vertices[2], vertices[1]);
            AddMeshSection(vertices[0], vertices[3], vertices[2]);
        }
    }
    
    
    private Dictionary<int, List < MeshStrip >> MakeHorizontalStrips( Vector2Int blockOrigin, int blockSize, int[] pointField)
    {
        Dictionary<int, List < MeshStrip >> stripDict = new Dictionary<int, List<MeshStrip>>();

        for ( int y = 0; y < blockSize; y++ )
        {
            stripDict[y] = new List<MeshStrip>();
            bool hasStrip = false;
            Vector2Int stripStart = new Vector2Int(0,0);
            Vector2Int stripEnd = new Vector2Int(0,0);
            
            for ( int x = 0; x < blockSize; x++ )
            {
                if ( pointField[x + y*blockSize] > 0)
                {
                    if ( !hasStrip )
                    {
                        stripStart =  new Vector2Int( blockOrigin.x + x, blockOrigin.y +y);
                        stripEnd =  new Vector2Int(blockOrigin.x + x, blockOrigin.y +y);
                        hasStrip = true;
                    }
                    else
                    {
                        stripEnd =  new Vector2Int(blockOrigin.x + x, blockOrigin.y +y);
                    }
                }
                else if ( hasStrip )
                {
                    stripDict[y].Add( new MeshStrip(stripStart, stripEnd) );
                    hasStrip = false;
                }
            }
            
            if(hasStrip)
                stripDict[y].Add( new MeshStrip(stripStart, stripEnd) );
            
        }
        
        return stripDict;
    }
    
    private Dictionary<int, List < MeshStrip >> MakeVerticalStrips( Vector2Int blockOrigin, int blockSize, int[] pointField)
    {
        Dictionary<int, List < MeshStrip >> stripDict = new Dictionary<int, List<MeshStrip>>();

        for ( int x = 0; x < blockSize; x++ )
        {
            stripDict[x] = new List<MeshStrip>();
            bool hasStrip = false;
            Vector2Int stripStart = new Vector2Int(0,0);
            Vector2Int stripEnd = new Vector2Int(0,0);
            
            for ( int y = 0; y < blockSize; y++ )
            {
                if ( pointField[x + y*blockSize] > 0)
                {
                    if ( !hasStrip )
                    {
                        stripStart =  new Vector2Int( blockOrigin.x + x, blockOrigin.y +y);
                        stripEnd =  new Vector2Int(blockOrigin.x + x, blockOrigin.y +y);
                        hasStrip = true;
                    }
                    else
                    {
                        stripEnd =  new Vector2Int(blockOrigin.x + x, blockOrigin.y +y);
                    }
                }
                else if ( hasStrip )
                {
                    stripDict[x].Add( new MeshStrip(stripStart, stripEnd) );
                    hasStrip = false;
                }
            }

            if ( hasStrip )
            {
                stripDict[x].Add( new MeshStrip(stripStart, stripEnd) );
            }
        }
        
        return stripDict;
    }
    

    private List<MeshStrip> JoinStrips(Dictionary<int, List < MeshStrip >> stripDict)
    {
        int[] rows = stripDict.Keys.ToArray();


        List<MeshStrip> result = new List<MeshStrip>();
        
        for(int index = 0; index < rows.Length-1; index++ )
        {
            int row = rows[index];
            foreach ( MeshStrip strip in stripDict[row] )
            {
                int checkRow = rows[index+1];

                
                for(int i = 0; i < stripDict[checkRow].Count; i++)
                {
                    MeshStrip checkStrip = stripDict[checkRow][i];
                    if ( strip.CanConnect( checkStrip ) )
                    {
                        strip.Connect( checkStrip );
                        stripDict[checkRow][i] = strip;
                    }
                }
                
                if(!result.Contains( strip ))
                    result.Add( strip );
            }
        }

        int lastRow = rows[^1];
        foreach ( MeshStrip strip in stripDict[lastRow] )
        {
            if(!result.Contains( strip ))
                result.Add( strip );
        }

        return result;
    }

    
    //Adds three new vertices and makes a triangle out of them
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
    
    
    private VertexData ImageCoordinatesToVertex(float x, float y)
    {
        float uvX = x;
        float uvY = y;
        Vector3 position = new Vector3(x, y, 0);
        return new VertexData()
        {
            Postion = position,
            Uv = new Vector2(uvX, uvY),
            Normal = Vector3.forward
        };
    }
    
    private VertexData BlobImageCoordinatesToVertex(float x, float y)
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
    
    private class MeshStrip
    {
        private Vector2Int _start;
        private Vector2Int _end;
        private Vector2 _bottomLeft;
        private Vector2 _topRight;
        
        private bool _isHorizontal;
        

        public int Length => math.max( _end.x - _start.x, _end.y - _start.y ) + 1;

        public Vector2 BottomLeft => _bottomLeft;

        public Vector2 TopRight => _topRight;

        public MeshStrip(Vector2Int start, Vector2Int end)
        {
            _start = start;
            _end = end;

            _bottomLeft = new Vector2(start.x -.5f, start.y - .5f);
            _topRight = new Vector2(end.x +.5f, end.y + .5f);
            
            
            _isHorizontal = _start.y == _end.y;
        }

        public void Connect( MeshStrip strip )
        {
            if ( _isHorizontal )
            {
                if ( strip._start.y < _start.y )
                {
                    _bottomLeft = strip._bottomLeft;
                    _start = strip._start;
                }
                else
                {
                    _topRight = strip._topRight;
                    _end = strip._end;
                }
            }
            else
            {
                if ( strip._start.x < _start.x )
                {
                    _bottomLeft = strip._bottomLeft;
                    _start = strip._start;
                }
                else
                {
                    _topRight = strip._topRight;
                    _end = strip._end;
                }
            }
            
        }
        
        public bool CanConnect( MeshStrip strip )
        {
            if ( strip.Equals( this ) )
                return false;

            if ( _isHorizontal )
            {
                bool sameX = ( _start.x == strip._start.x ) && ( _end.x == strip._end.x );
                int xDist = math.min( math.abs( _start.y - strip._end.y ), math.abs( _end.y - strip._start.y ) );
                return sameX && xDist == 1;
            }
                
            
            
            bool sameY = ( _start.y == strip._start.y ) && ( _end.y == strip._end.y );
            int yDist = math.min( math.abs( _start.x - strip._end.x ), math.abs( _end.x - strip._start.x ) );
            return sameY && yDist == 1;
        }

        public override string ToString()
        {
            return $"{_start} -> {_end}";
        }

        public override bool Equals( object obj )
        {
            if ( obj == null )
                return false;

            MeshStrip stripObj = (MeshStrip) obj;

            return ( stripObj._start == _start && stripObj._end == _end );
        }
    }
    
}


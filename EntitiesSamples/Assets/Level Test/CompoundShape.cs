using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class CompoundShape
{

    private List<int4> _shapes;
    private List<int4> _emptySpace;
    private int4 _bounds;

    private bool _nullShape;

    public bool NullShape => _nullShape;

    public CompoundShape()
    {
        _nullShape = true;
        _shapes = new List<int4>();
        _emptySpace = new List<int4>();
    }
    

    public void CreateShape()
    {
        for ( int i = 0; i < _shapes.Count; i++ )
        {
            for ( int j = 0; j < _emptySpace.Count; j++ )
            {
                if ( ApplyBoolean( i, _emptySpace[j] ) )
                {
                    j = -1;//next iteration j will be set back to zero
                    if(i >= _shapes.Count)
                        break;
                }
            }
        }


        if ( _shapes.Count <= 0 )
        {
            _nullShape = true;
            return;
        }
        
        _nullShape = false;
        _bounds = _shapes[0];
        for ( int i = 1; i < _shapes.Count; i++ )
        {
            _bounds.xy = math.min( _bounds.xy, _shapes[i].xy );
            _bounds.zw = math.max( _bounds.zw, _shapes[i].zw );
        }

        /*
        for ( int i = 0; i < _shapes.Count; i++ )
        {
            for ( int j = 0; j < _shapes.Count; j++ )
            {
                if(i == j)
                    continue;

                if ( _shapes[i].Overlaps( _shapes[j] ) )
                {
                    Debug.Log( "overlaps" );
                }
            }   
        }
        */

    }
    
    public void AddShape( int4 newShape )
    {
        _shapes.Add( newShape );

        _bounds.xy = math.min( newShape.xy, newShape.xy );
        _bounds.zw = math.max( newShape.zw, newShape.zw );
    }

    public void AddShape( int4 newShape, int4 newEmpty )
    {
        _shapes.Add( newShape );
        _emptySpace.Add( newEmpty );
        ApplyBoolean( _shapes.Count-1, newEmpty );
    }
    

    public void AddEmptySpace( int4 emptySpace )
    {
        _emptySpace.Add( emptySpace );
    }

    public void AddEmptySpaces( List<int4> emptySpaces )
    {
        _emptySpace.AddRange( emptySpaces );
    }
    
    public void DrawGizmos()
    {
        foreach ( int4 shape in _shapes )
        {
            int2 shapeSize = shape.Size();
            Vector3 size = new Vector3( shapeSize.x, shapeSize.y );
            Vector3 pos = new Vector3(shape.x, shape.y) + size/2;
            pos /= GameSettings.PixelsPerUnit;
            size /= GameSettings.PixelsPerUnit;
            
            Gizmos.color = Color.black;
            Gizmos.DrawCube( pos, size );
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube( pos, size );
        }
    }
    

    //splits the shape at index into 4 parts that will encapsulate otherShape
    //returns true if the shape was completely removed by the boolean operation, and false otherwise
    private bool ApplyBoolean(int index, int4 otherShape)
    {
        int4 shape = _shapes[index];

        if ( !shape.Overlaps( otherShape ) )
            return false;

        int4 overlap = shape;
        overlap.xy = math.max( shape.xy, otherShape.xy );
        overlap.zw = math.min( shape.zw, otherShape.zw );
        

        int4 bottom = new int4(shape.x, shape.y, shape.z, overlap.y-1);
        int4 top = new int4(shape.x, overlap.w+1, shape.z, shape.w);
        int4 left = new int4(shape.x, overlap.y, overlap.x -1, overlap.w);
        int4 right = new int4(overlap.z+1, overlap.y, shape.z, overlap.w);

        
        bool hasChangedIndex = false;
        if ( bottom.Area() > 0 )
        {
            _shapes[index] = bottom;
            hasChangedIndex = true;
        }
        if ( top.Area() > 0 )
        {
            if ( !hasChangedIndex )
            {
                _shapes[index] = top;
                hasChangedIndex = true;
            }
            else
            {
                _shapes.Add( top );
            }
        }
        if ( left.Area() > 0 )
        {
            if ( !hasChangedIndex )
            {
                _shapes[index] = left;
                hasChangedIndex = true;
            }
            else
            {
                _shapes.Add( left );
            }
        }
        if ( right.Area() > 0 )
        {
            if ( !hasChangedIndex )
            {
                _shapes[index] = right;
                hasChangedIndex = true;
            }
            else
            {
                _shapes.Add( right );
            }
        }

        if ( !hasChangedIndex )
        {
            _shapes.RemoveAt( index );
            return true;
        }

        return false;


    }
    
}

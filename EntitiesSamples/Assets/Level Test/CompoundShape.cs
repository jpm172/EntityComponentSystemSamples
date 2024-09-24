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
    
    public CompoundShape()
    {
        _shapes = new List<int4>();
        _emptySpace = new List<int4>();
    }

    public void Initialize( int4 initialShape )
    {
        _shapes.Add( initialShape );
        _bounds = initialShape;
    }

    public void AddShape( int4 newShape )
    {
        _shapes.Add( newShape );
        ApplyEmptySpace( _shapes.Count-1 );
        
        _bounds.xy = math.min( newShape.xy, newShape.xy );
        _bounds.zw = math.max( newShape.zw, newShape.zw );
    }

    private void ApplyEmptySpace(int index)
    {
        foreach ( int4 empty in _emptySpace )
        {
            //todo USE A STACK, and keep queing/poppping
        }
    }

    public void AddEmptySpace( int4 emptySpace )
    {
        _emptySpace.Add( emptySpace );
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

    public void AddBooleanShape( int4 shape, int4 boolean )
    {
        _shapes.Add( shape );
        ApplyBoolean( _shapes.Count-1, boolean );
    }
    
    public void Boolean( int4 otherShape )
    {
        int count = _shapes.Count;
        for ( int i = count - 1; i >= 0; i-- )
        {
            ApplyBoolean(i, otherShape);
        }
    }

    //floor/ceiling corner bias
    private void ApplyBoolean(int index, int4 otherShape)
    {
        int startCount = _shapes.Count;
        int4 shape = _shapes[index];

        if ( !shape.Overlaps( otherShape ) )
            return;

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
        
        if(!hasChangedIndex)
            _shapes.RemoveAt( index );
        

    }
    
}

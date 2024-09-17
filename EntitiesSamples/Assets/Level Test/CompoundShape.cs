using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CompoundShape
{

    private List<IntBounds> _shapes;
    private IntBounds _bounds;
    
    public CompoundShape(IntBounds initialShape)
    {
        _shapes = new List<IntBounds>();
        
        _shapes.Add( initialShape );
        _bounds = initialShape;
    }


    public void AddShape( IntBounds newShape )
    {
        _shapes.Add( newShape );
        _bounds.Bounds.xy = math.min( newShape.Bounds.xy, newShape.Bounds.xy );
        _bounds.Bounds.zw = math.max( newShape.Bounds.zw, newShape.Bounds.zw );
    }
    
    
}

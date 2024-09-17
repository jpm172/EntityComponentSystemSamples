using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MyExtensionMethods 
{
    public static bool Overlaps( this int4 bounds, int4 otherBounds )
    {
        if ( bounds.x > otherBounds.z || bounds.y > otherBounds.w )
            return false;

        if ( bounds.z < otherBounds.x || bounds.w < otherBounds.y )
            return false;

        return true;
    }

    
}

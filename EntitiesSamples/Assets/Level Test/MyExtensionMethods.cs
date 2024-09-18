using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MyExtensionMethods 
{
    public static readonly int2 Int2One = new int2(1,1);
    private static readonly int4 ExpandHorizontal = new int4(-1,0,1,0);
    private static readonly int4 ExpandVertical = new int4(0,1,0,1);

    
    
    
    public static bool Overlaps( this int4 bounds, int4 otherBounds )
    {
        if ( bounds.x > otherBounds.z || bounds.y > otherBounds.w )
            return false;

        if ( bounds.z < otherBounds.x || bounds.w < otherBounds.y )
            return false;

        return true;
    }
    
    
    
    public static int4 Boolean( this int4 bounds, int4 otherBounds )
    {
        int4 result = bounds;
        

        result.xy = math.max( result.xy, otherBounds.xy );
        result.zw = math.min( result.zw, otherBounds.zw );
        /*
        int x_distance = math.min(Bounds.z, otherBounds.Bounds.z) - math.max( Bounds.x, otherBounds.Bounds.x );
        int y_distance = math.min(Bounds.w, otherBounds.Bounds.w) - math.max( Bounds.y, otherBounds.Bounds.y );
        //x_distance = math.min(R1.x, R2.x) – max(L1.x, L2.x)
        //y_distance = min(R1.y, R2.y) – max(L1.y, L2.y)
        */

        return result;
    }

    public static int4 CutOut( this int4 bounds, int4 otherBounds, out int cuts, out int4 cut2 )
    {
        int4 result = bounds;
        int4 result2 = bounds;
        cuts = 1;
        
        int x_distance = math.min(bounds.z, otherBounds.z) - math.max( bounds.x, otherBounds.x );
        int y_distance = math.min(bounds.w, otherBounds.w) - math.max( bounds.y, otherBounds.y );

        //if either distance is negative, then the two bounds do not overlap
        if ( x_distance < 0 || y_distance < 0 )
        {
            cuts = 0;
            cut2 = result2;
            return bounds;
        }

        int4 boolean = result;
        boolean.xy = math.max( result.xy, otherBounds.xy );
        boolean.zw = math.min( result.zw, otherBounds.zw );

        //if the result of the boolean is the same dimensions as the current bounds, then the entire bounds is being cut out
        //return -1 to indicate that it has been completely cut away
        if ( boolean.Equals( result ) )
        {
            cuts = -1;
            cut2 = result2;
            return bounds;
        }

        bool removeA = !result.xy.Equals( boolean.xy ); 
        bool removeB = !result.zw.Equals( boolean.zw );


        //Debug.Log( $"remove above: {removeA}, remove below: {removeB}" );
        //Debug.Log( $"{result} : {boolean}" );
        
        int2 cutA = math.sign (boolean.xy - result.xy);
        int2 cutB = math.sign (boolean.zw - result.zw);

        if ( removeA && removeB )
        {
            result.zw = boolean.xy - cutA;
            result2.xy = boolean.zw - cutB;
            cuts = 2;
            
            cut2 = result2;
            return result;
        }

        if ( removeA )
        {
            result.zw = boolean.xy - cutA;
            cut2 = result2;
            return result;
        }
        else
        {
            result.xy = boolean.zw - cutB;
            cut2 = result2;
            return result;
        }
    }
    
    public static bool Borders( this int4 bounds, int4 otherBounds )
    {

        if ( bounds.Overlaps( otherBounds ) ) 
            return false;
        
        int4 horizontalEdge = bounds + ExpandHorizontal;
        int4 verticalEdge = bounds + ExpandVertical;

        return horizontalEdge.Overlaps( otherBounds ) || verticalEdge.Overlaps( otherBounds );
    }
    
    
    public static int4 Flatten( this int4 bounds, int4 flattenDirection )
    {
        int4 halfMask = math.abs( flattenDirection );//turns the direction into a mask that only keeps the axis that we will flatten against
        int4 fullMask = math.max( halfMask.xyzw, halfMask.zwxy );//creates a mask that will only keep the correct axis, either both x values or both y values (1010 or 0101)
        int4 inverseMask = fullMask.yxwz;
        
        int4 flattenedAxis = bounds * halfMask;
        
        return (fullMask * math.csum( flattenedAxis )) + ( inverseMask * bounds );
    }

    public static int2 Origin( this int4 bounds )
    {
        return bounds.xy;
    }
    public static int2 Size( this int4 bounds )
    {
        return ( bounds.zw - bounds.xy ) + Int2One;
    }
    
}

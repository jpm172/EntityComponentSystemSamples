using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public struct LevelCell
{
    public IntBounds Bounds;
    
    public int CellId;

    public int2 Origin => Bounds.Origin;
    public int2 Size => Bounds.Size;

    public int Area => Bounds.Area;

    public LevelCell( int id, int2 origin, int2 size )
    {
        CellId = id;
        Bounds = new IntBounds(origin, size);
    }
    
    public LevelCell( int id, int4 xyzw )
    {
        CellId = id;
        Bounds = new IntBounds(xyzw);
    }

    public override int GetHashCode()
    {
        return CellId.GetHashCode();
    }

    public override string ToString()
    {
        return $"Cell {CellId}: " + Bounds.ToString();
    }

    public override bool Equals( object obj )
    {
        if ( obj == null )
            return false;

        LevelCell cell = (LevelCell) obj;
        return cell.CellId == CellId;
    }
}

public struct IntBounds
{
    private static readonly int2 Int2One = new int2(1,1);
    private static readonly int4 ExpandHorizontal = new int4(-1,0,1,0);
    private static readonly int4 ExpandVertical = new int4(0,1,0,1);
    public int4 Bounds; //(X, Y, Z, W)


    public int x => Bounds.x;
    public int y => Bounds.y;
    public int z => Bounds.z;
    public int w => Bounds.w;
    
    
    
    public int2 Origin => Bounds.xy;
    public int2 Size => (Bounds.zw - Bounds.xy) + Int2One;

    public int Area => Size.x * Size.y;
    
    public IntBounds( int2 origin, int2 size )
    {
        Bounds = new int4(origin, origin + size - Int2One);
    }
    
    public IntBounds( int4 xyzw )
    {
        Bounds = xyzw;
    }
    
    public bool Overlaps( IntBounds otherBounds)
    {
        int4 other = otherBounds.Bounds;

        if ( Bounds.x > other.z || Bounds.y > other.w )
            return false;

        if ( Bounds.z < other.x || Bounds.w < other.y )
            return false;

        return true;
    }

    
    public bool Overlaps( int4 otherBounds)
    {

        if ( Bounds.x > otherBounds.z || Bounds.y > otherBounds.w )
            return false;

        if ( Bounds.z < otherBounds.x || Bounds.w < otherBounds.y )
            return false;

        return true;
    }
    
    public bool Borders( IntBounds otherBounds )
    {

        if ( Overlaps( otherBounds ) ) 
            return false;
        
        int4 horizontalEdge = Bounds + ExpandHorizontal;
        int4 verticalEdge = Bounds + ExpandVertical;

        return Overlaps( horizontalEdge ) || Overlaps( verticalEdge );
    }

    public bool Contains( IntBounds otherBounds )
    {
        bool x = Bounds.x <= otherBounds.Bounds.x && Bounds.z >= otherBounds.Bounds.z;
        bool y = Bounds.y <= otherBounds.Bounds.y && Bounds.w >= otherBounds.Bounds.w;
        
        return x && y;
    }
    

    public IntBounds CutOut( IntBounds otherBounds, out int cuts, out IntBounds cut2 )
    {
        int4 result = Bounds;
        int4 result2 = Bounds;
        cuts = 1;
        
        int x_distance = math.min(Bounds.z, otherBounds.Bounds.z) - math.max( Bounds.x, otherBounds.Bounds.x );
        int y_distance = math.min(Bounds.w, otherBounds.Bounds.w) - math.max( Bounds.y, otherBounds.Bounds.y );

        //if either distance is negative, then the two bounds do not overlap
        if ( x_distance < 0 || y_distance < 0 )
        {
            cuts = 0;
            cut2 = new IntBounds(result2);
            return this;
        }

        int4 boolean = result;
        boolean.xy = math.max( result.xy, otherBounds.Bounds.xy );
        boolean.zw = math.min( result.zw, otherBounds.Bounds.zw );

        //if the result of the boolean is the same dimensions as the current bounds, then the entire bounds is being cut out
        //return -1 to indicate that it has been completely cut away
        if ( boolean.Equals( result ) )
        {
            cuts = -1;
            cut2 = new IntBounds(result2);
            return this;
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
            
            cut2 = new IntBounds(result2);
            return new IntBounds(result);
        }

        if ( removeA )
        {
            result.zw = boolean.xy - cutA;
            cut2 = new IntBounds(result2);
            return new IntBounds(result);
        }
        else
        {
            result.xy = boolean.zw - cutB;
            cut2 = new IntBounds(result2);
            return new IntBounds(result);
        }
    }

    public IntBounds Boolean( IntBounds otherBounds )
    {
        int4 result = Bounds;
        

        result.xy = math.max( result.xy, otherBounds.Bounds.xy );
        result.zw = math.min( result.zw, otherBounds.Bounds.zw );
        /*
        int x_distance = math.min(Bounds.z, otherBounds.Bounds.z) - math.max( Bounds.x, otherBounds.Bounds.x );
        int y_distance = math.min(Bounds.w, otherBounds.Bounds.w) - math.max( Bounds.y, otherBounds.Bounds.y );
        //x_distance = math.min(R1.x, R2.x) – max(L1.x, L2.x)
        //y_distance = min(R1.y, R2.y) – max(L1.y, L2.y)
        */

        return new IntBounds(result);
    }
    
    public override string ToString()
    {
        return Bounds.ToString();
    }
}

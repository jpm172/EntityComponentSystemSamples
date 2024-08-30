using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct LevelCell
{
    public IntBounds Bounds;
    
    public int CellId;

    public int2 Origin => Bounds.Origin;
    public int2 Size => Bounds.Size;

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
        return Bounds.ToString();
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
    private static int2 Int2One = new int2(1,1);
    public int4 Bounds; //(X, Y, Z, W)

    public int2 Origin => Bounds.xy;
    public int2 Size => Bounds.zw - Bounds.xy + Int2One;


    public IntBounds( int2 origin, int2 size )
    {
        Bounds = new int4(origin, origin + size - Int2One);
    }
    
    public IntBounds( int4 xyzw )
    {
        Bounds = xyzw;
    }
    
    public bool Contains( IntBounds otherBounds)
    {
        int4 other = otherBounds.Bounds;

        if ( Bounds.x > other.z || Bounds.y > other.w )
            return false;

        if ( Bounds.z < other.x || Bounds.w < other.y )
            return false;

        return true;

    }

    public override string ToString()
    {
        return Bounds.ToString();
    }
}

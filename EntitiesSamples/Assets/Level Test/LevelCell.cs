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

    public LevelCell( int id, int2 xy, int2 zw )
    {
        CellId = id;
        Bounds = new IntBounds(xy, zw);
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
}

public struct IntBounds
{
    public int4 Bounds; //(X, Y, Z, W)

    public int2 Origin => Bounds.xy;
    public int2 Size => Bounds.zw - Bounds.xy;


    public IntBounds( int2 xy, int2 zw )
    {
        Bounds = new int4(xy, zw);
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
}

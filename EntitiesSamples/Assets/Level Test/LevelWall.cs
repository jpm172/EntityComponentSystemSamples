using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct LevelWall
{
    public IntBounds Bounds;
    public int Thickness;
    public int WallId;
    
    public int2 Origin => Bounds.Origin;
    public int2 Size => Bounds.Size;

    public LevelWall( int id, int2 origin, int2 size, int thickness  )
    {
        Bounds = new IntBounds(origin, size);
        Thickness = thickness;
        WallId = id;
    }
    
    public override bool Equals( object obj )
    {
        if ( obj == null )
            return false;

        LevelWall wall = (LevelWall) obj;
        return wall.WallId == WallId;
    }
}

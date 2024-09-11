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
    
    public bool Overlaps( IntBounds otherBounds)
    {
        int4 other = otherBounds.Bounds;

        if ( Bounds.x > other.z || Bounds.y > other.w )
            return false;

        if ( Bounds.z < other.x || Bounds.w < other.y )
            return false;

        return true;
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

        int width = result.z - result.x + 1;
        int height = result.w - result.y + 1;

        if ( width == 1 )
        {
            //if the cut doesnt overlap with the current peice dont make any cuts and return
            if ( otherBounds.Bounds.y > Bounds.w || otherBounds.Bounds.w < Bounds.y )
            {
                cuts = 0;
                cut2 = new IntBounds(result2);
                return new IntBounds(result);
            }
            
            bool lowerCut = otherBounds.Bounds.y > result.y;
            bool upperCut = otherBounds.Bounds.w < result.w;

            if ( lowerCut && upperCut )
            {
                result.w = otherBounds.Bounds.y-1;
                result2.y = otherBounds.Bounds.w + 1;
                
                cuts = 2;
                cut2 = new IntBounds(result2);
                return new IntBounds(result);
            }
            
            if ( otherBounds.Bounds.y  > result.y )
            {
                result.w = otherBounds.Bounds.y-1;
            }
            else if ( otherBounds.Bounds.w < result.w )
            {
                result.y = otherBounds.Bounds.w+1;
            }
        }
        
        if ( height == 1 )
        {
            //if the cut doesnt overlap with the current peice dont make any cuts and return
            if ( otherBounds.Bounds.x > Bounds.z || otherBounds.Bounds.z < Bounds.x )
            {
                cuts = 0;
                cut2 = new IntBounds(result2);
                return new IntBounds(result);
            }
            
            if ( otherBounds.Bounds.x  > result.x )
            {
                result.z = otherBounds.Bounds.x-1;
            }
            else if ( otherBounds.Bounds.z < result.z )
            {
                result.x = otherBounds.Bounds.z+1;
            }
        }

        cut2 = new IntBounds(result2);
        return new IntBounds(result);
    }

    public IntBounds Boolean( IntBounds otherBounds )
    {
        int4 result = Bounds;

        int width = result.z - result.x + 1;
        int height = result.w - result.y + 1;

        if ( width == 1 )
        {

            bool lowerCut = otherBounds.Bounds.y > result.y;
            bool upperCut = otherBounds.Bounds.w < result.w;

            if ( lowerCut  )
            {
                result.y = otherBounds.Bounds.y;
            }

            if ( upperCut )
            {
                result.w = otherBounds.Bounds.w;
            }
            
        }
        
        if ( height == 1 )
        {
            bool leftCut = otherBounds.Bounds.x  > result.x;
            bool rightCut = otherBounds.Bounds.z < result.z;

            if ( leftCut )
            {
                result.z = otherBounds.Bounds.x;
            }
            if ( rightCut )
            {
                result.x = otherBounds.Bounds.z;
            }
        }
        
        return new IntBounds(result);
    }
    
    public override string ToString()
    {
        return Bounds.ToString();
    }
}

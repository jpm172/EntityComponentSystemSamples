using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LevelWall
{
    public LevelCell Cell;
    public int Thickness;

    public LevelWall( LevelCell cell, int thickness )
    {
        Cell = cell;
        Thickness = thickness;
    }
}

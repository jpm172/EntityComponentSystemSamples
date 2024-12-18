using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class GameSettings
{
    private static float _pixelsPerUnit = 16;
    private static int2 _dimensions;


    public static float PixelsPerUnit => _pixelsPerUnit;

    public static int2 Dimensions
    {
        get => _dimensions;
        set => _dimensions = value;
    }
}

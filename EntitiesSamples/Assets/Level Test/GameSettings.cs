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


    public static Dictionary<StatsuEffectID, NameInfo> StatusEffectNames = new Dictionary<StatsuEffectID, NameInfo>{
        { StatsuEffectID.Tourniquet, new NameInfo("Tourniquet", "Turnqt") },
        { StatsuEffectID.Broken, new NameInfo("Broken", "Broken") },
    };

}

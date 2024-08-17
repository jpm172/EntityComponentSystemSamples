using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelRoom 
{
    private int _wallThickness;
    private LevelMaterial _material;
    private LevelGrowthType _growthType; 
    
    private int _id;
    private bool _canGrow;
    private Color _debugColor;
    private int2 _size;
    private int2 _origin;
    private int2 _graphPosition;

    public int2 Size
    {
        get => _size;
        set => _size = value;
    }
    public int2 Origin
    {
        get => _origin;
        set => _origin = value;
    }

    public int2 GraphPosition => _graphPosition;

    public bool CanGrow 
    {
        get => _canGrow;
        set => _canGrow = value;
    }

    public LevelGrowthType GrowthType => _growthType;
    public int WallThickness => _wallThickness;

    public int Index => _id - 1;
    
    public LevelMaterial Material => _material;
    public int Id => _id;
    public Color DebugColor => _debugColor;
    

    public LevelRoom( int id, int2 graphPosition, int2 origin, int2 size, LevelMaterial mat, int wallThickness, LevelGrowthType growthType )
    {
        _id = id;
        _material = mat;
        _growthType = growthType;
        _graphPosition = graphPosition;
        _origin = origin;
        _size = size;
        _wallThickness = wallThickness;
        _canGrow = true;
        _debugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
    }
}

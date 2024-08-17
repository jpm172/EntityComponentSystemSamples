using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelRoom 
{
    private int _wallThickness;
    private LevelMaterial _material;
    private LevelGrowthType _growthType; 
    
    private int _id;
    private bool _canGrow;
    private Color _debugColor;
    private Vector2Int _size;
    private Vector2Int _origin;
    private Vector2Int _graphPosition;

    public Vector2Int Size
    {
        get => _size;
        set => _size = value;
    }
    public Vector2Int Origin => _origin;

    public Vector2Int GraphPosition => _graphPosition;

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
    

    public LevelRoom( int id, Vector2Int graphPosition, Vector2Int origin, Vector2Int size, LevelMaterial mat, int wallThickness, LevelGrowthType growthType )
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

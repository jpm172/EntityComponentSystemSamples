using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelRoom
{
    private int _wallThickness;
    private int _weight;
    private LevelMaterial _material;
    private LevelGrowthType _growthType; 
    
    private int _id;
    private int _cellCount;
    private Color _debugColor;
    private int2 _sizeRatio;
    private int2 _graphPosition;

    private List<int4> _xGrowthDirections;
    private List<int4> _yGrowthDirections;

    private Dictionary<int, List<LevelConnection>> _roomConnections;
    

    public Mesh Mesh;

    

    public List<int4> XGrowthDirections
    {
        get => _xGrowthDirections;
    }

    public List<int4> YGrowthDirections
    {
        get => _yGrowthDirections;
    }

    public int CellCount
    {
        get => _cellCount;
        set => _cellCount = value;
    }
    
    public int2 SizeRatio => _sizeRatio;

    public int2 GraphPosition => _graphPosition;

    public bool CanGrow => CheckCanGrow();

    public LevelGrowthType GrowthType => _growthType;
    public int WallThickness => _wallThickness;

    public int Index => _id - 1;

    public int Weight => _weight;
    

    public LevelMaterial Material => _material;
    public int Id => _id;
    public Color DebugColor => _debugColor;
    

    public LevelRoom( int id, int2 graphPosition, int2 sizeRatio, LevelMaterial mat, int wallThickness, int weight, LevelGrowthType growthType )
    {
        _cellCount = 0;
        _id = id;
        _material = mat;
        _growthType = growthType;
        _graphPosition = graphPosition;
        

        _sizeRatio = sizeRatio;
        _wallThickness = wallThickness;
        _weight = weight;
        _xGrowthDirections = new List<int4>();
        _yGrowthDirections = new List<int4>();
        
        _debugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
    }

    
    
    
    private bool CheckCanGrow()
    {
        return (_xGrowthDirections.Count + _yGrowthDirections.Count) > 0;
    }


}

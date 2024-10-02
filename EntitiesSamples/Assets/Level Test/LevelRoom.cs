using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelRoom 
{
    private static readonly int2 Int2One = new int2(1,1);
    private int _wallThickness;
    private int _weight;
    private LevelMaterial _material;
    private LevelGrowthType _growthType; 
    
    private int _id;
    private int _wallId;
    private Color _debugColor;
    private int4 _bounds;
    private int2 _sizeRatio;
    private int2 _graphPosition;

    private List<int2> _xGrowthDirections;
    private List<int2> _yGrowthDirections;
    //private Dictionary<int, List<LevelConnection>> _roomConnections;
    

    public Mesh FloorMesh;
    public Mesh WallMesh;
    
    public List<int2> XGrowthDirections
    {
        get => _xGrowthDirections;
        set => _xGrowthDirections = value;
    }

    public List<int2> YGrowthDirections
    {
        get => _yGrowthDirections;
        set => _yGrowthDirections = value;
    }

    public int2 Size=> _bounds.Size();

    public int2 Origin => _bounds.Origin();

    public int4 Bounds//(X,Y,Z,W)
    {
        get => _bounds;
        set => _bounds = value;
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
    public int WallId => _wallId;
    public Color DebugColor => _debugColor;
    

    public LevelRoom( int id, int wallId, int2 graphPosition, int2 origin, int2 size, int2 sizeRatio, LevelMaterial mat, int wallThickness, int weight, LevelGrowthType growthType )
    {
        _id = id;
        _wallId = wallId; 
        _material = mat;
        _growthType = growthType;
        _graphPosition = graphPosition;
        _bounds = new int4(origin, origin+size - Int2One);
        _sizeRatio = sizeRatio;
        _wallThickness = wallThickness;
        _weight = weight;
        _xGrowthDirections = new List<int2>();
        _yGrowthDirections = new List<int2>();
        
        _debugColor = new Color(Random.Range( 0,1f ),Random.Range( 0,1f ),Random.Range( 0,1f ), 1);
    }

    private bool CheckCanGrow()
    {
        return (_xGrowthDirections.Count + _yGrowthDirections.Count) > 0;
    }
}

public struct RoomInfo
{
    public int WallThickness;
    public LevelMaterial WallMaterial;
}

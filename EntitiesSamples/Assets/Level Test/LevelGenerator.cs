using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField]
    private Vector2Int dimensions;
    
    private List<LevelFloor> _floors;
    private List<LevelWall> _walls;
    private int[] _levelLayout;
    public void GenerateLevel()
    {
        _levelLayout = new int[dimensions.x*dimensions.y];
        _floors = new List<LevelFloor>();
        _walls = new List<LevelWall>();
        
    }

    private void MakeEntities()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager entityManager = world.EntityManager;
    }
}

public struct LevelFloor
{
    public Mesh FloorMesh;
}

public struct LevelWall
{
    
}
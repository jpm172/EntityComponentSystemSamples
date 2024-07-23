using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CubeFieldAuthoring : MonoBehaviour
{
    public float CellRadius = 8.0f;
    public float SeparationWeight = 1.0f;
    public float AlignmentWeight = 1.0f;
    public float TargetWeight = 2.0f;
    public float ObstacleAversionDistance = 30.0f;
    public float MoveSpeed = 25.0f;

    class Baker : Baker<CubeFieldAuthoring>
    {
        public override void Bake( CubeFieldAuthoring authoring )
        {
            
        }
    }
    
    public struct CubeField : IComponentData
    {

    }
}

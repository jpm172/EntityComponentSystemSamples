using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CubeAuthoring : MonoBehaviour
{
    public float speed = 100;

    public class CubeBaker : Baker<CubeAuthoring>
    {
        public override void Bake( CubeAuthoring authoring )
        {
            Entity entity = GetEntity( TransformUsageFlags.Dynamic );
            AddComponent( entity, new CubeData()
            {
                speed = authoring.speed
            } );
        }
    }
}

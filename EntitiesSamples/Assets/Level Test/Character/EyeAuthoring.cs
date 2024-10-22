using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class EyeAuthoring : MonoBehaviour
{
    public GameObject owner; 
    public class EyeBaker : Baker<EyeAuthoring>
    {
        public override void Bake(EyeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            /*
            AddComponent(entity, new ThirdPersonPlayer
            {
                ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                ControlledCamera = GetEntity(authoring.ControlledCamera, TransformUsageFlags.Dynamic),
            });
            */
            AddComponent( entity, new ClearFogComponent() );
        }
    }
}

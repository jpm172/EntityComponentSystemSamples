using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class EntityColliderAuthoring : MonoBehaviour
{
    public class Baker : Baker<EntityColliderAuthoring>
    {
        public override void Bake( EntityColliderAuthoring authoring )
        {
            Entity entity = GetEntity( TransformUsageFlags.Dynamic );
            AddComponent( entity, new EntityCollider() );
            SetComponentEnabled<EntityCollider>( entity, false );
        }
    }
}

public struct EntityCollider : IComponentData, IEnableableComponent
{
    
}

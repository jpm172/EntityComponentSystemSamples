using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class WeaponItemAuthoring : MonoBehaviour
{
    public class WeaponItemBaker : Baker<WeaponItemAuthoring>
    {
        public override void Bake( WeaponItemAuthoring authoring )
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WeaponDesc
            {
                
            });
        }
    }
}

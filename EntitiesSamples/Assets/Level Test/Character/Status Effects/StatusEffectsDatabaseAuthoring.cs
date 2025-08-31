using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class StatusEffectDatabaseAuthoring : MonoBehaviour
{
    public List<GameObject> StatusEffects;

    public class Baker : Baker<StatusEffectDatabaseAuthoring>
    {
        public override void Bake(StatusEffectDatabaseAuthoring  authoring)
        {
        
        }
    }
}



using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public class GameConfigAuthoring : MonoBehaviour
{
    public GameObject GrenadePrefab;
    class Baker : Baker<GameConfigAuthoring>
    {
        public override void Bake(GameConfigAuthoring authoring)
        {
            // Create an EntityPrefabReference from a GameObject.
            // By using a reference, we only need one baked prefab entity instead of
            // duplicating the prefab entity everywhere it is used.
            var prefabEntity = new EntityPrefabReference(authoring.GrenadePrefab);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GameConfig
            {
                GrenadeReference = prefabEntity
            });
        }
    }

}


public struct GameConfig : IComponentData
{
    public EntityPrefabReference GrenadeReference;
}
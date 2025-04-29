using Unity.Collections;
using Unity.Entities;

public struct StatusEffectDatabase : IComponentData
{
    public NativeHashMap<int, Entity> statusEffectPrefabs;
}


using Unity.Entities;
using Unity.Mathematics;

public struct StructureInfo : IComponentData
{
    public LevelMaterial Material;
    public int2 Dimensions;
}

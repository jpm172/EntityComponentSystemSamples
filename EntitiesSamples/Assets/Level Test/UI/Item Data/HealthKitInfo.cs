using Unity.Entities;

public struct HealthKitInfo : IComponentData
{
    public HealthKitUseType UseType;
    public BodyPart TargetLimb;
    
}

public enum HealthKitUseType
{
    HealAll,
    HealLimb
}
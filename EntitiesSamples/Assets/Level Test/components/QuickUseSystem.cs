using Unity.Burst;
using Unity.Entities;

partial struct QuickUseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ( var (itemData, useData, player) in
            SystemAPI.Query< RefRO<CharacterItemData>, RefRO<UseOnEquip>>()
                .WithEntityAccess() )
        {
            
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

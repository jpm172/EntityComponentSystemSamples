using Unity.Burst;
using Unity.Entities;

partial struct PlayerItemSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<PlayerItemData>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        foreach ( var (itemData, playerItemData, entity) in SystemAPI.Query<RefRO<CharacterItemData>, EnabledRefRW<PlayerItemData>>()
            .WithEntityAccess() )
        {
            PlayerUIManager.Instance.NewItem( entity, itemData.ValueRO );
            playerItemData.ValueRW = false;
        }
    }
    
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

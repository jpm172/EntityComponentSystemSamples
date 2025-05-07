using Unity.Entities;

public struct CharacterAction : IBufferElementData
{
    public ActionType Action;
    public Entity Item;
    public int EquipSlot;
}

public enum ActionType
{
    EquipItem,
    EquipSlot,
    Use,
    Remove
}
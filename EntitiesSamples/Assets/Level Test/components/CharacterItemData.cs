using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct CharacterItemData : IComponentData
{
    [SerializeField]
    private int _key;
    public int Key => _key;

    public int Quantity;

    public float EquipTime;

    public Entity Owner;
    
    public CharacterItemData( Entity owner, float equipTime, int quantity, int key )
    {
        Owner = owner;
        EquipTime = equipTime;
        _key = key;
        Quantity = quantity;
    }
}

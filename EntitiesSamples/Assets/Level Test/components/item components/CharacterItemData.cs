using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct CharacterItemData : IComponentData
{
    [SerializeField]
    private int _id;
    
    [SerializeField]
    private int _key;
    public int Key => _key;

    public int ID => _id;

    public int Quantity;

    public float EquipTime;

    public Entity Owner;
    
    public CharacterItemData( Entity owner, int id, float equipTime, int quantity, int key )
    {
        Owner = owner;
        EquipTime = equipTime;
        _id = id;
        _key = key;
        Quantity = quantity;
    }
}

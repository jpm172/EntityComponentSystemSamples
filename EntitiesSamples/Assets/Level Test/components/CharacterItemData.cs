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
    
    public CharacterItemData( float equipTime, int quantity, int key )
    {
        EquipTime = equipTime;
        _key = key;
        Quantity = quantity;
    }
}

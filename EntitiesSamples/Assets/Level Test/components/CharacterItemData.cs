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
    
    public CharacterItemData( int quantity, int key )
    {
        _key = key;
        Quantity = quantity;
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(20)]
public struct DamageInfo : IBufferElementData
{
    public float Damage;
    public float BleedDamage;

    public DamageInfo( float damage, float bleedDamage )
    {
        Damage = damage;
        BleedDamage = bleedDamage;
    }
    
}

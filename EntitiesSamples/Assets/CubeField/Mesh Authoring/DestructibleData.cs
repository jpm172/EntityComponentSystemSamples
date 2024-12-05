using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;


[InternalBufferCapacity(32*32)]
public struct DestructibleData : IBufferElementData
{
    public int Value;
}

public struct DestructibleTag : IEnableableComponent
{}
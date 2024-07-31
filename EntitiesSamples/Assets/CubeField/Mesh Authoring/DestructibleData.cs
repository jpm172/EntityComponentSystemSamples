using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct DestructibleData : IComponentData
{
    public NativeArray<int> PointField;
}

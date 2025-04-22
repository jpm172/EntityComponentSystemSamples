using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct HealthItemDesc : IComponentData
{
    public int MaxCharges;
    public int CurrentCharges;
    public float HealRate;
    public float HealTimer;
    public HealthItemType Type;
}

public enum HealthItemType
{
    HealthKit = 0,
    Tourniquet = 1
}
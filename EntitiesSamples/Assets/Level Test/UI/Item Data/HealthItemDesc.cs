using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct HealthItemDesc : IComponentData
{
    public int MaxCharges;
    public int CurrentCharges;
    public float HealTime;
    public float TimerRemaining;
    public int ChargesPerHeal;
    public HealthItemType Type;
    public ItemState State;
}

public enum HealthItemType
{
    HealthKit = 0,
    Tourniquet = 1
}

public enum ItemState
{
    Ready,
    Using,
    Cancel
}
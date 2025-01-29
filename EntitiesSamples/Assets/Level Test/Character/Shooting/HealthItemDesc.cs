using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class HealthItemDesc : IComponentData
{
    public int MaxCharges;
    public int CurrentCharges;
}

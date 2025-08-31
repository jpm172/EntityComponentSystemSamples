using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct QuickUseData : IComponentData, IEnableableComponent
{
    public BodyPart Part;
}

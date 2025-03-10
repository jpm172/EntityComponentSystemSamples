using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct QuickUseData : IComponentData
{
    public Entity PreviousEquipped;
    public BodyPart Part;
    public bool InHotBar;
}

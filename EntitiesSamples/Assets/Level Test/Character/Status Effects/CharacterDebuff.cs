using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(30)]
public struct CharacterDebuff : IBufferElementData
{
    public BodyPart AffectedPart;
    public Entity DebuffEntity;
    
}
